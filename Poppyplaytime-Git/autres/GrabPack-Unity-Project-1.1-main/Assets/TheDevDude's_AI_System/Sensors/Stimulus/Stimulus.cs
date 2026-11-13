using UnityEngine;

public class Stimulus
{
    public StimulusSubject Subject;

    public StimulusType Type;

    public Vector3 Position;

    public Transform Target;

    public float Strength;

    public float TimeDetected;

    public Stimulus(
        StimulusSubject subject,
        StimulusType type,
        Vector3 position,
        Transform target,
        float strength)
    {
        Subject = subject;
        Type = type;
        Position = position;
        Target = target;
        Strength = Mathf.Clamp01(strength);
        TimeDetected = Time.time;
    }
}