using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeactivateGreenPower : MonoBehaviour
{
    public GreenHand greenHand;

    public void Deactivate()
    {
        greenHand.Deactivate();
    }
}
