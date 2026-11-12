using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class HazardKillZone : NetworkBehaviour
{
	[Header("Targeting Settings")]
	[SerializeField]
	private LayerMask survivorLayer;

	[SerializeField]
	private float jumpscareDuration = 2.5f;

	private void OnTriggerEnter(Collider other)
	{
		if (((1 << other.gameObject.layer) & (int)survivorLayer) != 0)
		{
			NetworkObject componentInParent = other.GetComponentInParent<NetworkObject>();
			if (componentInParent != null && componentInParent.IsOwner)
			{
				ExecuteHazardKillServerRpc(componentInParent.OwnerClientId, componentInParent.NetworkObjectId);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void ExecuteHazardKillServerRpc(ulong victimClientId, ulong victimNetworkObjectId)
	{
			SyncHazardKillClientRpc(victimClientId, victimNetworkObjectId);
			StartCoroutine(ServerRespawnTimer(victimClientId));
	}

	[ClientRpc]
	private void SyncHazardKillClientRpc(ulong victimClientId, ulong victimNetworkObjectId)
	{
		if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(victimNetworkObjectId, out var value))
		{
			return;
		}
		DbUnityMovment component = value.GetComponent<DbUnityMovment>();
		if (component != null)
		{
			if (component.grabpackScript != null && component.grabpackScript is GrabpackController grabpackController)
			{
				grabpackController.ResetHandsBeforeDeath();
			}
			if (component.survivorModelContainer != null)
			{
				component.survivorModelContainer.SetActive(value: false);
			}
			if (component.deadModelContainer != null)
			{
				component.deadModelContainer.SetActive(value: true);
			}
			component.SetGrabpackStatus(enableGrabpack: false);
			component.StartCoroutine(LocalFreezeRoutine(component));
		}
	}

	private IEnumerator ServerRespawnTimer(ulong victimClientId)
	{
		yield return new WaitForSeconds(jumpscareDuration);
		GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
		if (gameManager != null && gameManager.lobbySpawnPoint != null)
		{
			gameManager.ExecuteTargetedTeleport(victimClientId, gameManager.lobbySpawnPoint.position, isMonster: false, isSurvivor: true);
		}
	}

	private IEnumerator LocalFreezeRoutine(DbUnityMovment victimMovement)
	{
		CharacterController controller = victimMovement.GetComponent<CharacterController>();
		Animator componentInChildren = victimMovement.GetComponentInChildren<Animator>();
		if (controller != null)
		{
			controller.enabled = false;
		}
		if (componentInChildren != null)
		{
			componentInChildren.SetTrigger("Jumpscare");
		}
		yield return new WaitForSeconds(jumpscareDuration + 0.3f);
		if (victimMovement.IsOwner)
		{
			if (victimMovement.survivorModelContainer != null)
			{
				victimMovement.survivorModelContainer.SetActive(value: true);
			}
			if (victimMovement.deadModelContainer != null)
			{
				victimMovement.deadModelContainer.SetActive(value: false);
			}
			victimMovement.SetGrabpackStatus(enableGrabpack: true);
		}
		if (controller != null)
		{
			controller.enabled = true;
		}
	}
}

