using UnityEngine;

public abstract class UtilityAction : MonoBehaviour
{
    [SerializeField]
    private AITask task;

    protected AIBrain brain;

    public AITask Task => task;

    public virtual void Initialize(AIBrain brain)
    {
        this.brain = brain;

        if (task != null)
            task.Initialize(brain);
    }


    public abstract float Score();
}