using Unity.Netcode;
using UnityEngine;

public class PlayerCameraToggle : NetworkBehaviour
{
	[Header("Camera References")]
	[SerializeField]
	private GameObject firstPersonCamera;

	[SerializeField]
	private GameObject thirdPersonCamera;

	[Header("Settings")]
	[SerializeField]
	private bool startInFirstPerson = true;

	private bool isFirstPerson;

	public override void OnNetworkSpawn()
	{
		if (!IsOwner)
		{
			if (firstPersonCamera != null)
			{
				firstPersonCamera.SetActive(value: false);
			}
			if (thirdPersonCamera != null)
			{
				thirdPersonCamera.SetActive(value: false);
			}
		}
		else
		{
			isFirstPerson = startInFirstPerson;
			UpdateCameraPerspective();
		}
	}

	private void Update()
	{
		if (IsOwner && Input.GetKeyDown(KeyCode.V))
		{
			isFirstPerson = !isFirstPerson;
			UpdateCameraPerspective();
		}
	}

	private void UpdateCameraPerspective()
	{
		if (firstPersonCamera == null || thirdPersonCamera == null)
		{
			Debug.LogError("PlayerCameraToggle: Please assign both camera references in the Inspector!");
			return;
		}
		firstPersonCamera.SetActive(isFirstPerson);
		thirdPersonCamera.SetActive(!isFirstPerson);
		if (isFirstPerson)
		{
			firstPersonCamera.tag = "MainCamera";
			thirdPersonCamera.tag = "Untagged";
		}
		else
		{
			thirdPersonCamera.tag = "MainCamera";
			firstPersonCamera.tag = "Untagged";
		}
	}
}

