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
	private NetworkVariable<bool> _netGameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private NetworkList<ulong> _readyClients = new NetworkList<ulong>();
	private readonly HashSet<ulong> _eliminatedClients = new HashSet<ulong>();

	private bool _gameStarted;

	public bool IsGameStarted => _netGameStarted.Value;
	public bool IsClientReady(ulong clientId) => _readyClients.Contains(clientId);
	public bool IsClientEliminated(ulong clientId) => _eliminatedClients.Contains(clientId);

	public bool CanStartMatch
	{
		get
		{
			if (!IsServer || NetworkManager.Singleton == null || _gameStarted)
				return false;

			int count = NetworkManager.Singleton.ConnectedClientsList.Count;
			bool useAI = PlayerPrefs.GetInt("Poppy.EnemyMode", 0) == 1;
			bool validMode = useAI
				? aiMonsterPrefab != null && monsterSpawnPoint != null && count >= 1
				: count >= 2;

			if (!validMode)
				return false;

			foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
			{
				if (!_readyClients.Contains(client.ClientId))
					return false;
			}
			return true;
		}
	}

	public void StartMatchFromLobby()
	{
		if (!CanStartMatch)
			return;

		if (hostStartPromptUI != null)
			hostStartPromptUI.SetActive(false);
		StartCoroutine(StartGameLoop());
	}

	public void RequestReturnToLobby()
	{
		if (!IsSpawned)
			return;

		if (IsServer)
		{
			if (_gameStarted)
				EndGame(grantRewards: false);
		}
		else
		{
			RequestReturnToLobbyServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void RequestReturnToLobbyServerRpc(ServerRpcParams rpcParams = default(ServerRpcParams))
	{
		if (!IsServer || !_gameStarted || NetworkManager.Singleton == null)
			return;

		// Returning to the waiting room ends the current shared round for everyone.
		EndGame(grantRewards: false);
	}

	public void SetLocalReady(bool ready)
	{
		if (IsSpawned)
			SetReadyServerRpc(ready);
	}

	[ServerRpc(RequireOwnership = false)]
	private void SetReadyServerRpc(bool ready, ServerRpcParams rpcParams = default(ServerRpcParams))
	{
		if (!IsServer || NetworkManager.Singleton == null)
			return;

		ulong sender = rpcParams.Receive.SenderClientId;
		if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(sender))
			return;

		bool currentlyReady = _readyClients.Contains(sender);
		if (ready && !currentlyReady)
			_readyClients.Add(sender);
		else if (!ready && currentlyReady)
			_readyClients.Remove(sender);
	}

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
		NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
		_readyClients.Clear();
		SpawnAndSyncNetworkedObjects();
	}

	public override void OnNetworkDespawn()
	{
		_netTimer.OnValueChanged -= UpdateTimerUI;
		if (IsServer && NetworkManager.Singleton != null)
		{
			NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
			NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
		}
	}

	private void Update()
	{
		if (!IsServer || NetworkManager.Singleton == null)
			return;

		if (_gameStarted)
		{
			if (PlayerPrefs.GetInt("Poppy.EnemyMode", 0) == 1 &&
				NetworkManager.Singleton.ConnectedClientsList.Count > 0 &&
				_eliminatedClients.Count >= NetworkManager.Singleton.ConnectedClientsList.Count)
			{
				EndGame();
			}
			return;
		}

		bool canStartMatch = CanStartMatch;

		if (canStartMatch)
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

	private void OnClientDisconnected(ulong clientId)
	{
		if (IsServer && _readyClients.Contains(clientId))
			_readyClients.Remove(clientId);
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
		_netGameStarted.Value = true;
		_eliminatedClients.Clear();
		ResetRoundProgression();
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

		List<NetworkClient> clients = new List<NetworkClient>();
		foreach (NetworkClient connectedClient in NetworkManager.Singleton.ConnectedClientsList)
		{
			if (connectedClient != null && connectedClient.PlayerObject != null)
				clients.Add(connectedClient);
		}
		if (clients.Count == 0)
			return;

		bool useAI = PlayerPrefs.GetInt("Poppy.EnemyMode", 0) == 1;
		if (useAI && (aiMonsterPrefab == null || monsterSpawnPoint == null))
		{
			Debug.LogError("AI mode cannot start: assign aiMonsterPrefab and monsterSpawnPoint in GameManager.", this);
			_gameStarted = false;
			_netGameStarted.Value = false;
			return;
		}

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
		if (IsServer)
		{
			if (!isSurvivor && !isMonster)
				_eliminatedClients.Add(clientId);
			else if (isSurvivor)
				_eliminatedClients.Remove(clientId);
		}

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
			component.RefreshSkinColor();
			if (component.deadModelContainer != null)
			{
				component.deadModelContainer.SetActive(value: false);
			}
			component.SetEliminatedState(!isSurvivor && !isMonster, isSurvivor);
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

	[ClientRpc]
	private void GrantMatchTicketsClientRpc(int amount)
	{
		PoppyShop.AddTickets(amount);
	}

	private void ResetRoundProgression()
	{
		if (!IsServer || NetworkManager.Singleton == null)
			return;

		foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
		{
			if (client.PlayerObject == null)
				continue;

			GrabpackProgression progression = client.PlayerObject.GetComponent<GrabpackProgression>();
			if (progression != null)
				progression.ResetForNewMatch();
		}

		GrabpackUnlockStation[] stations = FindObjectsOfType<GrabpackUnlockStation>(true);
		for (int i = 0; i < stations.Length; i++)
			stations[i].ResetForNewMatch();

		GrabpackPuzzleInteractable[] puzzles = FindObjectsOfType<GrabpackPuzzleInteractable>(true);
		for (int i = 0; i < puzzles.Length; i++)
			puzzles[i].ResetForNewMatch();
	}

	private void EndGame(bool grantRewards = true)
	{
		if (!IsServer || NetworkManager.Singleton == null)
			return;

		_gameStarted = false;
		_netGameStarted.Value = false;
		_eliminatedClients.Clear();
		_readyClients.Clear();
		ResetRoundProgression();
		if (grantRewards)
		{
			GrantMatchCreditsClientRpc(100);
			GrantMatchTicketsClientRpc(1);
		}
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

