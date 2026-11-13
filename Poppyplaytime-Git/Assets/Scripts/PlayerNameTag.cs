using System;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNameTag : NetworkBehaviour
{
	[Header("3D Text Reference")]
	public TextMeshPro nameTextMesh;

	[Header("Positioning")]
	[SerializeField]
	private Vector3 offset = new Vector3(0f, 1f, 0f);

	private readonly NetworkVariable<FixedString32Bytes> networkName = new NetworkVariable<FixedString32Bytes>("Player");

	public override void OnNetworkSpawn()
	{
		NetworkVariable<FixedString32Bytes> networkVariable = networkName;
		networkVariable.OnValueChanged = (NetworkVariable<FixedString32Bytes>.OnValueChangedDelegate)Delegate.Combine(networkVariable.OnValueChanged, new NetworkVariable<FixedString32Bytes>.OnValueChangedDelegate(OnNameChanged));
		nameTextMesh.text = networkName.Value.ToString();
		if (IsOwner)
		{
			string playerName = ConnectionManafer.PlayerName;
			SetPlayerNameServerRpc(playerName);
		}
	}

	public override void OnNetworkDespawn()
	{
		NetworkVariable<FixedString32Bytes> networkVariable = networkName;
		networkVariable.OnValueChanged = (NetworkVariable<FixedString32Bytes>.OnValueChangedDelegate)Delegate.Remove(networkVariable.OnValueChanged, new NetworkVariable<FixedString32Bytes>.OnValueChangedDelegate(OnNameChanged));
	}

	private void OnNameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
	{
		nameTextMesh.text = newValue.ToString();
	}

	[ServerRpc]
	private void SetPlayerNameServerRpc(string newName)
	{
		networkName.Value = newName;
	}

	private void LateUpdate()
	{
		if (!(nameTextMesh == null))
		{
			Camera main = Camera.main;
			if (main != null)
			{
				nameTextMesh.transform.position = main.transform.position + main.transform.rotation * offset;
				nameTextMesh.transform.LookAt(nameTextMesh.transform.position + main.transform.rotation * Vector3.forward, main.transform.rotation * Vector3.up);
			}
		}
	}
}

