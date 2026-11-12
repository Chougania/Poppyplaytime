using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class GrabpackUnlockStation : MonoBehaviour
{
    [Header("Unlock")]
    public GrabpackAbility abilityToUnlock = GrabpackAbility.Green;
    public bool oneUse = true;
    public UnityEvent onUnlocked;

    private bool consumed;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        NetworkObject playerObject = other.GetComponentInParent<NetworkObject>();
        if (playerObject == null || !playerObject.IsSpawned || !playerObject.IsServer)
            return;

        if (oneUse && consumed)
            return;

        GrabpackProgression progression = playerObject.GetComponent<GrabpackProgression>();
        if (progression == null)
            return;

        if (progression.UnlockAbility(abilityToUnlock))
        {
            consumed = true;
            onUnlocked?.Invoke();
            Debug.Log("GrabPack débloqué : " + abilityToUnlock, this);
        }
    }
}
