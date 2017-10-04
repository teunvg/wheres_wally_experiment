using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Unity behaviour class to manage a single trial.
/// </summary>
public class TrialManager : MonoBehaviour {
    // Public fields
    public GameObject stimulus;

    public bool serialPresentation = false;
    public float serialPresentationTime = 2;

    public int stimulusCount = 25;
    public int targetCount = 3;

    public float stimulusSize = .5f;
    public float minimumStimulusSpacing = 1;
    public float minimumTargetSpacing = 3;

    public int rotationStep = 45;
    private int targetRotation = 180;

    public Sprite maskSprite, stimulusSprite;

    public Rect region = new Rect(new Vector2(-10, -3), new Vector2(16, 8));

    // Private fields
    private enum State : byte { Initialisation, Searching, Moving, CorrectTap, IncorrectTap, Done };

    private GameObject[] stimuli;
    private State state = State.Initialisation;
    private GameObject tapped;

    private Vector3 initPosition;

    private GameObject cue;

    private float presentationTime = 0;
    private int presentationIndex = 0;

    /// <summary>
    /// Struct containing all relevant trial statistics.
    /// </summary>
    public struct TrialStats {
        public float trialtime, airtime;
        public int hits, misses, targets, taps;
        public float tapdistance;
    }

    private TrialStats stats = new TrialStats() {
        trialtime = 0, airtime = 0,
        hits = 0, misses = 0, targets = 0,
        tapdistance = 0, taps = 0
    };

    /// <summary>
    /// Used for Unity object initialisation.
    /// </summary>
    void Start() {
        this.ChangeState(State.Initialisation);
        this.stats.targets = this.targetCount;

        float max_spacing = (float)Math.Max(this.minimumStimulusSpacing, this.minimumTargetSpacing);
        this.initPosition = this.region.position - new Vector2(max_spacing, max_spacing);

        this.targetRotation = (new System.Random()).Next(4) * 90;

        this.cue = Instantiate(this.stimulus, this.region.center + new Vector2(0, -2), Quaternion.identity);
        this.cue.GetComponent<Stimulus>().Initialize(Stimulus.Type.Cue, Quaternion.Euler(0, 0, this.targetRotation));
        this.cue.GetComponent<Stimulus>().Show();

        int orientations = (int)(360 / this.rotationStep) - 1;
        this.stimuli = new GameObject[this.stimulusCount + this.targetCount];
        for (int i = 0; i < this.stimuli.Length; i++) {
            this.stimuli[i] = Instantiate(this.stimulus, this.initPosition, Quaternion.identity);
            if (i >= this.stimulusCount) {
                this.stimuli[i].GetComponent<Stimulus>().Initialize(Stimulus.Type.Target, Quaternion.Euler(0, 0, this.targetRotation));
            } else {
                float orientation = this.rotationStep * (int)(Random.value * orientations);
                orientation += orientation >= this.targetRotation ? this.rotationStep : 0;
                this.stimuli[i].GetComponent<Stimulus>().Initialize(Stimulus.Type.Distractor, Quaternion.Euler(0, 0, orientation));
            }
        }
        (new System.Random()).Shuffle<GameObject>(this.stimuli);

        Vector3 newLocation;
        for (int i = 0; i < this.stimuli.Length; i++) {
            int counter = 0;
            do {
                newLocation = this.region.position + RandomVector(this.region.size);
                if (counter++ > 100) Debug.Break();
            } while (this.IsTouching(newLocation, i) || this.IsNearbyTarget(newLocation, i));
            this.stimuli[i].GetComponent<Transform>().localPosition = newLocation;
        }
    }

    /// <summary>
    /// Called when attached object is destroyed; removes all stimuli related to trial.
    /// </summary>
    void OnDestroy() {
        foreach (GameObject stimulus in this.stimuli) {
            Destroy(stimulus);
        }
    }

    /// <summary>
    /// Unity update function, called every frame.
    /// </summary>
    void Update() {
        if (this.serialPresentation && this.state == State.Searching) {
            this.presentationTime += Time.deltaTime;
            if (this.presentationTime >= this.serialPresentationTime) {
                this.presentationTime = 0;
                if (!this.ShowNext()) {
                    this.EndTrial();
                }
            }
        }

        if (this.state == State.Searching) {
            this.stats.trialtime += Time.deltaTime;
        } else if (this.state >= State.Moving && this.state <= State.IncorrectTap) {
            this.stats.airtime += Time.deltaTime;
        }
    }

    /// <summary>
    /// Show next stimulus when this trial is in serial presentation mode.
    /// </summary>
    /// <returns>Whether next stimulus could be displayed; returns false when all stimuli have been shown.</returns>
    private bool ShowNext() {
        this.stimuli[this.presentationIndex++].GetComponent<Stimulus>().Hide();
        if (this.presentationIndex < this.stimuli.Length) {
            this.stimuli[this.presentationIndex].GetComponent<Stimulus>().Show();
            return true;
        } else {
            return false;
        }
    }

    /// <summary>
    /// Start the trial.
    /// </summary>
    public void StartTrial() {
        this.ChangeState(State.Searching);
        this.cue.GetComponent<Stimulus>().Hide();
        if (this.serialPresentation) {
            this.stimuli[0].GetComponent<Stimulus>().Show();
        } else {
            foreach (var stimulus in this.stimuli) {
                stimulus.GetComponent<Stimulus>().Show();
            }
        }
    }

