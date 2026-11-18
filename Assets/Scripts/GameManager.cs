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

	[Tooltip("Point nommé exactement « Player spawn » : tous les survivants y apparaissent si renseigné.")]
	public Transform playerSpawnPoint;

	[Tooltip("Point nommé exactement « Player Monster spawn » : le joueur contrôlant le monstre y apparaît.")]
	public Transform playerMonsterSpawnPoint;

	[Tooltip("Anciens points de spawn des survivants (utilisés en secours si Player spawn est absent).") ]
	public Transform[] survivorSpawnPoints;

	[Tooltip("Ancien point de spawn du monstre (utilisé en secours si Player Monster spawn est absent).") ]
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

	[Tooltip("Durée d'une manche en minutes (remplace l'ancien réglage en secondes).")]
	[Min(1)] public int roundDurationMinutes = 10;

	private int RoundSeconds
	{
		get { return Mathf.Max(60, roundDurationMinutes * 60); }
	}

	public int RoundTimeLeft
	{
		get { return _netTimer.Value; }
	}

	private NetworkVariable<int> _netTimer = new NetworkVariable<int>(180, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private NetworkVariable<bool> _netGameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private NetworkList<ulong> _readyClients = new NetworkList<ulong>();
	private readonly HashSet<ulong> _eliminatedClients = new HashSet<ulong>();

	private bool _gameStarted;
	private int _roundId;

	// ---- objectifs : générateurs à réparer puis sortie
	private readonly NetworkVariable<int> _genTotal = new NetworkVariable<int>(0);
	private readonly NetworkVariable<int> _genDone = new NetworkVariable<int>(0);
	private readonly NetworkVariable<bool> _exitOpen = new NetworkVariable<bool>(false);
	private readonly NetworkVariable<int> _genSolved = new NetworkVariable<int>(0);
	private readonly NetworkVariable<int> _genCompletedMask = new NetworkVariable<int>(0);
	private readonly NetworkVariable<float> _g0 = new NetworkVariable<float>(0f);
	private readonly NetworkVariable<float> _g1 = new NetworkVariable<float>(0f);
	private readonly NetworkVariable<float> _g2 = new NetworkVariable<float>(0f);
	private readonly NetworkVariable<float> _g3 = new NetworkVariable<float>(0f);
	private readonly NetworkVariable<Vector3> _p0 = new NetworkVariable<Vector3>(Vector3.zero);
	private readonly NetworkVariable<Vector3> _p1 = new NetworkVariable<Vector3>(Vector3.zero);
	private readonly NetworkVariable<Vector3> _p2 = new NetworkVariable<Vector3>(Vector3.zero);
	private readonly NetworkVariable<Vector3> _p3 = new NetworkVariable<Vector3>(Vector3.zero);
	private readonly NetworkVariable<Vector3> _exitPos = new NetworkVariable<Vector3>(Vector3.zero);

	private readonly HashSet<ulong> _survivorsIn = new HashSet<ulong>();
	private float _lastNoiseTime = -10f;
	private float _soloAutoStartAfter;
	private ulong _monsterId;
	private bool _hasMonster;
	private int _escaped;

	public int GenTotal { get { return _genTotal.Value; } }
	public int GenDone { get { return _genDone.Value; } }
	public bool ExitOpen { get { return _exitOpen.Value; } }
	public Vector3 ExitPos { get { return _exitPos.Value; } }

	// Minijeu de disjoncteur : les générateurs 2 et 4 restent à 50 % tant que la séquence n'est pas réussie.
	public bool GenSolved(int i) { return (_genSolved.Value & (1 << i)) != 0; }
	public static bool GeneratorNeedsSequence(int i) { return i % 2 == 1; }

	public float GenProgress(int i)
	{
		switch (i)
		{
		case 0: return _g0.Value;
		case 1: return _g1.Value;
		case 2: return _g2.Value;
		default: return _g3.Value;
		}
	}

	public Vector3 GenPos(int i)
	{
		switch (i)
		{
		case 0: return _p0.Value;
		case 1: return _p1.Value;
		case 2: return _p2.Value;
		default: return _p3.Value;
		}
	}

	private void SetProgress(int i, float v)
	{
		switch (i)
		{
		case 0: _g0.Value = v; break;
		case 1: _g1.Value = v; break;
		case 2: _g2.Value = v; break;
		default: _g3.Value = v; break;
		}
	}

	private void SetupObjectives(int survivors)
	{
		int total = Mathf.Clamp(survivors + 1, 2, 4);
		Vector3[] gens;
		Vector3 exit;
		ObjectiveWorld.ComputePlaces(survivorSpawnPoints, monsterSpawnPoint, transform, total, out gens, out exit);
		_g0.Value = 0f; _g1.Value = 0f; _g2.Value = 0f; _g3.Value = 0f;
		_p0.Value = gens[0];
		_p1.Value = gens.Length > 1 ? gens[1] : gens[0];
		_p2.Value = gens.Length > 2 ? gens[2] : gens[0];
		_p3.Value = gens.Length > 3 ? gens[3] : gens[0];
		_exitPos.Value = exit;
		_genDone.Value = 0;
		_exitOpen.Value = false;
		_genSolved.Value = 0;
		_genCompletedMask.Value = 0;
		_genTotal.Value = total;
	}

	private void ClearObjectives()
	{
		_genTotal.Value = 0;
		_genDone.Value = 0;
		_exitOpen.Value = false;
		_genSolved.Value = 0;
		_genCompletedMask.Value = 0;
	}

	[ServerRpc(RequireOwnership = false)]
	public void RepairGeneratorServerRpc(int index, float seconds, ServerRpcParams rpcParams = default(ServerRpcParams))
	{
		ulong sender = rpcParams.Receive.SenderClientId;
		if (!_gameStarted || !_survivorsIn.Contains(sender) || index < 0 || index >= _genTotal.Value)
		{
			return;
		}
		float progress = GenProgress(index);
		if (progress >= 1f)
		{
			return;
		}
		NetworkClient client;
		if (NetworkManager.Singleton == null || !NetworkManager.Singleton.ConnectedClients.TryGetValue(sender, out client) || client.PlayerObject == null)
		{
			return;
		}
		if (Vector3.Distance(client.PlayerObject.transform.position, GenPos(index)) > 5f)
		{
			return;
		}
		progress = Mathf.Min(1f, progress + Mathf.Clamp(seconds, 0f, 0.7f) / 28f);
		if (GeneratorNeedsSequence(index) && !GenSolved(index))
			progress = Mathf.Min(progress, 0.5f);
		SetProgress(index, progress);

		// le bruit de la réparation attire le monstre (joueur ou IA)
		if (Time.time - _lastNoiseTime > 3f || progress >= 1f)
		{
			_lastNoiseTime = Time.time;
			HuggyHunterAI ai = FindObjectOfType<HuggyHunterAI>();
			if (ai != null)
			{
				ai.HearNoise(GenPos(index));
			}
			if (_hasMonster)
			{
				ClientRpcParams p = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[1] { _monsterId } } };
				NoisePingClientRpc(GenPos(index), p);
			}
		}
		if (progress >= 1f)
			MarkGeneratorComplete(index);
	}

	[ClientRpc]
	private void NoisePingClientRpc(Vector3 pos, ClientRpcParams rpcParams = default(ClientRpcParams))
	{
		if (ObjectiveWorld.Instance != null)
		{
			ObjectiveWorld.Instance.AddPing(pos);
		}
	}

	// ------------------------------------------------------------------ minijeux des générateurs
	private void RaiseNoise(Vector3 position)
	{
		HuggyHunterAI ai = FindObjectOfType<HuggyHunterAI>();
		if (ai != null)
			ai.HearNoise(position);
		if (_hasMonster)
			NoisePingClientRpc(position, Only(_monsterId));
	}

	private void MarkGeneratorComplete(int index)
	{
		if (!IsServer || index < 0 || index >= _genTotal.Value)
			return;
		int bit = 1 << index;
		if ((_genCompletedMask.Value & bit) != 0)
			return;
		_genCompletedMask.Value |= bit;
		_genDone.Value = Mathf.Min(_genTotal.Value, _genDone.Value + 1);
		if (_genDone.Value >= _genTotal.Value && !_exitOpen.Value)
		{
			_exitOpen.Value = true;
			SetRoleNotificationClientRpc("TOUS LES GÉNÉRATEURS SONT ACTIFS ! LA SORTIE EST OUVERTE !");
		}
	}

	/// <summary>kind 0 = test de timing (bonus / pénalité), kind 1 = séquence de disjoncteur.</summary>
	[ServerRpc(RequireOwnership = false)]
	public void GeneratorMinigameServerRpc(int index, int kind, bool success, ServerRpcParams rpcParams = default(ServerRpcParams))
	{
		ulong sender = rpcParams.Receive.SenderClientId;
		if (!_gameStarted || !_survivorsIn.Contains(sender) || index < 0 || index >= _genTotal.Value || (kind != 0 && kind != 1))
			return;
		DbUnityMovment player = PlayerOf(sender);
		if (player == null || player.IsBoxed)
			return;
		if (Vector3.Distance(player.transform.position, GenPos(index)) > 5f)
			return;
		float progress = GenProgress(index);
		if (progress >= 1f)
			return;

		if (kind == 1)
		{
			if (success && progress >= 0.499f && GeneratorNeedsSequence(index))
			{
				_genSolved.Value = _genSolved.Value | (1 << index);
			}
			else if (!success)
			{
				SetProgress(index, Mathf.Min(progress, Mathf.Max(0.45f, progress - 0.05f)));
				RaiseNoise(GenPos(index));
			}
			return;
		}

		if (success)
		{
			progress += 0.06f;
		}
		else
		{
			progress -= 0.08f;
			RaiseNoise(GenPos(index));
		}
		progress = Mathf.Clamp01(progress);
		if (GeneratorNeedsSequence(index) && !GenSolved(index))
			progress = Mathf.Min(progress, 0.5f);
		SetProgress(index, progress);
		if (progress >= 1f)
			MarkGeneratorComplete(index);
	}

	// ------------------------------------------------------------------ vies, coups et cartons
	private readonly Dictionary<ulong, float> _invulnUntil = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, float> _freeProgress = new Dictionary<ulong, float>();
	private readonly Dictionary<ulong, float> _freeLast = new Dictionary<ulong, float>();
	private readonly HashSet<ulong> _escapedClients = new HashSet<ulong>();
	private int _boxCount;
	private string _lastWinnerText = "";

	private DbUnityMovment PlayerOf(ulong clientId)
	{
		NetworkClient client;
		if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out client) && client.PlayerObject != null)
			return client.PlayerObject.GetComponent<DbUnityMovment>();
		return null;
	}

	private ClientRpcParams Only(ulong clientId)
	{
		return new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[1] { clientId } } };
	}

	private void ResetAllLives()
	{
		if (!IsServer || NetworkManager.Singleton == null)
			return;
		foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
		{
			DbUnityMovment mv = PlayerOf(client.ClientId);
			if (mv != null)
				mv.ServerResetLife();
		}
	}

	/// <summary>Serveur : un coup de monstre. 3 coups = encartonné (une vie perdue). Plus de vie = éliminé pour la manche.</summary>
	public void ServerDamageSurvivor(ulong victimId)
	{
		if (!IsServer || !_gameStarted || !_survivorsIn.Contains(victimId))
			return;

		DbUnityMovment mv = PlayerOf(victimId);
		if (mv == null || mv.IsBoxed)
			return;

		float until;
		if (_invulnUntil.TryGetValue(victimId, out until) && Time.time < until)
			return;
		_invulnUntil[victimId] = Time.time + 2f;

		int hits = mv.ServerAddHit();
		if (hits < DbUnityMovment.HitsToBox)
		{
			mv.ServerHitFeedback();
			return;
		}
		BoxSurvivor(victimId, mv);
	}

	private void BoxSurvivor(ulong victimId, DbUnityMovment mv)
	{
		int lives = mv.ServerLoseLife();
		if (lives <= 0)
		{
			// dernière vie perdue : élimination définitive (plus de points pour ce joueur)
			if (lobbySpawnPoint != null)
				ExecuteTargetedTeleport(victimId, lobbySpawnPoint.position, isMonster: false, isSurvivor: false);
			_survivorsIn.Remove(victimId);
			return;
		}

		mv.ServerSetBoxed(true);
		_boxCount++;
		_freeProgress[victimId] = 0f;
		StartCoroutine(AutoReleaseBoxed(victimId, _roundId));
	}

	private bool HasFreeHelper(ulong boxedId)
	{
		foreach (ulong id in _survivorsIn)
		{
			if (id == boxedId)
				continue;
			DbUnityMovment other = PlayerOf(id);
			if (other != null && !other.IsBoxed)
				return true;
		}
		return false;
	}

	private void FreeSurvivor(ulong clientId, DbUnityMovment mv)
	{
		mv.ServerSetBoxed(false);
		_freeProgress.Remove(clientId);
		_invulnUntil[clientId] = Time.time + 3f;
	}

	// Si personne ne vient libérer le survivant, le carton s'ouvre tout seul (la vie reste perdue).
	private IEnumerator AutoReleaseBoxed(ulong victimId, int roundId)
	{
		float waited = 0f;
		while (true)
		{
			yield return new WaitForSeconds(1f);
			waited += 1f;
			if (!_gameStarted || roundId != _roundId)
				yield break;
			DbUnityMovment mv = PlayerOf(victimId);
			if (mv == null || !mv.IsBoxed)
				yield break;
			float limit = HasFreeHelper(victimId) ? 40f : 15f;
			if (waited >= limit)
			{
				FreeSurvivor(victimId, mv);
				yield break;
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void FreeBoxedServerRpc(ulong boxedClientId, float seconds, ServerRpcParams rpcParams = default(ServerRpcParams))
	{
		ulong sender = rpcParams.Receive.SenderClientId;
		if (!_gameStarted || sender == boxedClientId || !_survivorsIn.Contains(sender))
			return;
		DbUnityMovment helper = PlayerOf(sender);
		DbUnityMovment boxed = PlayerOf(boxedClientId);
		if (helper == null || boxed == null || helper.IsBoxed || !boxed.IsBoxed)
			return;
		if (Vector3.Distance(helper.transform.position, boxed.transform.position) > 4.5f)
			return;

		float last;
		if (_freeLast.TryGetValue(boxedClientId, out last) && Time.time - last > 1.5f)
			_freeProgress[boxedClientId] = 0f;
		_freeLast[boxedClientId] = Time.time;

		float progress;
		_freeProgress.TryGetValue(boxedClientId, out progress);
		progress += Mathf.Clamp(seconds, 0f, 0.7f) / 3f;
		if (progress >= 1f)
			FreeSurvivor(boxedClientId, boxed);
		else
			_freeProgress[boxedClientId] = progress;
	}

	[ServerRpc(RequireOwnership = false)]
	public void PullBoxedServerRpc(ulong boxedClientId, Vector3 pullerPosition, ServerRpcParams rpcParams = default(ServerRpcParams))
	{
		ulong sender = rpcParams.Receive.SenderClientId;
		if (!_gameStarted || sender == boxedClientId || !_survivorsIn.Contains(sender))
			return;
		DbUnityMovment puller = PlayerOf(sender);
		DbUnityMovment boxed = PlayerOf(boxedClientId);
		if (puller == null || boxed == null || puller.IsBoxed || !boxed.IsBoxed)
			return;
		if (Vector3.Distance(puller.transform.position, boxed.transform.position) > 40f)
			return;
		boxed.ServerPullBoxed(pullerPosition);
	}

	private void GrantRoundRewards(bool grant)
	{
		if (!grant || NetworkManager.Singleton == null)
			return;

		foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
		{
			ulong id = client.ClientId;
			int credits;
			int tickets = 0;
			string label;
			if (_hasMonster && id == _monsterId)
			{
				credits = 40 + 30 * _boxCount;
				label = "MONSTRE";
			}
			else if (_escapedClients.Contains(id))
			{
				credits = 100;
				tickets = 1;
				label = "ÉVADÉ";
			}
			else if (_eliminatedClients.Contains(id))
			{
				credits = 0;
				label = "ÉLIMINÉ : AUCUN POINT";
			}
			else
			{
				credits = 60;
				label = "SURVIVANT";
			}
			string text = _lastWinnerText + "   -   " + label + (credits > 0 ? "  +" + credits : "");
			RoundRewardClientRpc(credits, tickets, text, Only(id));
		}
	}

	[ClientRpc]
	private void RoundRewardClientRpc(int credits, int tickets, string text, ClientRpcParams rpcParams = default(ClientRpcParams))
	{
		if (credits > 0)
			PoppyShop.AddCredits(credits);
		if (tickets > 0)
			PoppyShop.AddTickets(tickets);
		if (roleNotificationText != null)
		{
			roleNotificationText.text = text;
			CancelInvoke("ClearRoleText");
			Invoke("ClearRoleText", 7f);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void EscapeServerRpc(ServerRpcParams rpcParams = default(ServerRpcParams))
	{
		ulong sender = rpcParams.Receive.SenderClientId;
		NetworkClient client;
		if (!_gameStarted || !_exitOpen.Value || !_survivorsIn.Contains(sender) || NetworkManager.Singleton == null ||
		    !NetworkManager.Singleton.ConnectedClients.TryGetValue(sender, out client) || client.PlayerObject == null)
		{
			return;
		}
		if (Vector3.Distance(client.PlayerObject.transform.position, _exitPos.Value) > 4.5f)
		{
			return;
		}
		_survivorsIn.Remove(sender);
		_escapedClients.Add(sender);
		_escaped++;
		if (lobbySpawnPoint != null)
		{
			ExecuteTargetedTeleport(sender, lobbySpawnPoint.position, isMonster: false, isSurvivor: true);
		}
		ClientRpcParams p = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[1] { sender } } };
		EscapeBonusClientRpc(60, p);
	}

	[ClientRpc]
	private void EscapeBonusClientRpc(int credits, ClientRpcParams rpcParams = default(ClientRpcParams))
	{
		PoppyShop.AddCredits(credits);
		ProceduralAudio.Play2D(ProceduralAudio.Success, 0.9f);
		if (roleNotificationText != null)
		{
			roleNotificationText.text = "VOUS VOUS ÉCHAPPEZ !  +" + credits;
			CancelInvoke("ClearRoleText");
			Invoke("ClearRoleText", 6f);
		}
	}

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
			// En solo, le hôte seul suffit : le monstre IA est optionnel (avertissement si non configuré).
			bool validMode = useAI ? count >= 1 : count >= 2;

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

	/// <summary>Starts an AI solo round automatically when the local host is the only player.</summary>
	public bool TryStartSoloMatchFromLobby()
	{
		if (!IsServer || NetworkManager.Singleton == null || _gameStarted || Time.unscaledTime < _soloAutoStartAfter ||
			PlayerPrefs.GetInt("Poppy.EnemyMode", 0) != 1 ||
			NetworkManager.Singleton.ConnectedClientsList.Count != 1)
			return false;

		ulong hostClientId = NetworkManager.ServerClientId;
		if (!_readyClients.Contains(hostClientId))
			_readyClients.Add(hostClientId);

		if (!CanStartMatch)
			return false;

		StartMatchFromLobby();
		return _gameStarted;
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

	/// <summary>Serveur : change l'état « prêt » d'un joueur (appelé par le joueur lui-même, propriétaire de son objet).</summary>
	public void ServerSetReady(ulong clientId, bool ready)
	{
		if (!IsServer || NetworkManager.Singleton == null || !NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
			return;
		bool currentlyReady = _readyClients.Contains(clientId);
		if (ready && !currentlyReady)
			_readyClients.Add(clientId);
		else if (!ready && currentlyReady)
			_readyClients.Remove(clientId);
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

	private void ResolveSceneSpawnPoints()
	{
		// Les noms sont volontairement exacts pour correspondre aux nodes de la scène.
		if (playerSpawnPoint == null)
		{
			GameObject point = GameObject.Find("Player spawn");
			if (point != null) playerSpawnPoint = point.transform;
		}
		if (playerMonsterSpawnPoint == null)
		{
			GameObject point = GameObject.Find("Player Monster spawn");
			if (point != null) playerMonsterSpawnPoint = point.transform;
		}
		if (monsterSpawnPoint == null)
			monsterSpawnPoint = playerMonsterSpawnPoint;
		if (playerMonsterSpawnPoint == null)
			playerMonsterSpawnPoint = monsterSpawnPoint;
	}

	private Vector3 GetPlayerSpawnPosition(int index)
	{
		if (playerSpawnPoint != null) return playerSpawnPoint.position;
		if (survivorSpawnPoints != null && survivorSpawnPoints.Length > 0)
		{
			Transform point = survivorSpawnPoints[index % survivorSpawnPoints.Length];
			if (point != null) return point.position;
		}
		return transform.position;
	}

	private Vector3 GetMonsterPlayerSpawnPosition()
	{
		if (playerMonsterSpawnPoint != null) return playerMonsterSpawnPoint.position;
		if (monsterSpawnPoint != null) return monsterSpawnPoint.position;
		return transform.position;
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
		ResolveSceneSpawnPoints();
		_netTimer.OnValueChanged += UpdateTimerUI;
		UpdateTimerUI(0, _netTimer.Value);

		ObjectiveWorld ow = gameObject.GetComponent<ObjectiveWorld>();
		if (ow == null)
		{
			ow = gameObject.AddComponent<ObjectiveWorld>();
		}
		ow.Init(this);

		if (!IsServer || NetworkManager.Singleton == null)
			return;

		_netTimer.Value = RoundSeconds;
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

		// Match start is handled by PoppyLobbyUI with the O key or its button.
		// The server no longer starts a round automatically with Enter.
		if (hostStartPromptUI != null && hostStartPromptUI.activeSelf)
			hostStartPromptUI.SetActive(false);
	}

	/// <summary>Appelé par le joueur lui-même une fois apparu : placement au salon (pendant une manche : spectateur).</summary>
	public void PlaceJoinedClient(ulong clientId)
	{
		if (!IsServer || lobbySpawnPoint == null)
			return;
		// Toujours survivant au salon : caméra, déplacement et Grabpack actifs (pas de mode spectateur à l'arrivée).
		ExecuteTargetedTeleport(clientId, lobbySpawnPoint.position, isMonster: false, isSurvivor: true);
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
		if (IsServer)
		{
			_survivorsIn.Remove(clientId);
			if (_hasMonster && clientId == _monsterId)
			{
				_hasMonster = false;
				_survivorsIn.Clear();
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

		_roundId++;
		int currentRoundId = _roundId;
		_gameStarted = true;
		_netGameStarted.Value = true;
		StartRoundAudioClientRpc();
		_eliminatedClients.Clear();
		_survivorsIn.Clear();
		_hasMonster = false;
		_escaped = 0;
		_escapedClients.Clear();
		_boxCount = 0;
		_invulnUntil.Clear();
		_freeProgress.Clear();
		_freeLast.Clear();
		ResetAllLives();
		ResetRoundProgression();
		_netTimer.Value = RoundSeconds;
		SetRoleNotificationClientRpc("Assigning Roles...");
		yield return new WaitForSecondsRealtime(1f);
		if (!IsServer || currentRoundId != _roundId || !_gameStarted)
			yield break;

		// Attendre que chaque client connecté ait réellement son PlayerObject côté serveur.
		// Sans cela, une affectation trop précoce saute les joueurs dont le prefab arrive en retard.
		float spawnWait = 0f;
		while (spawnWait < 12f && !AllConnectedPlayersHaveSpawned())
		{
			if (!IsServer || currentRoundId != _roundId || !_gameStarted) yield break;
			spawnWait += 0.1f;
			yield return new WaitForSecondsRealtime(0.1f);
		}
		if (!AllConnectedPlayersHaveSpawned())
		{
			Debug.LogError("Round cancelled: at least one connected player has no spawned PlayerObject after 12 seconds.");
			EndGame(grantRewards: false);
			yield break;
		}

		AssignRolesAndSpawn();
		if (!_gameStarted || currentRoundId != _roundId)
			yield break;
		if (_survivorsIn.Count == 0)
		{
			Debug.LogError("Round cancelled: no player object could be assigned as survivor.");
			EndGame(false);
			yield break;
		}
		try
		{
			SetupObjectives(_survivorsIn.Count);
		}
		catch (System.Exception ex)
		{
			// un souci de placement ne doit jamais empêcher la manche de démarrer
			Debug.LogError("Objectives setup failed, round continues without them: " + ex.Message);
			ClearObjectives();
		}

		while (IsServer && currentRoundId == _roundId && _gameStarted && _netTimer.Value > 0)
		{
			yield return new WaitForSecondsRealtime(1f);
			if (currentRoundId != _roundId || !_gameStarted)
				yield break;
			_netTimer.Value = Mathf.Max(0, _netTimer.Value - 1);
			// plus aucun survivant en jeu (tous tués ou évadés) : fin de manche anticipée
			if (_survivorsIn.Count == 0)
				break;
		}
		if (currentRoundId == _roundId && _gameStarted)
			EndGame();
	}

	[ClientRpc]
	private void RoundEndAudioClientRpc()
	{
		PoppyAudioManager.PlayLobbyMusic();
	}

	[ClientRpc]
	private void StartRoundAudioClientRpc()
	{
		PoppyAudioManager.PlayGameplayMusic();
	}

	private bool AllConnectedPlayersHaveSpawned()
	{
		if (NetworkManager.Singleton == null) return false;
		foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
		{
			if (client == null || client.PlayerObject == null || !client.PlayerObject.IsSpawned)
				return false;
		}
		return NetworkManager.Singleton.ConnectedClientsList.Count > 0;
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
		bool aiAvailable = aiMonsterPrefab != null && monsterSpawnPoint != null;
		if (useAI && !aiAvailable)
		{
			// Ne bloque plus le solo : la manche démarre sans Huggy, avec les objectifs.
			Debug.LogError("AI monster missing: assign aiMonsterPrefab and monsterSpawnPoint in GameManager. Starting without Huggy.", this);
			SetRoleNotificationClientRpc("HUGGY IA NON CONFIGURÉ (aiMonsterPrefab / monsterSpawnPoint)");
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

				Vector3 spawn = GetPlayerSpawnPosition(survivorIndex);
				ExecuteTargetedTeleport(client.ClientId, spawn, isMonster: false, isSurvivor: true);
				NotifySingleClientRole(client.ClientId, "HUGGY IS HUNTING YOU. REPAIR THE GENERATORS, THEN ESCAPE!");
				_survivorsIn.Add(client.ClientId);
				survivorIndex++;
			}

			if (aiAvailable)
			{
				spawnedAiMonster = Instantiate(aiMonsterPrefab, monsterSpawnPoint.position, monsterSpawnPoint.rotation);
				spawnedAiMonster.Spawn();
			}
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
				Vector3 spawn = GetMonsterPlayerSpawnPosition();
				ExecuteTargetedTeleport(client.ClientId, spawn, isMonster: true, isSurvivor: false);
				NotifySingleClientRole(client.ClientId, "YOU ARE THE MONSTER! HUNT THE HIDERS!");
				_monsterId = client.ClientId;
				_hasMonster = true;
			}
			else
			{
				Vector3 spawn = GetPlayerSpawnPosition(survivorSpawnIndex);
				ExecuteTargetedTeleport(client.ClientId, spawn, isMonster: false, isSurvivor: true);
				NotifySingleClientRole(client.ClientId, "YOU ARE A HIDER! REPAIR THE GENERATORS, THEN ESCAPE!");
				_survivorsIn.Add(client.ClientId);
				survivorSpawnIndex++;
			}
		}
	}

	public void ExecuteTargetedTeleport(ulong clientId, Vector3 targetPosition, bool isMonster, bool isSurvivor)
	{
		if (IsServer)
		{
			if (!isSurvivor && !isMonster)
			{
				_eliminatedClients.Add(clientId);
				_survivorsIn.Remove(clientId);
			}
			else if (isSurvivor)
				_eliminatedClients.Remove(clientId);
		}

		if (IsServer && NetworkManager.Singleton != null &&
			NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var value) &&
			value.PlayerObject != null)
		{
			ulong networkObjectId = value.PlayerObject.NetworkObjectId;
			Debug.Log("[Spawn] Téléportation demandée : client=" + clientId + ", monster=" + isMonster + ", position=" + targetPosition);
			TeleportClientTargetClientRpc(targetPosition, isMonster, isSurvivor, networkObjectId, clientId);
		}
	}

	[ClientRpc]
	private void TeleportClientTargetClientRpc(Vector3 targetPosition, bool isMonster, bool isSurvivor, ulong targetNetObjectId, ulong targetOwnerId)
	{
		// Le RPC de téléportation peut arriver avant la réplication du PlayerObject chez un client.
		// On attend sa disponibilité au lieu d'abandonner définitivement le déplacement.
		StartCoroutine(ApplyTeleportWhenPlayerIsSpawned(targetPosition, isMonster, isSurvivor, targetNetObjectId, targetOwnerId));
	}

	private IEnumerator ApplyTeleportWhenPlayerIsSpawned(Vector3 targetPosition, bool isMonster, bool isSurvivor, ulong targetNetObjectId, ulong targetOwnerId)
	{
		NetworkObject value = null;
		for (int attempt = 0; attempt < 60; attempt++)
		{
			NetworkManager nm = NetworkManager.Singleton;
			if (nm == null || !nm.IsListening || nm.SpawnManager == null) yield break;
			if (nm.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjectId, out value) &&
				value != null && value.OwnerClientId == targetOwnerId) break;
			value = null;
			yield return new WaitForSecondsRealtime(0.1f);
		}
		if (value == null)
		{
			Debug.LogError("[Spawn] PlayerObject " + targetNetObjectId + " (client " + targetOwnerId + ") n'est pas apparu après 6 secondes.");
			yield break;
		}
		if (NetworkManager.Singleton != null && targetOwnerId == NetworkManager.Singleton.LocalClientId && isMonster)
		{
			PoppyAudioManager.PlayMonsterMusic();
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

		_roundId++;
		_soloAutoStartAfter = Time.unscaledTime + 10f;
		bool wasStarted = _gameStarted;
		_gameStarted = false;
		_netGameStarted.Value = false;
		ClearObjectives();
		_survivorsIn.Clear();
		_lastWinnerText = _escaped > 0 ? "LES SURVIVANTS S'ÉCHAPPENT !  (" + _escaped + ")" : "LE MONSTRE A GAGNÉ...";
		// Les points ne vont qu'aux joueurs encore en vie ou évadés : un joueur éliminé ne gagne rien.
		GrantRoundRewards(wasStarted && grantRewards);
		RoundEndAudioClientRpc();
		_eliminatedClients.Clear();
		_readyClients.Clear();
		ResetRoundProgression();
		ResetAllLives();
		_escapedClients.Clear();
		if (spawnedAiMonster != null && spawnedAiMonster.IsSpawned)
			spawnedAiMonster.Despawn(true);
		spawnedAiMonster = null;
		_netTimer.Value = RoundSeconds;
		foreach (NetworkClient connectedClients in NetworkManager.Singleton.ConnectedClientsList)
		{
			if (connectedClients.PlayerObject != null && lobbySpawnPoint != null)
			{
				ExecuteTargetedTeleport(connectedClients.ClientId, lobbySpawnPoint.position, isMonster: false, isSurvivor: true);
			}
		}
	}
}

