using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton class for storing and/or transmitting time-locked experiment data.
/// </summary>
public class EventLogger {
    /// <summary>
    /// Static constants for event types.
    /// </summary>
    public static class Type {
        public const string Experiment = "experiment";
        public const string Trial = "trial";
        public const string Stimulus = "stimulus";
    }

    /// <summary>
    /// Private class for storing a single event.
    /// </summary>
    /// <typeparam name="T">The type of data contained in the event.</typeparam>
    private class EventId<T> : IComparable {
        public string Type, Name;
        public float Time;
        public T Value;

        public EventId(string type, string name, float time, T value) {
            this.Type = type;
            this.Name = name;
            this.Time = time;
            this.Value = value;
        }

        public override string ToString() {
            return string.Format("{0}.{1} at +{2}s: {3}", this.Type, this.Name, this.Time, this.Value);
        }

        public override int GetHashCode() {
            return this.ToString().GetHashCode();
        }

        public int CompareTo(object obj) {
            return Time.CompareTo(obj);
        }
    }

    private static EventLogger instance;

    private IList<EventId<object>> events;

    /// <summary>
    /// Create new (singleton) event logger.
    /// </summary>
    private EventLogger() {
        events = new List<EventId<object>>();
    }

    /// <summary>
    /// Retrieve the singleton instance of the event logger.
    /// </summary>
    public static EventLogger Instance {
        get {
            if (instance == null) {
                instance = new EventLogger();
            }
            return instance;
        }
    }

    /// <summary>
    /// Logs an event of the given type, with a given name and value.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <param name="name">The name of the event.</param>
    /// <param name="value">the data associated with the event.</param>
    public void LogEvent(string type, string name, object value) {
        EventId<object> id = new EventId<object>(type, name, Time.time, value);
        events.Add(id);
        Debug.Log(id.ToString());
    }

    /// <summary>
    /// Logs an event of the given type, with a given name and no value associated with it.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <param name="name">The name of the event.</param>
    public void LogEvent(string type, string name) {
        this.LogEvent(type, name, true);
    }
}
