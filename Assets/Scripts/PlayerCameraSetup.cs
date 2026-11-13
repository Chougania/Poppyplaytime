using Unity.Netcode;
using UnityEngine;

public class PlayerCameraSetup : NetworkBehaviour
{
	[Header("First Person Camera")]
	public GameObject firstPersonCamera;

	[Header("Third Person Cameras")]
	public GameObject thirdPersonRightCamera;

	public GameObject thirdPersonLeftCamera;

	[Header("Starting Perspectives")]
	[SerializeField]
	private bool startInFirstPerson = false;

	[SerializeField]
	private bool startOnRightShoulder = true;

	private bool isFirstPerson;

	private bool isRightShoulder;

	public override void OnNetworkSpawn()
	{
		isFirstPerson = startInFirstPerson;
		isRightShoulder = startOnRightShoulder;
		if (IsOwner)
		{
			UpdateActiveCameras();
			Debug.Log("[Player Camera] Successfully activated rendering for local owner.");
		}
		else
		{
			DisableAllCameras();
		}
	}

	private void Update()
	{
		if (IsOwner)
		{
			if (Input.GetKeyDown(KeyCode.V))
			{
				isFirstPerson = !isFirstPerson;
				UpdateActiveCameras();
			}
			if (!isFirstPerson && Input.GetKeyDown(KeyCode.X))
			{
				isRightShoulder = !isRightShoulder;
				UpdateActiveCameras();
			}
		}
	}

	private void UpdateActiveCameras()
	{
		if (firstPersonCamera == null || thirdPersonRightCamera == null || thirdPersonLeftCamera == null)
		{
			Debug.LogError("[Player Camera] Please assign all 3 camera objects in the Inspector!");
			return;
		}
		DisableAllCameras();
		GameObject gameObject = null;
		if (isFirstPerson)
		{
			gameObject = firstPersonCamera;
		}
		else
		{
			gameObject = (isRightShoulder ? thirdPersonRightCamera : thirdPersonLeftCamera);
		}
		if (gameObject != null)
		{
			gameObject.SetActive(value: true);
			gameObject.tag = "MainCamera";
			Camera component = gameObject.GetComponent<Camera>();
			if (component != null)
			{
				component.enabled = true;
			}
		}
	}

	private void DisableAllCameras()
	{
		if (firstPersonCamera != null)
		{
			firstPersonCamera.SetActive(value: false);
			firstPersonCamera.tag = "Untagged";
		}
		if (thirdPersonRightCamera != null)
		{
			thirdPersonRightCamera.SetActive(value: false);
			thirdPersonRightCamera.tag = "Untagged";
		}
		if (thirdPersonLeftCamera != null)
		{
			thirdPersonLeftCamera.SetActive(value: false);
			thirdPersonLeftCamera.tag = "Untagged";
		}
	}
}

