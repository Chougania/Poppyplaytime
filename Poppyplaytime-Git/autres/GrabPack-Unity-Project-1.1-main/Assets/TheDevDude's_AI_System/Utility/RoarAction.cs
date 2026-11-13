using UnityEngine;

public class RoarAction : UtilityAction
{
    [SerializeField] private float roarDistance = 5f;

    public override float Score()
    {
        if (brain.Blackboard.Get<bool>(AIKeys.HasRoared))
            return 0f;

        if (!brain.Blackboard.Get<bool>(AIKeys.CanSeeTarget))
            return 0f;

        Transform target = brain.Blackboard.Get<Transform>(AIKeys.Target);

        if (target == null)
            return 0f;

        if (!brain.Blackboard.Get<bool>(AIKeys.ShouldRoar))
            return 0f;

        return 200f;
    }
}