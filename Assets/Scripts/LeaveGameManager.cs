using Unity.Netcode;
using UnityEngine;

public class LeaveGameManager : MonoBehaviour
{
	[Header("Main Menu Object References")]
	[Tooltip("Drag your Main Menu Canvas GameObject from the Hierarchy here.")]
	public GameObject mainMenuCanvas;

	[Tooltip("Drag your Main Menu Background Camera GameObject from the Hierarchy here.")]
	public GameObject mainMenuCamera;

	[Header("Pause UI Reference")]
	[Tooltip("Drag your active Pause/Tabs Canvas GameObject here so it turns off when you leave.")]
	public GameObject pauseMenuCanvas;

	public void LeaveServerAndReturnToMenu()
	{
		DisconnectFromNetwork();
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		TransitionSingleSceneObjects();
	}

	private void DisconnectFromNetwork()
	{
		if (NetworkManager.Singleton != null)
		{
			if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
			{
				Debug.Log("[LeaveSystem] Host shutting down server session...");
				NetworkManager.Singleton.Shutdown();
			}
			else if (NetworkManager.Singleton.IsClient)
			{
				Debug.Log("[LeaveSystem] Client disconnecting from server...");
				NetworkManager.Singleton.Shutdown();
			}
		}
	}

	private void TransitionSingleSceneObjects()
	{
		if (mainMenuCanvas != null)
		{
			mainMenuCanvas.SetActive(value: true);
		}
		else
		{
			Debug.LogWarning("[LeaveSystem] Main Menu Canvas reference is missing!");
		}
		if (mainMenuCamera != null)
		{
			mainMenuCamera.SetActive(value: true);
			Camera component = mainMenuCamera.GetComponent<Camera>();
			if (component != null)
			{
				component.enabled = true;
			}
		}
		else
		{
			Debug.LogWarning("[LeaveSystem] Main Menu Camera reference is missing!");
		}
		if (pauseMenuCanvas != null)
		{
			pauseMenuCanvas.SetActive(value: false);
		}
	}
}

