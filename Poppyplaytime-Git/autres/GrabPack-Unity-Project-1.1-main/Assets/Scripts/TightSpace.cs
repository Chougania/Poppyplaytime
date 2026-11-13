using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TightSpace : MonoBehaviour
{

    public RigidboyPlayerController player;

    public AudioSource audiosource;
    public AudioClip squeezeStart;
    public AudioClip squeezeEnd;

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb == null) return;

            RetractAndDisableAllHands();  

            player.playeranimations.SetBool("squeeze", true);
            player.squeeze = true;
            audiosource.PlayOneShot(squeezeStart, 1.0f);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb == null) return;

            EnableAllHands();  

            player.squeeze = false;
            player.playeranimations.SetBool("squeeze", false);
            audiosource.PlayOneShot(squeezeEnd, 1.0f);

        }
    }

    void RetractAndDisableAllHands()
    {
        LaunchHand[] hands = FindObjectsOfType<LaunchHand>();

        foreach (LaunchHand hand in hands)
        {
            if (hand != null)
            {
                hand.ForceImmediateReturn();
                hand.enabled = false;
            }
        }
    }

    void EnableAllHands()
    {
        LaunchHand[] hands = FindObjectsOfType<LaunchHand>();

        foreach (LaunchHand hand in hands)
        {
            if (hand != null)
            {
                hand.enabled = true;
            }
        }
    }
}
