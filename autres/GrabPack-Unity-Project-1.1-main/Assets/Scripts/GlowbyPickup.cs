using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlowbyPickup : MonoBehaviour
{
    public GameObject Glowby;

    public void Pickup()
    {
        gameObject.SetActive(false);
        Glowby.SetActive(true);
    }
}
