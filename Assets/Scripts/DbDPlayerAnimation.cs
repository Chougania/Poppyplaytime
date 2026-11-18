using Unity.Netcode;
using UnityEngine;

public class DbDPlayerAnimation : NetworkBehaviour
{
	public Animator dbUnityAnim;

	private DbUnityMovment _move;
	private float _nextMonsterAttackAnimation;

	private void Awake()
	{
		_move = GetComponent<DbUnityMovment>();
		FindAnimator();
	}

	private void Update()
	{
		if (_move == null)
			_move = GetComponent<DbUnityMovment>();

		// The survivor and Huggy meshes have separate Animators. Rebind after the
		// role swap so movement and attack triggers target the currently visible model.
		if (_move != null && _move.monsterModelContainer != null && _move.monsterModelContainer.activeInHierarchy)
		{
			if (dbUnityAnim == null || !dbUnityAnim.gameObject.activeInHierarchy ||
				dbUnityAnim.transform != _move.monsterModelContainer.transform &&
				!dbUnityAnim.transform.IsChildOf(_move.monsterModelContainer.transform))
				FindAnimator();
		}
		else if (dbUnityAnim == null || !dbUnityAnim.gameObject.activeInHierarchy)
		{
			FindAnimator();
		}

		if (_move != null && dbUnityAnim != null)
		{
			dbUnityAnim.SetFloat("Speed", _move.NetMoveSpeed.Value);
			dbUnityAnim.SetBool("IsCrouching", _move.IsCrouching.Value);
			dbUnityAnim.SetBool("IsSprinting", _move.IsSprinting.Value);
			dbUnityAnim.SetBool("IsWalking", _move.IsWalking.Value);
		}

		// DbUnityMovment already handles the hit detection on the owner's click.
		// This replicated trigger drives Huggy's visible Jumpscare/attack animation
		// on every peer, including the attacking player's active model.
		if (IsOwner && _move != null && _move.monsterModelContainer != null &&
			_move.monsterModelContainer.activeInHierarchy && Input.GetMouseButtonDown(0) &&
			!PoppyUIInputGate.IsGameplayBlocked && Time.unscaledTime >= _nextMonsterAttackAnimation)
		{
			_nextMonsterAttackAnimation = Time.unscaledTime + 0.8f;
			RequestMonsterAttackAnimationServerRpc();
		}
	}

	private void FindAnimator()
	{
		if (_move != null && _move.monsterModelContainer != null && _move.monsterModelContainer.activeInHierarchy)
		{
			Animator monsterAnimator = _move.monsterModelContainer.GetComponent<Animator>();
			if (monsterAnimator == null)
				monsterAnimator = _move.monsterModelContainer.GetComponentInChildren<Animator>(true);
			if (monsterAnimator != null && monsterAnimator.gameObject.activeInHierarchy)
			{
				dbUnityAnim = monsterAnimator;
				return;
			}
		}

		Animator[] componentsInChildren = GetComponentsInChildren<Animator>(true);
		foreach (Animator animator in componentsInChildren)
		{
			if (animator.gameObject.activeInHierarchy)
			{
				dbUnityAnim = animator;
				return;
			}
		}
		dbUnityAnim = null;
	}

	public void PlayJump()
	{
		dbUnityAnim?.SetTrigger("Jump");
	}

	public void PlayAttack()
	{
		dbUnityAnim?.SetTrigger("Attack");
	}

	public void PlayDeath()
	{
		dbUnityAnim?.SetTrigger("Death");
	}

	[ServerRpc]
	private void RequestMonsterAttackAnimationServerRpc()
	{
		PlayMonsterAttackAnimationClientRpc();
	}

	[ClientRpc]
	private void PlayMonsterAttackAnimationClientRpc()
	{
		if (_move == null)
			_move = GetComponent<DbUnityMovment>();
		if (_move == null || _move.monsterModelContainer == null || !_move.monsterModelContainer.activeInHierarchy)
			return;

		Animator animator = _move.monsterModelContainer.GetComponent<Animator>();
		if (animator == null)
			animator = _move.monsterModelContainer.GetComponentInChildren<Animator>(true);
		if (animator != null && animator.gameObject.activeInHierarchy)
			animator.SetTrigger("Jumpscare");
	}
}
