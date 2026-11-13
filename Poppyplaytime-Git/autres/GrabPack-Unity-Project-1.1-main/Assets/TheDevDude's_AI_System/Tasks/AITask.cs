using UnityEngine;

public abstract class AITask : MonoBehaviour
{
    protected AIBrain brain;

    public bool IsRunning { get; private set; }

    public virtual bool CanInterrupt => true;
    private bool completed;

    public virtual void Initialize(AIBrain brain)
    {
        this.brain = brain;
    }

    public virtual void Begin()
    {
        IsRunning = true;
        completed = false;
    }

    public virtual void Tick()
    {
    }

    public virtual void End()
    {
        IsRunning = false;
    }

    protected void Complete()
    {
        completed = true;
    }

    public virtual bool IsComplete()
    {
        return completed;
    }
}