using System;
using Unity.Netcode;
using UnityEngine;

public class GrabpackController : NetworkBehaviour
{
	[Header("Hand References")]
	public GrabpackHand blueHand;

	public GrabpackHand redHand;

	[Header("Detection Settings")]
	public Transform playerCamera;

	public float grabDistance = 25f;

	public LayerMask interactableLayer;

	[Header("Physics & Movement (Player)")]
	public float pullStrength = 15f;

	public float minPullDistance = 1.5f;

	[Header("Dynamic Object Reel Settings")]
	public float reelPower = 150f;

	public float reelDampening = 5f;

	public float maxReelVelocity = 25f;

	private CharacterController _controller;

	private GrabpackDraggableDoor activeBlueDoor;

	private GrabpackDraggableDoor activeRedDoor;

	private Rigidbody reeledObjectBlue;

	private Rigidbody reeledObjectRed;

	private NetworkVariable<NetworkObjectReference> heldBatteryBlue = new NetworkVariable<NetworkObjectReference>();

	private NetworkVariable<NetworkObjectReference> heldBatteryRed = new NetworkVariable<NetworkObjectReference>();

	private void Awake()
	{
		_controller = GetComponent<CharacterController>();
	}

	public override void OnNetworkSpawn()
	{
	}

	private void Update()
	{
		if (!IsSpawned || !IsOwner)
			return;

		UpdateBatteryPositions();
		if (Input.GetMouseButtonDown(0))
		{
			TryFireHand(blueHand, 0);
		}
		if (Input.GetMouseButtonUp(0))
		{
			RetractHandAndReleaseDoor(blueHand, 0);
		}
		if (Input.GetMouseButtonDown(1))
		{
			TryFireHand(redHand, 1);
		}
		if (Input.GetMouseButtonUp(1))
		{
			RetractHandAndReleaseDoor(redHand, 1);
		}
		if (Input.GetKeyDown(KeyCode.G))
		{
			if (HasBattery(heldBatteryBlue))
			{
				DropBatteryServerRpc(0);
			}
			else if (HasBattery(heldBatteryRed))
			{
				DropBatteryServerRpc(1);
			}
		}
		HandleDoorDragging();
	}

	private void FixedUpdate()
	{
		if (IsOwner)
		{
			HandleGrabpackPhysics();
			HandleReelInPhysics();
		}
	}

	private void TryFireHand(GrabpackHand hand, int mouseButton)
	{
		if (hand == null || hand.CurrentState != GrabpackHand.HandState.Idle || (mouseButton == 0 && HasBattery(heldBatteryBlue)) || (mouseButton == 1 && HasBattery(heldBatteryRed)))
		{
			return;
		}
		GrabpackDraggableDoor door = null;
		Vector3 targetPoint;
		if (playerCamera == null)
		{
			Debug.LogWarning("GrabpackController: Player camera is not assigned.", this);
			return;
		}

		if (Physics.Raycast(new Ray(playerCamera.position, playerCamera.forward), out var hitInfo, Mathf.Max(0.1f, grabDistance), interactableLayer, QueryTriggerInteraction.Ignore))
		{
			targetPoint = hitInfo.point;
			if (hitInfo.collider.CompareTag("Battery"))
			{
				NetworkObject componentInParent = hitInfo.collider.GetComponentInParent<NetworkObject>();
				if (componentInParent != null)
				{
					GrabBatteryServerRpc(componentInParent, mouseButton);
					hand.Fire(targetPoint, null);
					hand.Retract();
					return;
				}
			}
			GrabpackDraggableDoor grabpackDraggableDoor = hitInfo.collider.GetComponent<GrabpackDraggableDoor>() ?? hitInfo.collider.GetComponentInParent<GrabpackDraggableDoor>();
			if (grabpackDraggableDoor != null)
			{
				door = grabpackDraggableDoor;
				if (mouseButton == 0)
				{
					activeBlueDoor = door;
				}
				else
				{
					activeRedDoor = door;
				}
				NetworkObject networkObject = grabpackDraggableDoor.GetComponent<NetworkObject>() ?? grabpackDraggableDoor.GetComponentInParent<NetworkObject>();
				if (networkObject != null && !networkObject.IsOwner)
				{
					RequestNetworkOwnershipServerRpc(networkObject);
				}
			}
			else if (hitInfo.collider.CompareTag("Grab") || hitInfo.collider.CompareTag("Door"))
			{
				Rigidbody rigidbody = hitInfo.collider.GetComponent<Rigidbody>() ?? hitInfo.collider.GetComponentInParent<Rigidbody>();
				NetworkObject componentInParent2 = hitInfo.collider.GetComponentInParent<NetworkObject>();
				if (rigidbody != null)
				{
					if (mouseButton == 0)
					{
						reeledObjectBlue = rigidbody;
					}
					else
					{
						reeledObjectRed = rigidbody;
					}
					if (componentInParent2 != null && !componentInParent2.IsOwner)
					{
						RequestNetworkOwnershipServerRpc(componentInParent2);
					}
				}
			}
		}
		else
		{
			targetPoint = playerCamera.position + playerCamera.forward * grabDistance;
		}
		hand.Fire(targetPoint, door);
	}

