using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// Unity behaviour class to manage an experiment.
/// </summary>
public class ExperimentManager : MonoBehaviour {
    // Public fields
    public GameObject homePad, doneButton;
    public string returnScene;

    public GameObject[] trialTypes;
    public int repetitions = 1;

    public double touchClickTime = 0.5;

    public float hitScore = 5f;
    public float missPenalty = 20f;
    public float timePenalty = .5f;
    public float distanceScoreMultiplier = 15f;
    public float airtimePenalty = 2;

    // Private fields
    private enum State : byte { Initialisation, Instructions, PreTrial, InTrial, PostTrial, ThankYou };

    private State state = State.Initialisation;
    private int trialCount = 0;
    private float score = 0;

    private double touchTime = 0;
    private GameObject selected;

    private GameObject overlay;
    private Text overlayText;

    private GameObject trial;
    private TrialManager trialManager;
    private int trials;
    private int trialIndex = 0;

    /// <summary>
    /// Used for Unity object initialisation.
    /// </summary>
    void Start() {
        this.overlay = GameObject.Find("Overlay");
        this.overlayText = GameObject.Find("OverlayText").GetComponent<Text>();

        this.trials = this.trialTypes.Length * this.repetitions;
        GameObject[] trialTypes = new GameObject[this.trials];
        for (int i = 0; i < trialTypes.Length; i++) {
            trialTypes[i] = this.trialTypes[i % this.trialTypes.Length];
        }
        this.trialTypes = trialTypes;
        (new System.Random()).Shuffle<GameObject>(this.trialTypes);

        this.PrepareTrial();
	}

    /// <summary>
    /// Given a touched object, track long-presses.
    /// </summary>
    /// <param name="hit">The currently touched object.</param>
    /// <returns>The currently long-pressed object, or null otherwise.</returns>
    GameObject TrackTouch(GameObject hit) {
        if (Input.GetMouseButton(0)) {
            if (hit == this.selected) {
                this.touchTime += Time.deltaTime;
            } else {
                this.touchTime = 0;
                this.selected = hit;
            }
        } else {
            this.selected = null;
            this.touchTime = 0;
        }

        if (this.touchTime > this.touchClickTime) {
            return this.selected;
        } else {
            return null;
        }
    }

    /// <summary>
    /// Unity update function, called every frame.
    /// </summary>
    void Update() {
        Vector2 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        GameObject selected = this.TrackTouch(Physics2D.OverlapPoint(position)?.gameObject);

        if (selected == this.homePad) {
            if (this.state == State.PreTrial) {
                this.StartTrial();
            } else if (this.state == State.PostTrial) {
                if (this.trialCount >= this.trials) {
                    this.ShowOverlay("Thank you for your participation!");
                    this.ChangeState(State.ThankYou);
                    this.doneButton.SetActive(true);
                    this.homePad.SetActive(false);
                } else {
                    this.PrepareTrial();
                }
            }
        } else if (selected == this.doneButton) {
            if (this.state == State.ThankYou) {
                SceneManager.LoadScene(returnScene);
            }
        }

        if (this.state == State.InTrial) {
            if (selected == this.homePad) {
                this.trialManager.ReturnHome();
            } else {
                this.trialManager.LeaveHome();
                if (selected == this.doneButton) {
                    this.EndTrial();
                } else {
                    float? distance = this.trialManager.TapObject(selected, position);
                    if (distance != null) {
                        if (distance >= 0) {
                            this.score += this.hitScore +  (0.5f - (float)distance) * this.distanceScoreMultiplier;
                        } else {
                            this.score -= this.missPenalty;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Prepare for a new trial, show cue.
    /// </summary>
    private void PrepareTrial() {
        this.ShowOverlay("Look for the following shape:");
        this.ChangeState(State.PreTrial);
        this.doneButton.SetActive(false);
        this.homePad.SetActive(true);
        this.touchTime = -1;

        if (this.trial != null) {
            this.trialManager = null;
            Destroy(this.trial);
        }
        this.trial = Instantiate(this.trialTypes[this.trialIndex++]);
        this.trialManager = this.trial.GetComponent<TrialManager>();
    }

    /// <summary>
    /// Show the overlay with a given message.
    /// </summary>
    /// <param name="text">The message to display.</param>
    private void ShowOverlay(string text) {
        this.overlayText.text = text;
        this.overlay.SetActive(true);
    }

    /// <summary>
    /// Hide the overlay.
    /// </summary>
    private void HideOverlay() {
        this.overlayText.text = "";
        this.overlay.SetActive(false);
    }

    /// <summary>
    /// End the current trial, calculate the performance and provide feedback.
    /// </summary>
    private void EndTrial() {
        this.doneButton.SetActive(false);

        this.ChangeState(State.PostTrial);
        var stats = this.trialManager.EndTrial();

        // Do something with stats, calculate score, show cue. Currently super arbitrary.
        var airscore = -1 * stats.airtime * this.airtimePenalty;
        var timescore = -1 * stats.trialtime * this.timePenalty;
        var missscore = -1 * (float)stats.misses / stats.targets * this.missPenalty;
        var hitscore = (float)stats.hits / stats.targets * this.hitScore;
        var distancescore = stats.taps > 0 ? stats.tapdistance / stats.taps * this.distanceScoreMultiplier : 0;
        this.score += airscore + timescore + missscore + hitscore + distancescore;

        var losses = new float[] { airscore, timescore, missscore, hitscore - this.hitScore, distancescore - this.distanceScoreMultiplier };
        var highest_loss_index = Array.IndexOf(losses, losses.Min());
        var cue_messages = new string[] {
            "move faster, reducing airtime,",
            "complete trials faster",
            "prevent tapping incorrect targets",
            "make sure that you do not miss any targets",
            "hit targets more closely in the center"
        };
        string cue = string.Format("Try to {0}\nto improve your score!\nTap to continue...", cue_messages[highest_loss_index]);

        EventLogger.Instance.LogEvent(EventLogger.Type.Trial, "score", score);
        EventLogger.Instance.LogEvent(EventLogger.Type.Trial, "stats", stats);
        EventLogger.Instance.LogEvent(EventLogger.Type.Trial, "cue", cue_messages[highest_loss_index]);
        Debug.Log(string.Join(", ", losses)); // Debug for raw loss scores

        this.ShowOverlay(cue);
    }

    /// <summary>
    /// Start the next trial.
    /// </summary>
    private void StartTrial() {
        this.HideOverlay();
        this.trialManager.StartTrial();
        this.trialCount++;
        this.ChangeState(State.InTrial);
        this.doneButton.SetActive(true);
    }

    /// <summary>
    /// Logs an internal trial state change.
    /// </summary>
    /// <param name="newState">The state to transition to.</param>
    private void ChangeState(State newState) {
        this.state = newState;
        EventLogger.Instance.LogEvent(EventLogger.Type.Experiment, "state", this.state);
    }
}
