using UnityEngine;

public class InvestigateAction : UtilityAction
{
    public override float Score()
    {
        if (brain.Blackboard.Get<bool>(AIKeys.CanSeeTarget))
            return 0f;

        Memory memory = brain.MemorySystem.GetMemory(
            MemorySubject.Player,
            MemoryType.Vision);

        if (memory == null)
        {
            memory = brain.MemorySystem.GetMemory(
                MemorySubject.Player,
                MemoryType.Sound);
        }

        if (memory == null)
            return 0f;

        return memory.Confidence * 50f;
    }
}