	private void UpdateBatteryPositions()
	{
		if (heldBatteryBlue.Value.TryGet(out var networkObject) && blueHand != null && blueHand.handSpawnPoint != null)
		{
			networkObject.transform.position = blueHand.handSpawnPoint.position;
			networkObject.transform.rotation = blueHand.handSpawnPoint.rotation;
		}
		if (heldBatteryRed.Value.TryGet(out var networkObject2) && redHand != null && redHand.handSpawnPoint != null)
		{
			networkObject2.transform.position = redHand.handSpawnPoint.position;
			networkObject2.transform.rotation = redHand.handSpawnPoint.rotation;
		}
	}

	private bool HasBattery(NetworkVariable<NetworkObjectReference> batteryVar)
	{
		NetworkObject networkObject;
		return batteryVar.Value.TryGet(out networkObject);
	}

	private void HandleDoorDragging()
	{
		if (Input.GetMouseButton(0) && activeBlueDoor != null)
		{
			activeBlueDoor.StartGrabInteraction();
		}
		if (Input.GetMouseButton(1) && activeRedDoor != null)
		{
			activeRedDoor.StartGrabInteraction();
		}
	}

	private void HandleReelInPhysics()
	{
		if (Input.GetMouseButton(0) && reeledObjectBlue != null && blueHand != null && blueHand.handSpawnPoint != null)
		{
			ApplyMassBasedForce(reeledObjectBlue, blueHand.handSpawnPoint.position);
		}
		if (Input.GetMouseButton(1) && reeledObjectRed != null && redHand != null && redHand.handSpawnPoint != null)
		{
			ApplyMassBasedForce(reeledObjectRed, redHand.handSpawnPoint.position);
		}
	}

	private void ApplyMassBasedForce(Rigidbody targetRb, Vector3 targetMuzzlePosition)
	{
		Vector3 vector = targetMuzzlePosition - targetRb.position;
		if (vector.magnitude > 0.6f)
		{
			Vector3 vector2 = vector.normalized * reelPower;
			if (targetRb.CompareTag("Door"))
			{
				vector2.x = 0f;
				vector2.z = 0f;
			}
			Vector3 velocity = targetRb.velocity;
			Vector3 force = (vector2 - velocity) * reelDampening;
			targetRb.AddForce(force, ForceMode.Force);
			if (targetRb.velocity.magnitude > maxReelVelocity)
			{
				targetRb.velocity = targetRb.velocity.normalized * maxReelVelocity;
			}
		}
		else
		{
			targetRb.velocity = Vector3.MoveTowards(targetRb.velocity, Vector3.zero, Time.fixedDeltaTime * 10f);
		}
	}

	private void RetractHandAndReleaseDoor(GrabpackHand hand, int mouseButton)
	{
		if (hand != null)
		{
			hand.Retract();
		}
		switch (mouseButton)
		{
		case 0:
			if (activeBlueDoor != null)
			{
				NetworkObject networkObject2 = activeBlueDoor.GetComponent<NetworkObject>() ?? activeBlueDoor.GetComponentInParent<NetworkObject>();
				if (networkObject2 != null && networkObject2.IsOwner)
				{
					RemoveNetworkOwnershipServerRpc(networkObject2);
				}
				activeBlueDoor.StopGrabInteraction();
				activeBlueDoor = null;
			}
			if (reeledObjectBlue != null)
			{
				NetworkObject componentInParent2 = reeledObjectBlue.GetComponentInParent<NetworkObject>();
				if (componentInParent2 != null && componentInParent2.IsOwner)
				{
					RemoveNetworkOwnershipServerRpc(componentInParent2);
				}
				reeledObjectBlue = null;
			}
			break;
		case 1:
			if (activeRedDoor != null)
			{
				NetworkObject networkObject = activeRedDoor.GetComponent<NetworkObject>() ?? activeRedDoor.GetComponentInParent<NetworkObject>();
				if (networkObject != null && networkObject.IsOwner)
				{
					RemoveNetworkOwnershipServerRpc(networkObject);
				}
				activeRedDoor.StopGrabInteraction();
				activeRedDoor = null;
			}
			if (reeledObjectRed != null)
			{
				NetworkObject componentInParent = reeledObjectRed.GetComponentInParent<NetworkObject>();
				if (componentInParent != null && componentInParent.IsOwner)
				{
					RemoveNetworkOwnershipServerRpc(componentInParent);
				}
				reeledObjectRed = null;
			}
			break;
		}
	}

	private void HandleGrabpackPhysics()
	{
		Vector3 zero = Vector3.zero;
		zero += CalculatePull(blueHand);
		zero += CalculatePull(redHand);
		if (_controller != null && _controller.enabled && zero.sqrMagnitude > 0.01f)
		{
			_controller.Move(zero * Time.fixedDeltaTime);
		}
	}

