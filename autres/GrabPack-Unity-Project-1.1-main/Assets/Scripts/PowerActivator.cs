using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class PowerActivator : MonoBehaviour
{
    public bool startPowered;

    [SerializeField] private List<MonoBehaviour> scriptsToEnable;

    [SerializeField] private List<GameObject> objectsToEnable;

    [SerializeField] private List<GameObject> objectsToDisable;

    [SerializeField] private List<Animator> animators;
    [SerializeField] private string animationBool = "open";

    [SerializeField] private AudioClip activationSound;

    public UnityEvent OnActivate;
    public UnityEvent OnDeactivate;

    private bool powered;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        SetPower(startPowered);
    }

    public void Activate()
    {
        SetPower(true);
    }

    public void Deactivate()
    {
        SetPower(false);
    }

    public void Toggle()
    {
        SetPower(!powered);
    }

    private void SetPower(bool state)
    {
        if (powered == state) return; 

        powered = state;

        foreach (MonoBehaviour script in scriptsToEnable)
        {
            if (script != null)
                script.enabled = powered;
        }

        foreach (GameObject obj in objectsToEnable)
        {
            if (obj != null)
                obj.SetActive(powered);
        }

        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null)
                obj.SetActive(!powered);
        }

        foreach (Animator anim in animators)
        {
            if (anim != null)
                anim.SetBool(animationBool, powered);
        }

        if (powered && activationSound != null)
            audioSource.PlayOneShot(activationSound);

        if (powered)
        {
            OnActivate?.Invoke();
        }
        else
        {
            OnDeactivate?.Invoke(); 
        }
    }
}