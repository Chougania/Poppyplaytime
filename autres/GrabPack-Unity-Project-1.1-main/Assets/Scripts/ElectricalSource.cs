using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ElectricalSource : MonoBehaviour
{
    [SerializeField] private AudioClip powerOnSFX;

    public bool Powering { get; private set; }

    private AudioSource audioSource;
    private CableMaterials cableMaterials;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        cableMaterials = FindAnyObjectByType<CableMaterials>(FindObjectsInactive.Include);
        CheckPowerState();
    }

    void OnTransformChildrenChanged()
    {
        CheckPowerState();
    }

    void CheckPowerState()
    {
        bool shouldBePowered = transform.childCount > 6;

        if (shouldBePowered == Powering)
            return;

        Powering = shouldBePowered;

        if (cableMaterials != null)
            cableMaterials.SetPowered(Powering);

        if (Powering && powerOnSFX != null)
            audioSource.PlayOneShot(powerOnSFX);
    }
}