	private Vector3 CalculatePull(GrabpackHand hand)
	{
		if (hand != null && hand.handObject != null && hand.CurrentState == GrabpackHand.HandState.Attached)
		{
			if (hand == blueHand && reeledObjectBlue != null)
			{
				return Vector3.zero;
			}
			if (hand == redHand && reeledObjectRed != null)
			{
				return Vector3.zero;
			}
			Vector3 vector = hand.handObject.transform.position - transform.position;
			if (vector.magnitude > minPullDistance)
			{
				return vector.normalized * pullStrength;
			}
		}
		return Vector3.zero;
	}

	public void ResetHandsBeforeDeath()
	{
		if (blueHand != null)
		{
			blueHand.Retract();
		}
		if (redHand != null)
		{
			redHand.Retract();
		}
		if (activeBlueDoor != null)
		{
			NetworkObject networkObject = activeBlueDoor.GetComponent<NetworkObject>() ?? activeBlueDoor.GetComponentInParent<NetworkObject>();
			if (networkObject != null && networkObject.IsOwner)
			{
				RemoveNetworkOwnershipServerRpc(networkObject);
			}
			activeBlueDoor.StopGrabInteraction();
			activeBlueDoor = null;
		}
		if (activeRedDoor != null)
		{
			NetworkObject networkObject2 = activeRedDoor.GetComponent<NetworkObject>() ?? activeRedDoor.GetComponentInParent<NetworkObject>();
			if (networkObject2 != null && networkObject2.IsOwner)
			{
				RemoveNetworkOwnershipServerRpc(networkObject2);
			}
			activeRedDoor.StopGrabInteraction();
			activeRedDoor = null;
		}
		if (reeledObjectBlue != null)
		{
			NetworkObject componentInParent = reeledObjectBlue.GetComponentInParent<NetworkObject>();
			if (componentInParent != null && componentInParent.IsOwner)
			{
				RemoveNetworkOwnershipServerRpc(componentInParent);
			}
			reeledObjectBlue = null;
		}
		if (reeledObjectRed != null)
		{
			NetworkObject componentInParent2 = reeledObjectRed.GetComponentInParent<NetworkObject>();
			if (componentInParent2 != null && componentInParent2.IsOwner)
			{
				RemoveNetworkOwnershipServerRpc(componentInParent2);
			}
			reeledObjectRed = null;
		}
		if (IsOwner)
		{
			if (HasBattery(heldBatteryBlue))
			{
				DropBatteryServerRpc(0);
			}
			if (HasBattery(heldBatteryRed))
			{
				DropBatteryServerRpc(1);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void GrabBatteryServerRpc(NetworkObjectReference batteryRef, int handSide)
	{
		if (batteryRef.TryGet(out var networkObject))
		{
			networkObject.ChangeOwnership(OwnerClientId);
			SyncBatteryPhysicsClientRpc(batteryRef, isHeld: true);
			if (handSide == 0)
			{
				heldBatteryBlue.Value = networkObject;
			}
			else
			{
				heldBatteryRed.Value = networkObject;
			}
		}
	}

	[ServerRpc]
	private void DropBatteryServerRpc(int handSide)
	{
		NetworkVariable<NetworkObjectReference> networkVariable = ((handSide == 0) ? heldBatteryBlue : heldBatteryRed);
		if (networkVariable.Value.TryGet(out var networkObject))
		{
			networkObject.RemoveOwnership();
			SyncBatteryPhysicsClientRpc(networkVariable.Value, isHeld: false);
			networkVariable.Value = default;
		}
	}

	[ClientRpc]
	private void SyncBatteryPhysicsClientRpc(NetworkObjectReference batteryRef, bool isHeld)
	{
		if (!batteryRef.TryGet(out var networkObject))
		{
			return;
		}
		Rigidbody component = networkObject.GetComponent<Rigidbody>();
		if (component != null)
		{
			component.useGravity = !isHeld;
			component.isKinematic = isHeld;
			if (!isHeld)
			{
				component.velocity = Vector3.zero;
				Vector3 dropDirection = playerCamera != null ? playerCamera.forward : transform.forward;
				component.AddForce(dropDirection * 4f, ForceMode.VelocityChange);
			}
		}
		networkObject.transform.SetParent(null);
	}

	[ServerRpc]
	private void RequestNetworkOwnershipServerRpc(NetworkObjectReference targetNetObjRef)
	{
		if (targetNetObjRef.TryGet(out var networkObject))
		{
			networkObject.ChangeOwnership(OwnerClientId);
		}
	}

	[ServerRpc]
	private void RemoveNetworkOwnershipServerRpc(NetworkObjectReference targetNetObjRef)
	{
		if (targetNetObjRef.TryGet(out var networkObject))
		{
			networkObject.RemoveOwnership();
		}
	}
}

