using TMPro;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
public class PlayerNameTag : NetworkBehaviour
{
	[Header("3D Text Reference")]
	public TextMeshPro nameTextMesh;

	[Header("Positioning")]
	[SerializeField]
	private Vector3 offset = new Vector3(0f, 1f, 0f);

	private readonly NetworkVariable<FixedString32Bytes> networkName = new NetworkVariable<FixedString32Bytes>(new FixedString32Bytes("Player"), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	public override void OnNetworkSpawn()
	{
		networkName.OnValueChanged += OnNameChanged;
		RefreshName();

		if (IsOwner)
		{
			string playerName = string.IsNullOrWhiteSpace(ConnectionManafer.PlayerName)
				? "Player"
				: ConnectionManafer.PlayerName.Trim();

			if (playerName.Length > 20)
				playerName = playerName.Substring(0, 20);

			SetPlayerNameServerRpc(playerName);
		}
	}

	public override void OnNetworkDespawn()
	{
		networkName.OnValueChanged -= OnNameChanged;
	}

	private void OnNameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
	{
		RefreshName();
	}

	private void RefreshName()
	{
		if (nameTextMesh != null)
			nameTextMesh.text = networkName.Value.ToString();
	}

	[ServerRpc]
	private void SetPlayerNameServerRpc(string newName)
	{
		if (!IsServer)
			return;

		string safeName = string.IsNullOrWhiteSpace(newName) ? "Player" : newName.Trim();
		if (safeName.Length > 20)
			safeName = safeName.Substring(0, 20);

		networkName.Value = new FixedString32Bytes(safeName);
	}

	private void LateUpdate()
	{
		if (!(nameTextMesh == null))
		{
			nameTextMesh.transform.position = transform.position + offset;

			Camera main = Camera.main;
			if (main != null)
			{
				Vector3 direction = nameTextMesh.transform.position - main.transform.position;
				if (direction.sqrMagnitude > 0.0001f)
					nameTextMesh.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
			}
		}
	}
}

