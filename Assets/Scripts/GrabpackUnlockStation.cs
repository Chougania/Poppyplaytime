using System.Collections.Generic;
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

    private readonly HashSet<ulong> playersWhoUsedStation = new HashSet<ulong>();

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        NetworkObject playerObject = other.GetComponentInParent<NetworkObject>();
        if (playerObject == null || !playerObject.IsSpawned ||
            NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        ulong playerId = playerObject.OwnerClientId;
        if (oneUse && playersWhoUsedStation.Contains(playerId))
            return;

        GrabpackProgression progression = playerObject.GetComponent<GrabpackProgression>();
        if (progression == null)
            return;

        if (progression.UnlockAbility(abilityToUnlock))
        {
            playersWhoUsedStation.Add(playerId);
            onUnlocked?.Invoke();
            Debug.Log("GrabPack débloqué pour le joueur " + playerId + " : " + abilityToUnlock, this);
        }
    }

    // A station can be used again in the next round.
    public void ResetForNewMatch()
    {
        playersWhoUsedStation.Clear();
    }
}
