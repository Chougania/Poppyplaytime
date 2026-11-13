using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class GrabpackHand : NetworkBehaviour
{
	public enum HandSide
	{
		LeftHand,
		RightHand
	}

	public enum HandState
	{
		Idle,
		Shooting,
		Attached,
		Retracting
	}

	[Header("Hand Identity")]
	public HandSide handSide;

	[Header("Hand Setup")]
	public GameObject handObject;

	public Transform handSpawnPoint;

	[Header("Hand Physics & Distance")]
	public float maxRange = 25f;

	public float shootSpeed = 40f;

	public float retractSpeed = 30f;

	public LayerMask grabableLayers;

	private LineRenderer ropeRenderer;

	private Camera playerCamera;

	private HandState currentState;

	private Vector3 targetHandPosition;

	private Vector3 currentHandPosition;

	private Quaternion shootRotation;

	private GrabpackDraggableDoor attachedDoor;

	private Transform originalParent;

	public HandState CurrentState => currentState;

	private void Awake()
	{
		ropeRenderer = GetComponent<LineRenderer>();
		ropeRenderer.startWidth = 0.05f;
		ropeRenderer.endWidth = 0.05f;
		ropeRenderer.positionCount = 0;
	}

	private void Start()
	{
		playerCamera = Camera.main;
		if (handObject != null)
		{
			originalParent = handObject.transform.parent;
		}
	}

	private void Update()
	{
		if (IsOwner)
		{
			UpdateHandMovement();
			UpdateRopeVisuals();
		}
	}

	public void Fire(Vector3 targetPoint, GrabpackDraggableDoor door)
	{
		if (currentState == HandState.Idle && !(handObject == null) && !(handSpawnPoint == null))
		{
			currentState = HandState.Shooting;
			currentHandPosition = handSpawnPoint.position;
			targetHandPosition = targetPoint;
			attachedDoor = door;
			shootRotation = playerCamera.transform.rotation;
			handObject.transform.SetParent(null);
		}
	}

	public void Retract()
	{
		if (currentState != HandState.Idle && currentState != HandState.Retracting)
		{
			currentState = HandState.Retracting;
			if (attachedDoor != null)
			{
				attachedDoor.StopGrabInteraction();
				attachedDoor = null;
			}
			if (handObject != null)
			{
				handObject.transform.SetParent(null);
			}
		}
	}

	private void UpdateHandMovement()
	{
		if (handObject == null || handSpawnPoint == null)
		{
			return;
		}
		switch (currentState)
		{
		case HandState.Shooting:
		{
			currentHandPosition = Vector3.MoveTowards(currentHandPosition, targetHandPosition, shootSpeed * Time.deltaTime);
			handObject.transform.position = currentHandPosition;
			handObject.transform.rotation = shootRotation;
			if (!(Vector3.Distance(currentHandPosition, targetHandPosition) < 0.1f))
			{
				break;
			}
			Collider[] array = Physics.OverlapSphere(currentHandPosition, 0.2f, grabableLayers);
			bool flag = false;
			Collider[] array2 = array;
			foreach (Collider collider in array2)
			{
				if (collider.CompareTag("Grab") || collider.CompareTag("Door") || collider.CompareTag("Battery") || LayerMask.LayerToName(collider.gameObject.layer) == "Grabable")
				{
					currentState = HandState.Attached;
					handObject.transform.SetParent(collider.transform);
					flag = true;
					break;
				}
			}
			if (!flag && attachedDoor != null)
			{
				currentState = HandState.Attached;
				handObject.transform.SetParent(attachedDoor.transform);
				flag = true;
			}
			if (!flag)
			{
				Retract();
			}
			break;
		}
		case HandState.Attached:
			currentHandPosition = handObject.transform.position;
			break;
		case HandState.Retracting:
			currentHandPosition = Vector3.MoveTowards(currentHandPosition, handSpawnPoint.position, retractSpeed * Time.deltaTime);
			handObject.transform.position = currentHandPosition;
			handObject.transform.rotation = shootRotation;
			if (Vector3.Distance(currentHandPosition, handSpawnPoint.position) < 0.2f)
			{
				ResetHandToIdle();
			}
			break;
		}
	}

	private void ResetHandToIdle()
	{
		currentState = HandState.Idle;
		ropeRenderer.positionCount = 0;
		if (handObject != null && handSpawnPoint != null)
		{
			handObject.transform.SetParent(originalParent);
			handObject.transform.position = handSpawnPoint.position;
			handObject.transform.rotation = handSpawnPoint.rotation;
		}
	}

	private void UpdateRopeVisuals()
	{
		if (currentState != HandState.Idle && !(handSpawnPoint == null))
		{
			ropeRenderer.positionCount = 2;
			ropeRenderer.SetPosition(0, handSpawnPoint.position);
			ropeRenderer.SetPosition(1, currentHandPosition);
		}
	}
}

