using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(LineRenderer))]
public class GrabpackHand : NetworkBehaviour
{
    public enum HandSide { LeftHand, RightHand }
    public enum HandState { Idle, Shooting, Attached, Retracting }

    [Header("Hand Identity")]
    public HandSide handSide;

    [Header("Hand Setup")]
    public GameObject handObject;
    public Transform handSpawnPoint;

    [Header("Hand Physics & Distance")]
    [Min(0.1f)] public float maxRange = 25f;
    [Min(0.1f)] public float shootSpeed = 40f;
    [Min(0.1f)] public float retractSpeed = 30f;
    public LayerMask grabableLayers;

    [Header("Rope Visuals")]
    [Min(0.001f)] public float ropeStartWidth = 0.055f;
    [Min(0.001f)] public float ropeEndWidth = 0.035f;
    public Gradient ropeGradient;
    [Min(0)] public int ropeCornerVertices = 4;
    [Min(0)] public int ropeCapVertices = 4;

    private LineRenderer ropeRenderer;
    private Camera playerCamera;
    private HandState currentState;
    private Vector3 targetHandPosition;
    private Vector3 currentHandPosition;
    private Quaternion shootRotation;
    private GrabpackDraggableDoor attachedDoor;
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    public HandState CurrentState => currentState;

    private void Awake()
    {
        ropeRenderer = GetComponent<LineRenderer>();
        ConfigureRope();
        currentState = HandState.Idle;
    }

    private void Start()
    {
        playerCamera = GetComponentInParent<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (handObject != null)
        {
            originalParent = handObject.transform.parent;
            originalLocalPosition = handObject.transform.localPosition;
            originalLocalRotation = handObject.transform.localRotation;
        }

        if (handSpawnPoint != null && handObject != null)
            ResetHandToIdle();
    }

