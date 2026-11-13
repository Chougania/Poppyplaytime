using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreenPowerSource : MonoBehaviour
{

    public AudioSource globalAudio;
    public AudioClip grabsfx;
    private bool played = false;

    public ParticleSystem grabparticles;

    void Update()
    {
        Transform greenHand = null;

        foreach (Transform child in transform)
        {
            if (child.name == "Hand_Green")
            {
                greenHand = child;
                break;
            }
        }

        if (greenHand != null)
        {
            GreenHand Greenhand = greenHand.GetComponent<GreenHand>();
            Greenhand.Power();

            if (!played)
            {
                if (grabparticles != null)
                    grabparticles.Play();

                globalAudio.PlayOneShot(grabsfx, 3.0f);
                played = true;
            }
        }
        else
        {
            played = false;
        }
    }

    public void RejectHand()
    {
        LaunchHand[] hands = GetComponentsInChildren<LaunchHand>();

        foreach (LaunchHand hand in hands)
        {
            hand.return1();
        }
    }
}