using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
	[Header("Networked Objects List")]
	[Tooltip("Drag any NetworkObjects here (Doors, Props, Items) that both Host and Clients must see and sync.")]
	public List<NetworkObject> networkedObjects = new List<NetworkObject>();

	[Header("Spawn Points")]
	public Transform lobbySpawnPoint;

	public Transform[] survivorSpawnPoints;

	public Transform monsterSpawnPoint;

	[Header("TextMesh Pro UI Elements")]
	public TextMeshProUGUI timerText;

	public TextMeshProUGUI roleNotificationText;

	[Header("Host Controls")]
	public GameObject hostStartPromptUI;

	[Header("Game Settings")]
	public int gameDurationSeconds = 180;

	private NetworkVariable<int> _netTimer = new NetworkVariable<int>(180);

	private bool _gameStarted;

	private void Start()
	{
		if (timerText != null)
		{
			timerText.text = "";
		}
		if (roleNotificationText != null)
		{
			roleNotificationText.text = "";
		}
		if (hostStartPromptUI != null)
		{
			hostStartPromptUI.SetActive(value: false);
		}
	}

	public override void OnNetworkSpawn()
	{
		NetworkVariable<int> netTimer = _netTimer;
		netTimer.OnValueChanged = (NetworkVariable<int>.OnValueChangedDelegate)Delegate.Combine(netTimer.OnValueChanged, new NetworkVariable<int>.OnValueChangedDelegate(UpdateTimerUI));
		UpdateTimerUI(0, _netTimer.Value);
		if (IsServer)
		{
			_netTimer.Value = gameDurationSeconds;
			NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
			SpawnAndSyncNetworkedObjects();
		}
	}

	public override void OnNetworkDespawn()
	{
		NetworkVariable<int> netTimer = _netTimer;
		netTimer.OnValueChanged = (NetworkVariable<int>.OnValueChangedDelegate)Delegate.Remove(netTimer.OnValueChanged, new NetworkVariable<int>.OnValueChangedDelegate(UpdateTimerUI));
		if (IsServer && NetworkManager.Singleton != null)
		{
			NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
		}
	}

	private void Update()
	{
		if (!IsServer || _gameStarted)
		{
			return;
		}
		if (NetworkManager.Singleton.ConnectedClientsList.Count >= 2)
		{
			if (hostStartPromptUI != null && !hostStartPromptUI.activeSelf)
			{
				hostStartPromptUI.SetActive(value: true);
			}
			if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
			{
				if (hostStartPromptUI != null)
				{
					hostStartPromptUI.SetActive(value: false);
				}
				StartCoroutine(StartGameLoop());
			}
		}
		else if (hostStartPromptUI != null && hostStartPromptUI.activeSelf)
		{
			hostStartPromptUI.SetActive(value: false);
		}
	}

	private void OnClientConnected(ulong clientId)
	{
		if (IsServer)
		{
			SpawnAndSyncNetworkedObjects();
			if (lobbySpawnPoint != null)
			{
				ExecuteTargetedTeleport(clientId, lobbySpawnPoint.position, isMonster: false, isSurvivor: true);
			}
		}
	}

	private void SpawnAndSyncNetworkedObjects()
	{
		if (!IsServer)
		{
			return;
		}
		foreach (NetworkObject networkedObject in networkedObjects)
		{
			if (networkedObject != null && !networkedObject.IsSpawned)
			{
				networkedObject.Spawn();
			}
		}
	}

	private IEnumerator StartGameLoop()
	{
		_gameStarted = true;
		SetRoleNotificationClientRpc("Assigning Roles...");
		yield return new WaitForSeconds(3f);
		AssignRolesAndSpawn();
		while (_netTimer.Value > 0)
		{
			yield return new WaitForSeconds(1f);
			_netTimer.Value--;
		}
		EndGame();
	}

	private void AssignRolesAndSpawn()
	{
		if (!IsServer)
		{
			return;
		}
		IReadOnlyList<NetworkClient> connectedClientsList = NetworkManager.Singleton.ConnectedClientsList;
		if (connectedClientsList.Count == 0)
		{
			return;
		}
		int num = UnityEngine.Random.Range(0, connectedClientsList.Count);
		int num2 = 0;
		for (int i = 0; i < connectedClientsList.Count; i++)
		{
			ulong clientId = connectedClientsList[i].ClientId;
			if (!(connectedClientsList[i].PlayerObject == null))
			{
				if (i == num)
				{
					Vector3 targetPosition = ((monsterSpawnPoint != null) ? monsterSpawnPoint.position : transform.position);
					ExecuteTargetedTeleport(clientId, targetPosition, isMonster: true, isSurvivor: false);
					NotifySingleClientRole(clientId, "YOU ARE THE MONSTER! HUNT THE HIDERS!");
				}
				else
				{
					Vector3 targetPosition2 = ((survivorSpawnPoints != null && survivorSpawnPoints.Length != 0) ? survivorSpawnPoints[num2 % survivorSpawnPoints.Length].position : transform.position);
					ExecuteTargetedTeleport(clientId, targetPosition2, isMonster: false, isSurvivor: true);
					NotifySingleClientRole(clientId, "YOU ARE A HIDER! SURVIVE!");
					num2++;
				}
			}
		}
	}

	public void ExecuteTargetedTeleport(ulong clientId, Vector3 targetPosition, bool isMonster, bool isSurvivor)
	{
		if (IsServer && NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var value) && value.PlayerObject != null)
		{
			ulong networkObjectId = value.PlayerObject.NetworkObjectId;
			TeleportClientTargetClientRpc(targetPosition, isMonster, isSurvivor, networkObjectId, clientId);
		}
	}

	[ClientRpc]
	private void TeleportClientTargetClientRpc(Vector3 targetPosition, bool isMonster, bool isSurvivor, ulong targetNetObjectId, ulong targetOwnerId)
	{
		if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjectId, out var value) || value.OwnerClientId != targetOwnerId)
		{
			return;
		}
		DbUnityMovment component = value.GetComponent<DbUnityMovment>();
		CharacterController component2 = value.GetComponent<CharacterController>();
		Rigidbody component3 = value.GetComponent<Rigidbody>();
		if (component2 != null)
		{
			component2.enabled = false;
		}
		bool isKinematic = false;
		if (component3 != null)
		{
			isKinematic = component3.isKinematic;
			component3.isKinematic = true;
			component3.velocity = Vector3.zero;
			component3.angularVelocity = Vector3.zero;
		}
		value.transform.position = targetPosition;
		if (component3 != null)
		{
			component3.position = targetPosition;
		}
		if (component != null)
		{
			if (component.survivorModelContainer != null)
			{
				component.survivorModelContainer.SetActive(isSurvivor);
			}
			if (component.monsterModelContainer != null)
			{
				component.monsterModelContainer.SetActive(isMonster);
			}
			if (component.deadModelContainer != null)
			{
				component.deadModelContainer.SetActive(value: false);
			}
			if (value.IsOwner)
			{
				component.SetGrabpackStatus(isSurvivor);
			}
		}
		if (component3 != null)
		{
			component3.isKinematic = isKinematic;
			component3.velocity = Vector3.zero;
			component3.angularVelocity = Vector3.zero;
		}
		if (value.IsOwner && component2 != null)
		{
			component2.enabled = true;
		}
	}

	private void NotifySingleClientRole(ulong clientId, string message)
	{
		ClientRpcParams rpcParams = new ClientRpcParams
		{
			Send = new ClientRpcSendParams
			{
				TargetClientIds = new ulong[1] { clientId }
			}
		};
		SetRoleNotificationClientRpc(message, rpcParams);
	}

	[ClientRpc]
	private void SetRoleNotificationClientRpc(string text, ClientRpcParams rpcParams = default(ClientRpcParams))
	{
		if (roleNotificationText != null)
		{
			roleNotificationText.text = text;
			CancelInvoke("ClearRoleText");
			Invoke("ClearRoleText", 5f);
		}
	}

	private void ClearRoleText()
	{
		if (roleNotificationText != null)
		{
			roleNotificationText.text = "";
		}
	}

	private void UpdateTimerUI(int oldVal, int newVal)
	{
		if (!(timerText == null))
		{
			int num = newVal / 60;
			int num2 = newVal % 60;
			timerText.text = $"{num:00}:{num2:00}";
		}
	}

	private void EndGame()
	{
		if (!IsServer)
		{
			return;
		}
		_gameStarted = false;
		_netTimer.Value = gameDurationSeconds;
		foreach (NetworkClient connectedClients in NetworkManager.Singleton.ConnectedClientsList)
		{
			if (connectedClients.PlayerObject != null && lobbySpawnPoint != null)
			{
				ExecuteTargetedTeleport(connectedClients.ClientId, lobbySpawnPoint.position, isMonster: false, isSurvivor: true);
			}
		}
	}
}

