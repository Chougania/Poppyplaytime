using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(AudioSource))]
public class magnetHand : MonoBehaviour
{
    public MeshRenderer renderer;
    public SkinnedMeshRenderer handrenderer;

    public Material positive;
    public Material negative;

    public Material positiveHand;
    public Material negativeHand;

    public bool postiveforce = true;

    public ParticleSystem blastparticles;

    public AudioClip blastsfx;
    private AudioSource audioSource;
    void Start()
    {
        UpdateVisuals();
        audioSource = GetComponent<AudioSource>();

    }

    public void TogglePolarity()
    {
        postiveforce = !postiveforce;
        blastparticles.Play();
        audioSource.PlayOneShot(blastsfx, 1.0f);
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (postiveforce)
        {
            renderer.material = positive;
            handrenderer.material = positiveHand;

        }
        if (!postiveforce)
        {
            renderer.material = negative;
            handrenderer.material = negativeHand;

        }
    }


}
