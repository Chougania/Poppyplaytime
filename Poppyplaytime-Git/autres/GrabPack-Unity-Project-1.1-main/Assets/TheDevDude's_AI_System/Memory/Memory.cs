using UnityEngine;

public enum MemoryType
{
    Vision,
    Sound,
    Interaction,
    Danger,
    Interest
}

public class Memory
{
    public MemorySubject Subject;

    public MemoryType Type;

    public Vector3 Position;

    public Transform Target;

    public float Confidence;

    public float TimeCreated;

    public float Duration;

    public float Age => Time.time - TimeCreated;

    public bool IsExpired => Age >= Duration;

    public Memory(
        MemorySubject subject,
        MemoryType type,
        Vector3 position,
        Transform target,
        float confidence,
        float duration)
    {
        Subject = subject;
        Type = type;
        Position = position;
        Target = target;
        Confidence = confidence;
        Duration = duration;
        TimeCreated = Time.time;
    }

    public void UpdateConfidence(float decayRate)
    {
        Confidence -= decayRate * Time.deltaTime;
        Confidence = Mathf.Clamp01(Confidence);
    }
}