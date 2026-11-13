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

	private async void Awake()
	{
		VerifyInspectorAssignments();
		if (UnityServices.State != ServicesInitializationState.Uninitialized)
		{
			return;
		}
		InitializationOptions options = new InitializationOptions();
		options.SetProfile("Player_" + UnityEngine.Random.Range(1000, 9999));
		try
		{
			await UnityServices.InitializeAsync(options);
			if (!AuthenticationService.Instance.IsSignedIn)
			{
				await AuthenticationService.Instance.SignInAnonymouslyAsync();
				Debug.Log("[Netcode Authentication] Profile Loaded: " + AuthenticationService.Instance.PlayerId);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[Netcode Error] Initialization crashed: " + ex.Message);
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
		rawLiveCode = incomingText.Replace("\u200b", "").Replace("\n", "").Replace("\r", "")
			.ToUpper()
			.Trim();
	}

	public async void HostAServer()
	{
		if (networkManager == null)
		{
			networkManager = NetworkManager.Singleton;
		}
		SavePlayerName();
		float timeoutCounter = 5f;
		while ((UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn) && timeoutCounter > 0f)
		{
			await Task.Delay(250);
			timeoutCounter -= 0.25f;
		}
		if (UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
		{
			Debug.LogError("[Netcode Critical] Host canceled! Services failed to authenticate.");
			return;
		}
		if (networkManager.IsServer || networkManager.IsClient)
		{
			Debug.LogWarning("[Netcode] NetworkManager was already running! Forcing a hard shutdown before restarting host...");
			networkManager.Shutdown();
			await Task.Delay(500);
		}
		try
		{
			UnityTransport transport = networkManager.GetComponent<UnityTransport>();
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
		{
			networkManager = NetworkManager.Singleton;
		}
		if (string.IsNullOrEmpty(rawLiveCode) && codeInputField != null)
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
		float timeoutCounter = 5f;
		while ((UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn) && timeoutCounter > 0f)
		{
			await Task.Delay(250);
			timeoutCounter -= 0.25f;
		}
		try
		{
			Debug.Log("[Netcode] Connecting client to Relay room: '" + rawLiveCode + "'");
			JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(rawLiveCode);
			networkManager.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "udp"));
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
		{
			networkManager = NetworkManager.Singleton;
		}
		SavePlayerName();
		float timeoutCounter = 5f;
		while ((UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn) && timeoutCounter > 0f)
		{
			await Task.Delay(250);
			timeoutCounter -= 0.25f;
		}
		try
		{
			Debug.Log("[Netcode] Searching for an active random room...");
			Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
			if (lobby == null || !lobby.Data.ContainsKey("RelayCode"))
			{
				return;
			}
			string discoveredCode = lobby.Data["RelayCode"].Value;
			JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(discoveredCode);
			networkManager.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(allocation, "udp"));
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
		if (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text))
		{
			PlayerName = nameInputField.text;
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
		if (canvas2 != null)
		{
			canvas2.SetActive(value: true);
		}
		else
		{
			Debug.LogError("[Netcode UI Assignment Missing] 'canvas2' is NOT assigned in the inspector component slots!");
		}
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

	private void CleanUpLobby()
	{
		if (hostedLobby != null && AuthenticationService.Instance.IsSignedIn)
		{
			try
			{
				LobbyService.Instance.DeleteLobbyAsync(hostedLobby.Id);
				hostedLobby = null;
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Netcode] Could not clean up lobby room on closure: " + ex.Message);
			}
		}
	}
}

