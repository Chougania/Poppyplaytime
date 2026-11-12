using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Glowby : MonoBehaviour
{
    private Animator anim;

    public float tapThreshold = 0.3f;
    private float lastTapTime = -1f;

    private enum LightMode { Off, Flashlight, Blacklight }
    private LightMode currentMode = LightMode.Off;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            CycleMode();
        }
    }

    void SetMode(LightMode mode)
    {
        currentMode = mode;

        anim.SetBool("flashlight", mode == LightMode.Flashlight);
        anim.SetBool("blacklight", mode == LightMode.Blacklight);
    }

    public void CycleMode()
    {
        switch (currentMode)
        {
            case LightMode.Off:
                SetMode(LightMode.Flashlight);
                break;

            case LightMode.Flashlight:
                SetMode(LightMode.Blacklight);
                break;

            case LightMode.Blacklight:
                SetMode(LightMode.Off);
                break;
        }
    }
}