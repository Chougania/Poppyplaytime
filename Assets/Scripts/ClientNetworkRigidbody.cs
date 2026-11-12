using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkTransform))]
[AddComponentMenu("Netcode/Client Network Rigidbody")]
public class ClientNetworkRigidbody : NetworkBehaviour
{
	private Rigidbody _rigidbody;

	private NetworkTransform _networkTransform;

	private void Awake()
	{
		_rigidbody = GetComponent<Rigidbody>();
		_networkTransform = GetComponent<NetworkTransform>();
	}

	private void Update()
	{
		if (!IsOwner)
		{
			if (!_rigidbody.isKinematic)
			{
				_rigidbody.isKinematic = true;
			}
		}
		else if (_rigidbody.isKinematic)
		{
			_rigidbody.isKinematic = false;
		}
	}

	public void RequestOwnership(ulong gainingClientClientId)
	{
		if (IsServer)
		{
			NetworkObject.ChangeOwnership(gainingClientClientId);
		}
	}
}

