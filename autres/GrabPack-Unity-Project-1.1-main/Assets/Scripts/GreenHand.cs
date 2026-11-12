using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreenHand : MonoBehaviour
{
    public GameObject Particles;

    public SkinnedMeshRenderer handRenderer;

    public Material poweredMaterial;
    public Material normalMaterial;

    public bool isPowered = false;


    public void Power()
    {
        handRenderer.material = poweredMaterial;
        Particles.SetActive(true);
        isPowered = true;
    }

    public void Deactivate()
    {
        handRenderer.material = normalMaterial;
        Particles.SetActive(false);
        isPowered = false;
    }

    void OnDisable()
    {
        Deactivate();
    }
}
