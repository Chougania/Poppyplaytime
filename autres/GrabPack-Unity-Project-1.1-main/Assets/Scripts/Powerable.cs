using UnityEngine;

public class Powerable : MonoBehaviour
{
    public bool requiresPower = true;

    private bool isPowered = false;

    public bool IsPowered()
    {
        if (!requiresPower)
            return true;

        return isPowered;
    }

    public void SetPower(bool state)
    {
        isPowered = state;
    }
}