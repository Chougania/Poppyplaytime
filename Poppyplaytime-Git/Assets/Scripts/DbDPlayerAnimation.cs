using Unity.Netcode;
using UnityEngine;

public class DbDPlayerAnimation : NetworkBehaviour
{
	public Animator dbUnityAnim;

	private DbUnityMovment _move;

	private void Awake()
	{
		_move = GetComponent<DbUnityMovment>();
		FindAnimator();
	}

	private void Update()
	{
		if (dbUnityAnim == null || !dbUnityAnim.gameObject.activeInHierarchy)
		{
			FindAnimator();
		}
		if (!(_move == null) && !(dbUnityAnim == null))
		{
			dbUnityAnim.SetFloat("Speed", _move.NetMoveSpeed.Value);
			dbUnityAnim.SetBool("IsCrouching", _move.IsCrouching.Value);
			dbUnityAnim.SetBool("IsSprinting", _move.IsSprinting.Value);
			dbUnityAnim.SetBool("IsWalking", _move.IsWalking.Value);
		}
	}

	private void FindAnimator()
	{
		Animator[] componentsInChildren = GetComponentsInChildren<Animator>(includeInactive: true);
		foreach (Animator animator in componentsInChildren)
		{
			if (animator.gameObject.activeInHierarchy)
			{
				dbUnityAnim = animator;
				break;
			}
		}
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
}

