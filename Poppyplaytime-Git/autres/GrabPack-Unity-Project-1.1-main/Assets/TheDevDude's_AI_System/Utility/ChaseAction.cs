using UnityEngine;

public class ChaseAction : UtilityAction
{
    [SerializeField] private float chasePersistence = 3f;

    private float loseTargetTime = -1f;

    public override float Score()
    {
        bool hasTarget = brain.Blackboard.Get<Transform>(AIKeys.Target) != null;
        bool canSee = brain.Blackboard.Get<bool>(AIKeys.CanSeeTarget);

        if (canSee)
        {
            loseTargetTime = -1f;
            return 100f;
        }

        if (hasTarget)
        {
            if (loseTargetTime < 0f)
                loseTargetTime = Time.time;

            if (Time.time - loseTargetTime < chasePersistence)
                return 100f;

            brain.Blackboard.Set<Transform>(AIKeys.Target, null);
            brain.Blackboard.Set(AIKeys.HasRoared, false);
            brain.Blackboard.Set(AIKeys.ShouldRoar, false);
            loseTargetTime = -1f;
        }

        if (brain.Blackboard.Get<bool>(AIKeys.ForceChase))
            return 100f;

        return brain.Blackboard.Get<Transform>(AIKeys.Target) != null ? 100f : 0f;

        return 0f;
    }
}