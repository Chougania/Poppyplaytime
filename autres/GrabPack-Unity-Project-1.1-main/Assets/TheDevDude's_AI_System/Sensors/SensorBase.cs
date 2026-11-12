using UnityEngine;

public abstract class SensorBase : MonoBehaviour
{
    protected Blackboard blackboard;
    protected MemorySystem memorySystem;

    public virtual void Initialize(Blackboard blackboard, MemorySystem memorySystem)
    {
        this.blackboard = blackboard;
        this.memorySystem = memorySystem;
    }

    public abstract void Sense();
}