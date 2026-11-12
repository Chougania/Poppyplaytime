using Unity.Netcode;
using UnityEngine;

public class ClientObjectHandler : NetworkBehaviour
{
	public override void OnNetworkSpawn()
	{
		if (IsOwner)
		{
			Debug.Log("I own this object!");
			SetupLocalPlayer();
		}
		else
		{
			DisableRemoteComponents();
		}
	}

	private void SetupLocalPlayer()
	{
	}

	private void DisableRemoteComponents()
	{
		Camera componentInChildren = GetComponentInChildren<Camera>();
		if (componentInChildren != null)
		{
			componentInChildren.enabled = false;
		}
		AudioListener componentInChildren2 = GetComponentInChildren<AudioListener>();
		if (componentInChildren2 != null)
		{
			componentInChildren2.enabled = false;
		}
	}

	private void Update()
	{
		_ = IsOwner;
	}
}

