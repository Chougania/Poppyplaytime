using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkTransform))]
[AddComponentMenu("Netcode/Client Network Rigidbody")]
public class ClientNetworkRigidbody : NetworkBehaviour
{
	private Rigidbody _rigidbody;

	private void Awake()
	{
		_rigidbody = GetComponent<Rigidbody>();
	}

	public override void OnNetworkSpawn()
	{
		ApplySimulationAuthority();
	}

	public override void OnGainedOwnership()
	{
		ApplySimulationAuthority();
	}

	public override void OnLostOwnership()
	{
		ApplySimulationAuthority();
	}

	private void ApplySimulationAuthority()
	{
		if (!IsSpawned || _rigidbody == null)
			return;

		// Only the owning peer simulates this Rigidbody; NetworkTransform replicates it.
		_rigidbody.isKinematic = !IsOwner;
	}

	public void RequestOwnership(ulong gainingClientClientId)
	{
		if (!IsServer || !IsSpawned || NetworkManager.Singleton == null)
			return;

		if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(gainingClientClientId))
		{
			Debug.LogWarning($"Cannot transfer object ownership: client {gainingClientClientId} is not connected.", this);
			return;
		}

		NetworkObject.ChangeOwnership(gainingClientClientId);
	}
}

