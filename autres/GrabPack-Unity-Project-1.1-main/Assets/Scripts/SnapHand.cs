using UnityEngine;

public class SnapHand : MonoBehaviour
{
    public Transform righttarg;
    public Transform lefttarg;

    public Transform GetSnapTarget(string hand)
    {
        if (hand == "Right" && righttarg != null) return righttarg;
        if (hand == "Left" && lefttarg != null) return lefttarg;
        return righttarg ?? lefttarg;
    }
}