    /// <summary>
    /// End the trial.
    /// </summary>
    /// <returns>All relevant trial statistics.</returns>
    public TrialStats EndTrial() {
        this.ShowStimuli();
        this.ChangeState(State.Done);
        foreach (var stimulus in this.stimuli) {
            stimulus.GetComponent<Stimulus>().Hide();
        }
        return this.stats;
    }

    /// <summary>
    /// Trigger this method when participant is no longer touching the home button.
    /// </summary>
    public void LeaveHome() {
        if (this.state == State.Searching) {
            this.ChangeState(State.Moving);
            this.HideStimuli();
        }
    }

    /// <summary>
    /// Trigger this method when participant returns to touching the home button.
    /// </summary>
    public void ReturnHome() {
        if (this.state >= State.Moving && this.state <= State.IncorrectTap) {
            this.ShowStimuli();
            if (this.state == State.CorrectTap) {
                this.tapped.GetComponent<SpriteRenderer>().color = Color.green;
            } else if (this.state == State.IncorrectTap) {
                this.tapped.GetComponent<SpriteRenderer>().color = Color.red;
            }
            this.ChangeState(State.Searching);
        }        
    }

    /// <summary>
    /// Hide all stimuli using the trials masking sprite.
    /// </summary>
    private void HideStimuli() {
        foreach (var stimulus in stimuli) {
            stimulus.GetComponent<Stimulus>().Mask(maskSprite);
        }
    }

    /// <summary>
    /// Show all stimuli.
    /// </summary>
    private void ShowStimuli() {
        foreach (var stimulus in stimuli) {
            stimulus.GetComponent<Stimulus>().Unmask();
        }
    }

    /// <summary>
    /// Method to check whether the tapped object was a stimulus, and if so, 
    /// whether it was a target and how closely to the center the participant tapped it.
    /// </summary>
    /// <param name="obj">The tapped object.</param>
    /// <param name="position">The world position of the tap.</param>
    /// <returns>
    /// Returns -1 if a distractor was tapped, a value of 0 or higher indicating the distance 
    /// from the center of a tapped target, or null if no stimulus was tapped.
    /// </returns>
    public float? TapObject(GameObject obj, Vector2 position) {
        if (this.state == State.Moving && Array.IndexOf(this.stimuli, obj) != -1) {
            float distance = obj.GetComponent<Stimulus>().Tap(position);
            obj.GetComponent<SpriteRenderer>().color = Color.gray;
            this.tapped = obj;
            if (distance >= 0) {
                this.ChangeState(State.CorrectTap);
                this.stats.hits++;
            } else {
                this.ChangeState(State.IncorrectTap);
                this.stats.misses++;
            }
            this.stats.tapdistance += 1 - (distance / stimulusSize);
            this.stats.taps++;
            return distance;
        } else {
            return null;
        }
    }

    /// <summary>
    /// Logs an internal trial state change.
    /// </summary>
    /// <param name="newState">The state to transition to.</param>
    private void ChangeState(State newState) {
        this.state = newState;
        EventLogger.Instance.LogEvent(EventLogger.Type.Trial, "state", this.state);
    }

    /// <summary>
    /// Checks whether a stimulus placed at a given position would collide with any other stimulus up to a given index.
    /// </summary>
    /// <param name="position">The position to check for space for a new stimulus.</param>
    /// <param name="index">The number of existing stimuli to check for collisions for.</param>
    /// <returns>Whether a stimulus placed at the given position would collide with any other stimulus up to the given index.</returns>
    private bool IsTouching(Vector3 position, int index) {
        for (int i = 0; i < index; i++) {
            //Stimulus.Type type = this.stimuli[i].GetComponent<Stimulus>().type;
            Vector3 other = this.stimuli[i].GetComponent<Transform>().localPosition;
            float distance = (position - other).magnitude;
            if (distance < this.minimumStimulusSpacing) {
                //Debug.Log(String.Format("{0} collides with {1}", position, other));
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks whether a target placed at a given position would be too close to any other target up to a given stimulus index.
    /// </summary>
    /// <param name="position">The position to check for space for a new target.</param>
    /// <param name="index">The number of existing stimuli to check for target collisions for.</param>
    /// <returns>Whether a target placed at the given position would collide with any other target up to the given stimulus index.</returns>
    private bool IsNearbyTarget(Vector3 position, int index) {
        for (int i = this.stimulusCount - this.targetCount - 1; i < index; i++) {
            Vector3 other = this.stimuli[i].GetComponent<Transform>().localPosition;
            float distance = (position - other).magnitude;
            if (distance < this.minimumTargetSpacing) {
                //Debug.Log(String.Format("{0} is too close to {1}", position, other));
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Generates a random vector between (0, 0) and the given maximum vector.
    /// </summary>
    /// <param name="size">The maximum vector.</param>
    /// <returns>A random vector between (0, 0) and the given maximum vector.</returns>
    private static Vector2 RandomVector(Vector2 size) {
        return new Vector2(
            Random.Range(0, size.x),
            Random.Range(0, size.y));
    }
}