    private void ConfigureRope()
    {
        if (ropeRenderer == null)
            return;

        ropeRenderer.positionCount = 0;
        ropeRenderer.useWorldSpace = true;
        ropeRenderer.startWidth = Mathf.Max(0.001f, ropeStartWidth);
        ropeRenderer.endWidth = Mathf.Max(0.001f, ropeEndWidth);
        ropeRenderer.numCornerVertices = ropeCornerVertices;
        ropeRenderer.numCapVertices = ropeCapVertices;
        ropeRenderer.alignment = LineAlignment.View;
        ropeRenderer.textureMode = LineTextureMode.Tile;
        ropeRenderer.shadowCastingMode = ShadowCastingMode.Off;
        ropeRenderer.receiveShadows = false;

        if (ropeGradient == null || ropeGradient.colorKeys == null || ropeGradient.colorKeys.Length == 0)
        {
            Color ropeColor = handSide == HandSide.LeftHand
                ? new Color(0.12f, 0.65f, 1f, 1f)
                : new Color(1f, 0.22f, 0.16f, 1f);

            ropeGradient = new Gradient();
            ropeGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(ropeColor, 0f),
                    new GradientColorKey(ropeColor * 0.8f, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.85f, 1f)
                });
        }

        ropeRenderer.colorGradient = ropeGradient;
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner)
            return;

        UpdateHandMovement();
        UpdateRopeVisuals();
    }

    public void Fire(Vector3 targetPoint, GrabpackDraggableDoor door)
    {
        if (currentState != HandState.Idle || handObject == null || handSpawnPoint == null)
            return;

        Vector3 direction = targetPoint - handSpawnPoint.position;
        if (direction.sqrMagnitude > maxRange * maxRange)
            targetPoint = handSpawnPoint.position + direction.normalized * maxRange;

        currentState = HandState.Shooting;
        currentHandPosition = handSpawnPoint.position;
        targetHandPosition = targetPoint;
        attachedDoor = door;
        shootRotation = playerCamera != null
            ? playerCamera.transform.rotation
            : handSpawnPoint.rotation;

        handObject.transform.SetParent(null, true);
        handObject.transform.position = currentHandPosition;
        handObject.transform.rotation = shootRotation;

        if (ropeRenderer != null)
            ropeRenderer.enabled = true;
    }

    public void Retract()
    {
        if (currentState == HandState.Idle || currentState == HandState.Retracting)
            return;

        currentState = HandState.Retracting;

        if (attachedDoor != null)
        {
            attachedDoor.StopGrabInteraction();
            attachedDoor = null;
        }

        if (handObject != null)
            handObject.transform.SetParent(null, true);
    }

    private void UpdateHandMovement()
    {
        if (handObject == null || handSpawnPoint == null)
            return;

        switch (currentState)
        {
            case HandState.Shooting:
                currentHandPosition = Vector3.MoveTowards(
                    currentHandPosition,
                    targetHandPosition,
                    Mathf.Max(0.1f, shootSpeed) * Time.deltaTime);

                handObject.transform.SetPositionAndRotation(currentHandPosition, shootRotation);

                if ((currentHandPosition - targetHandPosition).sqrMagnitude > 0.01f)
                    break;

                AttachToTarget();
                break;

            case HandState.Attached:
                currentHandPosition = handObject.transform.position;
                break;

            case HandState.Retracting:
                currentHandPosition = Vector3.MoveTowards(
                    currentHandPosition,
                    handSpawnPoint.position,
                    Mathf.Max(0.1f, retractSpeed) * Time.deltaTime);

                handObject.transform.SetPositionAndRotation(currentHandPosition, shootRotation);

                if ((currentHandPosition - handSpawnPoint.position).sqrMagnitude <= 0.04f)
                    ResetHandToIdle();
                break;
        }
    }

    private void AttachToTarget()
    {
        Collider[] hits = Physics.OverlapSphere(currentHandPosition, 0.2f, grabableLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
                continue;

            bool isGrabable = hit.CompareTag("Grab")
                || hit.CompareTag("Door")
                || hit.CompareTag("Battery")
                || LayerMask.LayerToName(hit.gameObject.layer) == "Grabable";

            if (!isGrabable)
                continue;

            currentState = HandState.Attached;
            handObject.transform.SetParent(hit.transform, true);
            currentHandPosition = handObject.transform.position;
            return;
        }

        if (attachedDoor != null)
        {
            currentState = HandState.Attached;
            handObject.transform.SetParent(attachedDoor.transform, true);
            currentHandPosition = handObject.transform.position;
            return;
        }

        Retract();
    }

    private void ResetHandToIdle()
    {
        currentState = HandState.Idle;
        attachedDoor = null;

        if (ropeRenderer != null)
        {
            ropeRenderer.positionCount = 0;
            ropeRenderer.enabled = false;
        }

        if (handObject == null || handSpawnPoint == null)
            return;

        handObject.transform.SetParent(originalParent, false);
        handObject.transform.localPosition = originalLocalPosition;
        handObject.transform.localRotation = originalLocalRotation;
        handObject.transform.SetPositionAndRotation(handSpawnPoint.position, handSpawnPoint.rotation);

        currentHandPosition = handSpawnPoint.position;
        targetHandPosition = currentHandPosition;
    }

    private void UpdateRopeVisuals()
    {
        if (ropeRenderer == null || handSpawnPoint == null || currentState == HandState.Idle)
            return;

        if (!ropeRenderer.enabled)
            ropeRenderer.enabled = true;

        if (ropeRenderer.positionCount != 2)
            ropeRenderer.positionCount = 2;

        ropeRenderer.SetPosition(0, handSpawnPoint.position);
        ropeRenderer.SetPosition(1, currentHandPosition);
    }

    private void OnDisable()
    {
        if (ropeRenderer != null)
        {
            ropeRenderer.positionCount = 0;
            ropeRenderer.enabled = false;
        }
    }
}
