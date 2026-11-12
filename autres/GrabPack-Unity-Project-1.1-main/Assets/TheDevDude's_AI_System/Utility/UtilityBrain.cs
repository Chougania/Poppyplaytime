using UnityEngine;

public class UtilityBrain : MonoBehaviour
{
    private UtilityAction[] actions;
    private UtilityAction currentAction;

    [SerializeField] private UtilityAction startingAction;

    public void Initialize(AIBrain brain)
    {
        actions = GetComponents<UtilityAction>();

        foreach (UtilityAction action in actions)
        {
            action.Initialize(brain);
        }

    }

    public void StartBrain()
    {
        if (startingAction != null)
        {
            currentAction = startingAction;

            if (currentAction.Task is AmbushTask ambush)
            {
                ambush.SkipAmbushPointSelection = true;
            }

            currentAction.Task.Begin();
        }
    }

    public void Tick()
    {
        UtilityAction bestAction = null;
        float bestScore = float.MinValue;

        foreach (UtilityAction action in actions)
        {
            float score = action.Score();

            if (score > bestScore)
            {
                bestScore = score;
                bestAction = action;
            }
        }

        if (bestAction == null)
            return;

        if (bestAction != currentAction)
        {
            if (currentAction != null &&
                !currentAction.Task.CanInterrupt &&
                !currentAction.Task.IsComplete())
            {
                currentAction.Task.Tick();
                return;
            }

            currentAction?.Task.End();

            currentAction = bestAction;

            currentAction.Task.Begin();
        }

        currentAction.Task.Tick();
    }
}