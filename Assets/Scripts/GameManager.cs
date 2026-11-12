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

	[Header("Optional AI Enemy")]
	[Tooltip("Network prefab with NetworkObject, server-authoritative NetworkTransform, NavMeshAgent and HuggyHunterAI.")]
	public NetworkObject aiMonsterPrefab;

	private NetworkObject spawnedAiMonster;

	[Header("TextMesh Pro UI Elements")]
	public TextMeshProUGUI timerText;

	public TextMeshProUGUI roleNotificationText;

	[Header("Host Controls")]
	public GameObject hostStartPromptUI;

	[Header("Game Settings")]
	public int gameDurationSeconds = 180;

	private NetworkVariable<int> _netTimer = new NetworkVariable<int>(180, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	private bool _gameStarted;

	private void OnValidate()
	{
		gameDurationSeconds = Mathf.Max(1, gameDurationSeconds);
		if (networkedObjects == null)
			networkedObjects = new List<NetworkObject>();
	}

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
		_netTimer.OnValueChanged += UpdateTimerUI;
		UpdateTimerUI(0, _netTimer.Value);

		if (!IsServer || NetworkManager.Singleton == null)
			return;

		_netTimer.Value = Mathf.Max(1, gameDurationSeconds);
		NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
		SpawnAndSyncNetworkedObjects();
	}

	public override void OnNetworkDespawn()
	{
		_netTimer.OnValueChanged -= UpdateTimerUI;
		if (IsServer && NetworkManager.Singleton != null)
		{
			NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
		}
	}

	private void Update()
	{
		if (!IsServer || _gameStarted || NetworkManager.Singleton == null)
			return;

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
		if (IsServer && NetworkManager.Singleton != null)
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
		if (!IsServer || networkedObjects == null)
			return;

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
		if (!IsServer || _gameStarted || NetworkManager.Singleton == null)
			yield break;

		_gameStarted = true;
		_netTimer.Value = Mathf.Max(1, gameDurationSeconds);
		SetRoleNotificationClientRpc("Assigning Roles...");
		yield return new WaitForSecondsRealtime(3f);
		AssignRolesAndSpawn();

		while (IsServer && _netTimer.Value > 0)
		{
			yield return new WaitForSecondsRealtime(1f);
			_netTimer.Value = Mathf.Max(0, _netTimer.Value - 1);
		}
		EndGame();
	}

	private void AssignRolesAndSpawn()
	{
		if (!IsServer || NetworkManager.Singleton == null)
			return;

		IReadOnlyList<NetworkClient> clients = NetworkManager.Singleton.ConnectedClientsList;
		if (clients.Count == 0)
			return;

		bool useAI = PlayerPrefs.GetInt("Poppy.EnemyMode", 0) == 1 &&
			aiMonsterPrefab != null && monsterSpawnPoint != null;

		if (spawnedAiMonster != null && spawnedAiMonster.IsSpawned)
			spawnedAiMonster.Despawn(true);
		spawnedAiMonster = null;

		if (useAI)
		{
			int survivorIndex = 0;
			for (int i = 0; i < clients.Count; i++)
			{
				NetworkClient client = clients[i];
				if (client.PlayerObject == null)
					continue;

				Vector3 spawn = survivorSpawnPoints != null && survivorSpawnPoints.Length > 0
					? survivorSpawnPoints[survivorIndex % survivorSpawnPoints.Length].position
					: transform.position;
				ExecuteTargetedTeleport(client.ClientId, spawn, isMonster: false, isSurvivor: true);
				NotifySingleClientRole(client.ClientId, "HUGGY IS HUNTING YOU. SURVIVE!");
				survivorIndex++;
			}

			spawnedAiMonster = Instantiate(aiMonsterPrefab, monsterSpawnPoint.position, monsterSpawnPoint.rotation);
			spawnedAiMonster.Spawn();
			return;
		}

		if (PlayerPrefs.GetInt("Poppy.EnemyMode", 0) == 1 && aiMonsterPrefab == null)
			Debug.LogWarning("AI mode was selected, but GameManager.aiMonsterPrefab is not assigned. Falling back to player-controlled monster.", this);

		int monsterIndex = UnityEngine.Random.Range(0, clients.Count);
		int survivorSpawnIndex = 0;
		for (int i = 0; i < clients.Count; i++)
		{
			NetworkClient client = clients[i];
			if (client.PlayerObject == null)
				continue;

			if (i == monsterIndex)
			{
				Vector3 spawn = monsterSpawnPoint != null ? monsterSpawnPoint.position : transform.position;
				ExecuteTargetedTeleport(client.ClientId, spawn, isMonster: true, isSurvivor: false);
				NotifySingleClientRole(client.ClientId, "YOU ARE THE MONSTER! HUNT THE HIDERS!");
			}
			else
			{
				Vector3 spawn = survivorSpawnPoints != null && survivorSpawnPoints.Length > 0
					? survivorSpawnPoints[survivorSpawnIndex % survivorSpawnPoints.Length].position
					: transform.position;
				ExecuteTargetedTeleport(client.ClientId, spawn, isMonster: false, isSurvivor: true);
				NotifySingleClientRole(client.ClientId, "YOU ARE A HIDER! SURVIVE!");
				survivorSpawnIndex++;
			}
		}
	}

	public void ExecuteTargetedTeleport(ulong clientId, Vector3 targetPosition, bool isMonster, bool isSurvivor)
	{
		if (IsServer && NetworkManager.Singleton != null &&
			NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var value) &&
			value.PlayerObject != null)
		{
			ulong networkObjectId = value.PlayerObject.NetworkObjectId;
			TeleportClientTargetClientRpc(targetPosition, isMonster, isSurvivor, networkObjectId, clientId);
		}
	}

	[ClientRpc]
	private void TeleportClientTargetClientRpc(Vector3 targetPosition, bool isMonster, bool isSurvivor, ulong targetNetObjectId, ulong targetOwnerId)
	{
		if (NetworkManager.Singleton == null ||
			!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjectId, out var value) ||
			value.OwnerClientId != targetOwnerId)
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

	[ClientRpc]
	private void GrantMatchCreditsClientRpc(int amount)
	{
		PoppyShop.AddCredits(amount);
	}

	private void EndGame()
	{
		if (!IsServer || NetworkManager.Singleton == null)
			return;

		_gameStarted = false;
		GrantMatchCreditsClientRpc(100);
		if (spawnedAiMonster != null && spawnedAiMonster.IsSpawned)
			spawnedAiMonster.Despawn(true);
		spawnedAiMonster = null;
		_netTimer.Value = Mathf.Max(1, gameDurationSeconds);
		foreach (NetworkClient connectedClients in NetworkManager.Singleton.ConnectedClientsList)
		{
			if (connectedClients.PlayerObject != null && lobbySpawnPoint != null)
			{
				ExecuteTargetedTeleport(connectedClients.ClientId, lobbySpawnPoint.position, isMonster: false, isSurvivor: true);
			}
		}
	}
}

