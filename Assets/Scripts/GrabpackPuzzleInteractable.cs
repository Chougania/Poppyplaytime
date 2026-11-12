using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class GrabpackPuzzleInteractable : NetworkBehaviour
{
    [Header("Puzzle requirement")]
    public GrabpackAbility requiredAbility = GrabpackAbility.Green;
    [Min(0.5f)] public float maxUseDistance = 4.5f;
    public bool oneShot = true;

    [Header("Feedback")]
    public GameObject activatedVisual;
    public UnityEvent onActivated;
    public UnityEvent onReset;

    private readonly NetworkVariable<bool> isActivated = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsActivated => isActivated.Value;

    public override void OnNetworkSpawn()
    {
        isActivated.OnValueChanged += OnActivationChanged;
        ApplyActivationVisual();
    }

    public override void OnNetworkDespawn()
    {
        isActivated.OnValueChanged -= OnActivationChanged;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TryActivateServerRpc(ServerRpcParams rpcParams = default)
    {
        if (oneShot && isActivated.Value)
            return;

        ulong sender = rpcParams.Receive.SenderClientId;
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.ConnectedClients.TryGetValue(sender, out NetworkClient client) ||
            client.PlayerObject == null)
            return;

        GrabpackProgression progression = client.PlayerObject.GetComponent<GrabpackProgression>();
        if (progression == null || !progression.HasAbility(requiredAbility))
            return;

        Vector3 playerPosition = client.PlayerObject.transform.position;
        if ((playerPosition - transform.position).sqrMagnitude > maxUseDistance * maxUseDistance)
            return;

        isActivated.Value = true;
        onActivated?.Invoke();
    }

    public void ResetForNewMatch()
    {
        if (!IsServer)
            return;

        isActivated.Value = false;
        onReset?.Invoke();
    }

    private void OnActivationChanged(bool previous, bool current)
    {
        ApplyActivationVisual();
    }

    private void ApplyActivationVisual()
    {
        if (activatedVisual != null)
            activatedVisual.SetActive(isActivated.Value);
    }
}
