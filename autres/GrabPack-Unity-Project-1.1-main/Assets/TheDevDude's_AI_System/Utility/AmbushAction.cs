using UnityEngine;

public class AmbushAction : UtilityAction
{
    public override float Score()
    {
       // Debug.Log(brain.Blackboard.Get<bool>(AIKeys.IsAmbushing));

        return brain.Blackboard.Get<bool>(AIKeys.IsAmbushing) ? 120f : 0f;

    }
}