using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class ConnectionManafer : MonoBehaviour
{
	[Header("Network Manager Reference")]
	public NetworkManager networkManager;

	[Header("UI Canvas References")]
	public GameObject canvas1;

	public GameObject canvas2;

	[Header("Input References")]
	public TMP_InputField codeInputField;

	public TMP_InputField nameInputField;

	public TMP_Text permanentCodeText;

	public GameObject testingCamera;

	public static string PlayerName;

	private string rawLiveCode = "";

	private Lobby hostedLobby;

	private bool _isJoiningClient;
	private bool _isCleaningLobby;
	private Task<bool> _servicesReadyTask;

	private async void Awake()
	{
		VerifyInspectorAssignments();
		await EnsureServicesReadyAsync();
	}

	private Task<bool> EnsureServicesReadyAsync()
	{
		if (_servicesReadyTask == null || _servicesReadyTask.IsFaulted || _servicesReadyTask.IsCanceled)
			_servicesReadyTask = InitializeServicesAsync();

		return _servicesReadyTask;
	}

	private async Task<bool> InitializeServicesAsync()
	{
		try
		{
			if (UnityServices.State == ServicesInitializationState.Uninitialized)
			{
				InitializationOptions options = new InitializationOptions();
				options.SetProfile("Player_" + UnityEngine.Random.Range(1000, 9999));
				await UnityServices.InitializeAsync(options);
			}

			int waitAttempts = 0;
			while (UnityServices.State == ServicesInitializationState.Initializing && waitAttempts++ < 100)
				await Task.Delay(100);

			if (UnityServices.State != ServicesInitializationState.Initialized)
			{
				Debug.LogError("[Netcode] Unity Services did not reach the Initialized state.");
				return false;
			}

			if (!AuthenticationService.Instance.IsSignedIn)
			{
				await AuthenticationService.Instance.SignInAnonymouslyAsync();
				Debug.Log("[Netcode Authentication] Profile Loaded: " + AuthenticationService.Instance.PlayerId);
			}

			return AuthenticationService.Instance.IsSignedIn;
		}
		catch (Exception ex)
		{
			Debug.LogError("[Netcode Error] Services initialization/authentication failed: " + ex.Message);
			return false;
		}
	}

	private void Start()
	{
		if (canvas2 != null)
		{
			canvas2.SetActive(value: false);
		}
		if (networkManager == null)
		{
			networkManager = NetworkManager.Singleton;
		}
		if (codeInputField != null)
		{
			codeInputField.onValueChanged.RemoveAllListeners();
			codeInputField.onValueChanged.AddListener(MirrorTextToNetworkManager);
		}
	}

	private void Update()
	{
		if (_isJoiningClient && networkManager != null && networkManager.IsClient && networkManager.IsConnectedClient)
		{
			_isJoiningClient = false;
			TransitionUI();
			Debug.Log("[Netcode] Client fully loaded locally. UI transitioned.");
		}
	}

	private void MirrorTextToNetworkManager(string incomingText)
	{
		rawLiveCode = (incomingText ?? string.Empty).Replace("\u200b", "").Replace("\n", "").Replace("\r", "")
			.ToUpper()
			.Trim();
	}

	/// <summary>Starts a local host without Authentication, Lobby or Relay services.</summary>
	public void HostSoloOffline()
	{
		if (networkManager == null)
			networkManager = NetworkManager.Singleton;

		if (networkManager == null)
		{
			Debug.LogError("[Netcode] Solo mode cannot start: NetworkManager is missing.");
			return;
		}

		if (networkManager.IsListening)
		{
			Debug.LogWarning("[Netcode] A session is already active; cannot start offline solo.");
			return;
		}

		SavePlayerName();
		if (networkManager.StartHost())
		{
			Debug.Log("[Netcode] Offline solo host started.");
			TransitionUI();
		}
		else
		{
			Debug.LogError("[Netcode] Offline solo StartHost failed. Check Player Prefab and UnityTransport configuration.");
		}
	}

	public async void HostAServer()
	{
		if (networkManager == null)
			networkManager = NetworkManager.Singleton;
		if (networkManager == null)
		{
			Debug.LogError("[Netcode] Cannot host: no NetworkManager is assigned or available in the scene.");
			return;
		}

		SavePlayerName();
		if (!await EnsureServicesReadyAsync())
		{
			Debug.LogError("[Netcode Critical] Host canceled because Unity Services authentication is unavailable.");
			return;
		}

		if (networkManager.IsListening)
		{
			Debug.LogWarning("[Netcode] Shutting down the existing session before starting a new host.");
			networkManager.Shutdown();
			await Task.Delay(500);
			if (networkManager.IsListening)
			{
				Debug.LogError("[Netcode] Existing session did not shut down in time.");
				return;
			}
		}
		try
		{
			UnityTransport transport = networkManager.GetComponent<UnityTransport>();
			if (transport == null)
			{
				Debug.LogError("[Netcode] Cannot host: UnityTransport is missing from the NetworkManager object.");
				return;
			}
			Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
			string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
			if (permanentCodeText != null)
			{
				permanentCodeText.text = "ROOM: " + joinCode;
			}
			transport.SetRelayServerData(new RelayServerData(allocation, "udp"));
			Debug.Log("[Netcode] Attempting to invoke StartHost()...");
			if (networkManager.StartHost())
			{
				TransitionUI();
				try
				{
					CreateLobbyOptions options = new CreateLobbyOptions
					{
						IsPrivate = false,
						Data = new Dictionary<string, DataObject> {
						{
							"RelayCode",
							new DataObject(DataObject.VisibilityOptions.Public, joinCode)
						} }
					};
					hostedLobby = await LobbyService.Instance.CreateLobbyAsync("HorrorRoom_" + UnityEngine.Random.Range(100, 999), 4, options);
					InvokeRepeating("HeartbeatLobby", 15f, 15f);
					Debug.Log("[Netcode] Host established and registered to lobby database successfully. Code: " + joinCode);
				}
				catch (Exception ex)
				{
					Debug.LogWarning("[Netcode Warning] Relay active, but public Lobby registration failed: " + ex.Message);
				}
			}
			else
			{
				Debug.LogError("[Netcode Critical] StartHost() returned FALSE! Check if your NetworkManager prefab configuration is missing a Player Prefab setup.");
			}
		}
		catch (Exception ex2)
		{
			Debug.LogError("Host Error: " + ex2.Message);
		}
	}

	private async void HeartbeatLobby()
	{
		try
		{
			if (hostedLobby != null && AuthenticationService.Instance.IsSignedIn)
			{
				await LobbyService.Instance.SendHeartbeatPingAsync(hostedLobby.Id);
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Netcode] Heartbeat ping skipped: " + ex.Message);
		}
	}

	public async void JoinWithCode()
	{
		if (networkManager == null)
			networkManager = NetworkManager.Singleton;
		if (networkManager == null)
		{
			Debug.LogError("[Netcode] Cannot join: no NetworkManager is assigned or available in the scene.");
			return;
		}
		if (networkManager.IsListening)
		{
			Debug.LogWarning("[Netcode] A network session is already active. Leave it before joining another room.");
			return;
		}
		if (codeInputField != null)
		{
			rawLiveCode = codeInputField.text.Replace("\u200b", "").Replace("\n", "").Replace("\r", "")
				.ToUpper()
				.Trim();
		}
		if (string.IsNullOrEmpty(rawLiveCode))
		{
			Debug.LogError("[UI Failure] Code field is completely empty!");
			return;
		}
		SavePlayerName();
		if (!await EnsureServicesReadyAsync())
		{
			Debug.LogError("[Netcode] Cannot join: Unity Services authentication is unavailable.");
			return;
		}
		try
		{
			Debug.Log("[Netcode] Connecting client to Relay room: '" + rawLiveCode + "'");
			UnityTransport transport = networkManager.GetComponent<UnityTransport>();
			if (transport == null)
			{
				Debug.LogError("[Netcode] Cannot join: UnityTransport is missing from the NetworkManager object.");
				return;
			}
			JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(rawLiveCode);
			transport.SetRelayServerData(new RelayServerData(allocation, "udp"));
			await Task.Delay(200);
			if (networkManager.StartClient())
			{
				if (permanentCodeText != null)
				{
					permanentCodeText.text = "ROOM: " + rawLiveCode;
				}
				_isJoiningClient = true;
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Join Error: " + ex.Message);
		}
	}

	public async void JoinRandomServer()
	{
		if (networkManager == null)
			networkManager = NetworkManager.Singleton;
		if (networkManager == null)
		{
			Debug.LogError("[Netcode] Cannot quick-join: no NetworkManager is assigned or available in the scene.");
			return;
		}
		if (networkManager.IsListening)
		{
			Debug.LogWarning("[Netcode] A network session is already active. Leave it before joining another room.");
			return;
		}
		SavePlayerName();
		if (!await EnsureServicesReadyAsync())
		{
			Debug.LogError("[Netcode] Cannot quick-join: Unity Services authentication is unavailable.");
			return;
		}
		try
		{
			Debug.Log("[Netcode] Searching for an active random room...");
			Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
			if (lobby == null || lobby.Data == null || !lobby.Data.ContainsKey("RelayCode") || string.IsNullOrWhiteSpace(lobby.Data["RelayCode"].Value))
			{
				return;
			}
			string discoveredCode = lobby.Data["RelayCode"].Value;
			UnityTransport transport = networkManager.GetComponent<UnityTransport>();
			if (transport == null)
			{
				Debug.LogError("[Netcode] Cannot quick-join: UnityTransport is missing from the NetworkManager object.");
				return;
			}
			JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(discoveredCode);
			transport.SetRelayServerData(new RelayServerData(allocation, "udp"));
			await Task.Delay(200);
			if (networkManager.StartClient())
			{
				if (permanentCodeText != null)
				{
					permanentCodeText.text = "ROOM: " + discoveredCode;
				}
				_isJoiningClient = true;
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Join Random Match Failed: " + ex.Message);
		}
	}

	private void SavePlayerName()
	{
		if (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
		{
			PlayerName = nameInputField.text.Trim();
			if (PlayerName.Length > 20)
				PlayerName = PlayerName.Substring(0, 20);
		}
		else
		{
			PlayerName = "Player" + UnityEngine.Random.Range(10, 99);
		}
	}

	private void TransitionUI()
	{
		Debug.Log("[Netcode UI] TransitionUI executing right now...");
		if (canvas1 != null)
		{
			canvas1.SetActive(value: false);
		}
		else
		{
			Debug.LogError("[Netcode UI Assignment Missing] 'canvas1' is NOT assigned in the inspector component slots!");
		}
		// The new PoppyLobbyUI owns the waiting-room interface.
		// Keep the legacy canvas disabled to avoid duplicate buttons intercepting clicks.
		if (canvas2 != null)
			canvas2.SetActive(false);
		if (testingCamera != null)
		{
			testingCamera.SetActive(value: false);
		}
		else
		{
			Debug.LogWarning("[Netcode UI Assignment Optional] 'testingCamera' reference slot is empty.");
		}
	}

	private void VerifyInspectorAssignments()
	{
		if (canvas1 == null)
		{
			Debug.LogError("[CRITICAL CONFIG] Canvas 1 field is unassigned! It will fail to turn off when hosting.");
		}
		if (canvas2 == null)
		{
			Debug.LogError("[CRITICAL CONFIG] Canvas 2 field is unassigned! It will fail to turn on when hosting.");
		}
		if (testingCamera == null)
		{
			Debug.LogWarning("[CONFIG] Testing Camera/Scene reference is empty.");
		}
	}

	private void OnApplicationQuit()
	{
		CleanUpLobby();
	}

	private void OnDestroy()
	{
		CleanUpLobby();
	}

	private async void CleanUpLobby()
	{
		if (_isCleaningLobby || hostedLobby == null)
			return;

		_isCleaningLobby = true;
		Lobby lobbyToDelete = hostedLobby;
		hostedLobby = null;
		CancelInvoke(nameof(HeartbeatLobby));

		if (UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
			return;

		try
		{
			await LobbyService.Instance.DeleteLobbyAsync(lobbyToDelete.Id);
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Netcode] Could not clean up lobby room on closure: " + ex.Message);
		}
	}
}

