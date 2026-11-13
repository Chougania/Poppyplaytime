using System.Collections.Generic;
using UnityEngine;

public class MemorySystem
{
    private readonly Blackboard blackboard;

    private readonly List<Memory> memories = new();

    public IReadOnlyList<Memory> Memories => memories;

    public MemorySystem(Blackboard blackboard)
    {
        this.blackboard = blackboard;
    }

    public void AddMemory(Memory memory)
    {
        memories.Add(memory);
    }

    public void Update(float decayRate)
    {
        for (int i = memories.Count - 1; i >= 0; i--)
        {
            Memory memory = memories[i];

            memory.UpdateConfidence(decayRate);

            if (memory.IsExpired || memory.Confidence <= 0f)
            {
                memories.RemoveAt(i);
            }
        }
    }

    public Memory GetStrongestMemory(MemoryType type)
    {
        Memory best = null;
        float bestConfidence = -1;

        foreach (Memory memory in memories)
        {
            if (memory.Type != type)
                continue;

            if (memory.Confidence > bestConfidence)
            {
                bestConfidence = memory.Confidence;
                best = memory;
            }
        }

        return best;
    }

    public void ForgetAll()
    {
        memories.Clear();
    }
    public void Forget(MemorySubject subject)
    {
        memories.RemoveAll(m => m.Subject == subject);
    }

    public Memory GetMemory(MemorySubject subject, MemoryType type)
    {
        foreach (Memory memory in memories)
        {
            if (memory.Subject == subject && memory.Type == type)
                return memory;
        }

        return null;
    }

    public void ProcessStimulus(Stimulus stimulus)
    {
        Memory existing = memories.Find(m =>
            m.Subject == (MemorySubject)stimulus.Subject &&
            m.Type == ConvertMemoryType(stimulus.Type));

        if (existing != null)
        {
            existing.Position = stimulus.Position;
            existing.Target = stimulus.Target;
            existing.Confidence = stimulus.Strength;
            existing.TimeCreated = Time.time;
        }
        else
        {
            Memory memory = new Memory(
                (MemorySubject)stimulus.Subject,
                ConvertMemoryType(stimulus.Type),
                stimulus.Position,
                stimulus.Target,
                stimulus.Strength,
                10f
            );

            memories.Add(memory);
            existing = memory;
        }

        //UpdateBlackboard(existing);
    }

    private MemoryType ConvertMemoryType(StimulusType stimulusType)
    {
        return stimulusType switch
        {
            StimulusType.Vision => MemoryType.Vision,
            StimulusType.Sound => MemoryType.Sound,
            StimulusType.Touch => MemoryType.Interaction,
            StimulusType.Light => MemoryType.Interest,
            StimulusType.Interaction => MemoryType.Interaction,
            _ => MemoryType.Interest
        };
    }

   
}