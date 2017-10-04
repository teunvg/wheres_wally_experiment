using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unity behaviour class to manage a single stimulus.
/// </summary>
public class Stimulus : MonoBehaviour {
    // Public fields
    public enum Type : byte { Cue, Distractor, Target };
    public Quaternion rotation;

    public Type type;

    // Private field
    private Sprite sprite;

    /// <summary>
    /// A struct to store stimulus data.
    /// </summary>
    private struct StimulusData {
        public Vector2 position;
        public float rotation;
        public Type type;
    }

    /// <summary>
    /// A struct to store stimulus tap data.
    /// </summary>
    private struct StimulusTap {
        public Vector2 position;
        public Vector2 tap;
    }

    /// <summary>
    /// Masks this stimulus using the given sprite.
    /// </summary>
    /// <param name="mask">The sprite to use to mask this stimulus.</param>
	public void Mask(Sprite mask) {
        this.gameObject.GetComponent<SpriteRenderer>().sprite = mask;
        this.gameObject.transform.rotation = Quaternion.identity;
    }

    /// <summary>
    /// Unmasks this stimulus, returning to its initial presentation.
    /// </summary>
    public void Unmask() {
        this.gameObject.GetComponent<SpriteRenderer>().sprite = this.sprite;
        this.gameObject.transform.rotation = this.rotation;
    }

    /// <summary>
    /// Initializes the stimulus with the given type and rotation.
    /// </summary>
    /// <param name="type">The stimulus type (Cue, Target or Distractor).</param>
    /// <param name="rotation">The desired stimulus rotation.</param>
    public void Initialize(Type type, Quaternion rotation) {
        this.type = type;
        this.rotation = rotation;
        this.Hide();
        this.sprite = this.gameObject.GetComponent<SpriteRenderer>().sprite;
        this.Unmask();
    }

    /// <summary>
    /// Hides the stimulus.
    /// </summary>
    public void Hide() {
        this.gameObject.GetComponent<SpriteRenderer>().enabled = false;
    }

    /// <summary>
    /// Shows the stimulus.
    /// </summary>
    public void Show() {
        this.gameObject.GetComponent<SpriteRenderer>().enabled = true;
        if (this.type != Type.Cue) {
            EventLogger.Instance.LogEvent(EventLogger.Type.Stimulus, "show", new StimulusData { position = this.transform.position, rotation = this.rotation.eulerAngles.z, type = this.type });
        }
    }

    /// <summary>
    /// Processes a given tap event, returning whether the stimulus is a target and, if so, what the tap distance from its center is.
    /// </summary>
    /// <param name="position">The tap position.</param>
    /// <returns>The distance from the stimulus center when the stimulus is a target, -1 otherwise.</returns>
    public float Tap(Vector2 position) {
        EventLogger.Instance.LogEvent(EventLogger.Type.Stimulus, "tap", new StimulusTap { position = this.transform.position, tap = position });
        return this.type == Type.Target ? (position - (Vector2)this.transform.position).magnitude : -1;
    }
}
