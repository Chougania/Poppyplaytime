using Unity.Netcode;
using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
	[Header("References")]
	public DbUnityMovment ownerMovement;

	public NetworkObject networkObject;

	private void Awake()
	{
		if (ownerMovement == null)
		{
			ownerMovement = GetComponentInParent<DbUnityMovment>();
		}
		if (networkObject == null)
		{
			networkObject = GetComponentInParent<NetworkObject>();
		}
	}
}

