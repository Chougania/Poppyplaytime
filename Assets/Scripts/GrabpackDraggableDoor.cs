using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GrabpackDraggableDoor : NetworkBehaviour
{
	[Header("Door Movement Constraints")]
	[Tooltip("The maximum distance the door can be pulled down from its starting position.")]
	public float maxPullDistance = 3f;

	[Tooltip("How fast the door follows the mouse movement.")]
	public float pullSpeed = 10f;

	[Tooltip("How quickly the door settles when released.")]
	public float doorDamping = 5f;

	[Header("Network Synchronization")]
	private readonly NetworkVariable<float> netDoorYPosition = new NetworkVariable<float>(0f);

	private Rigidbody doorRigidbody;

	private Vector3 startingPosition;

	private bool isBeingDragged;

	private float targetYOffset;

	private Camera mainCamera;

	private float initialMouseY;

	private float initialDoorYOffset;

	private void Awake()
	{
		doorRigidbody = GetComponent<Rigidbody>();
		startingPosition = transform.position;
		doorRigidbody.constraints = (RigidbodyConstraints)122;
		doorRigidbody.useGravity = false;
		doorRigidbody.isKinematic = false;
		doorRigidbody.drag = doorDamping;
	}

	private void Start()
	{
		mainCamera = Camera.main;
	}

	private void Update()
	{
		if (IsServer)
		{
			if (!isBeingDragged)
			{
				float value = startingPosition.y - transform.position.y;
				netDoorYPosition.Value = value;
			}
		}
		else if (!isBeingDragged)
		{
			Vector3 b = new Vector3(transform.position.x, startingPosition.y - netDoorYPosition.Value, transform.position.z);
			transform.position = Vector3.Lerp(transform.position, b, Time.deltaTime * pullSpeed);
		}
		if (isBeingDragged)
		{
			HandleDraggingInput();
		}
	}

	private void FixedUpdate()
	{
		if (isBeingDragged)
		{
			float y = startingPosition.y - targetYOffset;
			Vector3 vector = new Vector3(doorRigidbody.position.x, y, doorRigidbody.position.z) - doorRigidbody.position;
			doorRigidbody.velocity = vector * pullSpeed;
		}
		else if (IsServer)
		{
			doorRigidbody.velocity = Vector3.MoveTowards(doorRigidbody.velocity, Vector3.zero, Time.fixedDeltaTime * doorDamping);
		}
	}

	public void StartGrabInteraction()
	{
		if (!isBeingDragged)
		{
			isBeingDragged = true;
			initialMouseY = Input.mousePosition.y;
			initialDoorYOffset = startingPosition.y - transform.position.y;
			if (NetworkManager.Singleton != null && !IsServer)
			{
				UpdateInteractionStateServerRpc(draggingState: true);
			}
		}
	}

	public void StopGrabInteraction()
	{
		if (isBeingDragged)
		{
			isBeingDragged = false;
			if (NetworkManager.Singleton != null && !IsServer)
			{
				UpdateInteractionStateServerRpc(draggingState: false);
			}
		}
	}

	private void HandleDraggingInput()
	{
		float y = Input.mousePosition.y;
		float num = initialMouseY - y;
		float num2 = 0.01f;
		float num3 = num * num2;
		targetYOffset = Mathf.Clamp(initialDoorYOffset + num3, 0f, maxPullDistance);
		if (NetworkManager.Singleton != null)
		{
			SyncPullOffsetServerRpc(targetYOffset);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void UpdateInteractionStateServerRpc(bool draggingState)
	{
		isBeingDragged = draggingState;
		if (!draggingState)
		{
			doorRigidbody.velocity = Vector3.zero;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void SyncPullOffsetServerRpc(float serverYOffset)
	{
			targetYOffset = serverYOffset;
			float y = startingPosition.y - targetYOffset;
			Vector3 position = new Vector3(doorRigidbody.position.x, y, doorRigidbody.position.z);
			doorRigidbody.MovePosition(position);
			netDoorYPosition.Value = serverYOffset;
	}
}

