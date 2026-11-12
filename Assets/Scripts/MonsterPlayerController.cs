using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MonsterPlayerController : NetworkBehaviour
{
	[Header("Model Swap Slots")]
	public GameObject standardPlayerModel;

	public GameObject monsterPlayerModel;

	[Header("Monster Attack Settings")]
	public float killDistance = 2.5f;

	public LayerMask hiderLayer;

	public Transform attackRaycastOrigin;

	public BoxCollider attackCubeCollider;

	[Header("Components & FX")]
	public Animator monsterAnimator;

	public Rigidbody rb;

	public AudioSource audioSource;

	public AudioClip jumpscareAudio;

	[Header("State Testing (Optional Debug)")]
	public bool isCrouchingTest;

	public NetworkVariable<bool> isMonsterMeshActive = new NetworkVariable<bool>(value: false);

	private NetworkVariable<bool> isAttacking = new NetworkVariable<bool>(value: false);

	public override void OnNetworkSpawn()
	{
		if (monsterAnimator == null)
		{
			monsterAnimator = GetComponentInChildren<Animator>();
		}
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		if (rb == null)
		{
			rb = GetComponent<Rigidbody>();
		}
		if (attackRaycastOrigin == null)
		{
			attackRaycastOrigin = ((Camera.main != null) ? Camera.main.transform : transform);
		}
		if (attackCubeCollider != null)
		{
			attackCubeCollider.isTrigger = true;
			attackCubeCollider.enabled = false;
		}
		NetworkVariable<bool> networkVariable = isMonsterMeshActive;
		networkVariable.OnValueChanged = (NetworkVariable<bool>.OnValueChangedDelegate)Delegate.Combine(networkVariable.OnValueChanged, new NetworkVariable<bool>.OnValueChangedDelegate(OnMeshStateChanged));
		UpdateLocalMeshVisibility(isMonsterMeshActive.Value);
	}

	public override void OnNetworkDespawn()
	{
		NetworkVariable<bool> networkVariable = isMonsterMeshActive;
		networkVariable.OnValueChanged = (NetworkVariable<bool>.OnValueChangedDelegate)Delegate.Remove(networkVariable.OnValueChanged, new NetworkVariable<bool>.OnValueChangedDelegate(OnMeshStateChanged));
	}

	private void OnMeshStateChanged(bool oldVal, bool newVal)
	{
		UpdateLocalMeshVisibility(newVal);
	}

	public void UpdateLocalMeshVisibility(bool showMonster)
	{
		if (standardPlayerModel != null)
		{
			standardPlayerModel.SetActive(!showMonster);
		}
		if (monsterPlayerModel != null)
		{
			monsterPlayerModel.SetActive(showMonster);
		}
	}

	private void Update()
	{
		if (IsOwner)
		{
			AnimateMovement();
			if (!isAttacking.Value && Input.GetMouseButtonDown(0) && isMonsterMeshActive.Value)
			{
				RequestKillTargetServerRpc();
			}
		}
	}

	private void AnimateMovement()
	{
		if (!(monsterAnimator == null) && monsterPlayerModel.activeSelf)
		{
			float value = 0f;
			if (rb != null)
			{
				value = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
			}
			monsterAnimator.SetFloat("Speed", value);
			monsterAnimator.SetBool("IsCrouching", isCrouchingTest);
		}
	}

	[ServerRpc]
	private void RequestKillTargetServerRpc()
	{
		if (isAttacking.Value)
		{
			return;
		}
		if (attackCubeCollider != null)
		{
			attackCubeCollider.enabled = true;
		}
		if (Physics.Raycast(attackRaycastOrigin.position, attackRaycastOrigin.forward, out var hitInfo, killDistance, hiderLayer))
		{
			NetworkObject componentInParent = hitInfo.collider.GetComponentInParent<NetworkObject>();
			if (componentInParent != null && componentInParent.OwnerClientId != OwnerClientId)
			{
				ulong ownerClientId = componentInParent.OwnerClientId;
				StartCoroutine(ServerKillSequence(componentInParent, ownerClientId));
				return;
			}
		}
		StartCoroutine(BoxCheckFallbackSequence());
	}

	private IEnumerator BoxCheckFallbackSequence()
	{
		yield return new WaitForFixedUpdate();
		if (!(attackCubeCollider != null) || isAttacking.Value)
		{
			yield break;
		}
		Collider[] array = Physics.OverlapBox(attackCubeCollider.transform.position, attackCubeCollider.bounds.extents, attackCubeCollider.transform.rotation, hiderLayer);
		for (int i = 0; i < array.Length; i++)
		{
			NetworkObject componentInParent = array[i].GetComponentInParent<NetworkObject>();
			if (componentInParent != null && componentInParent.OwnerClientId != OwnerClientId)
			{
				ulong ownerClientId = componentInParent.OwnerClientId;
				StartCoroutine(ServerKillSequence(componentInParent, ownerClientId));
				yield break;
			}
		}
		attackCubeCollider.enabled = false;
	}

	private IEnumerator ServerKillSequence(NetworkObject victimPlayer, ulong victimId)
	{
		isAttacking.Value = true;
		TriggerJumpscareEffectsClientRpc(victimId);
		FreezePlayerMovementClientRpc(OwnerClientId, shouldFreeze: true);
		FreezePlayerMovementClientRpc(victimId, shouldFreeze: true);
		yield return new WaitForSeconds(2.5f);
		GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
		if (gameManager != null && gameManager.lobbySpawnPoint != null)
		{
			gameManager.ExecuteTargetedTeleport(victimId, gameManager.lobbySpawnPoint.position, isMonster: false, isSurvivor: false);
		}
		FreezePlayerMovementClientRpc(OwnerClientId, shouldFreeze: false);
		FreezePlayerMovementClientRpc(victimId, shouldFreeze: false);
		if (attackCubeCollider != null)
		{
			attackCubeCollider.enabled = false;
		}
		isAttacking.Value = false;
	}

	[ClientRpc]
	private void TriggerJumpscareEffectsClientRpc(ulong victimId)
	{
		if (monsterAnimator != null && monsterPlayerModel.activeSelf)
		{
			monsterAnimator.SetTrigger("Jumpscare");
		}
		if (audioSource != null && jumpscareAudio != null)
		{
			audioSource.PlayOneShot(jumpscareAudio);
		}
		if (NetworkManager.Singleton.LocalClientId == victimId)
		{
			Camera.main.transform.LookAt(transform.position + Vector3.up * 1.5f);
		}
	}

	[ClientRpc]
	private void FreezePlayerMovementClientRpc(ulong targetId, bool shouldFreeze)
	{
		if (NetworkManager.Singleton.LocalClientId != targetId)
		{
			return;
		}
		NetworkObject playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
		if (!(playerObject != null))
		{
			return;
		}
		MonoBehaviour monoBehaviour = playerObject.GetComponent("DbUnityMovement") as MonoBehaviour;
		Rigidbody component = playerObject.GetComponent<Rigidbody>();
		if (monoBehaviour != null)
		{
			monoBehaviour.enabled = !shouldFreeze;
		}
		if (component != null)
		{
			if (shouldFreeze)
			{
				component.velocity = Vector3.zero;
				component.angularVelocity = Vector3.zero;
				component.isKinematic = true;
			}
			else
			{
				component.isKinematic = false;
			}
		}
	}
}

