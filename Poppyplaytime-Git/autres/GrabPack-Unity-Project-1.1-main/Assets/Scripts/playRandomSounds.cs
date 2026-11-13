using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playRandomSounds : MonoBehaviour
{

    public AudioClip[] footsteps;
    private int max;
    private int selected;

    public AudioSource audiosource;

    void Start()
    {
        max = footsteps.Length;
    }

    public void OnFootstep(AnimationEvent evt)
    {

        if (evt.animationState.weight < .5f)
        {
            return;
        }

        PlaySound();
    }


    public void PlaySound()
    {
        selected = Random.Range(0, max);

        audiosource.PlayOneShot(footsteps[selected], 1.0f);
    }

    public void StopSounds()
    {
        audiosource.Stop();
    }
}
