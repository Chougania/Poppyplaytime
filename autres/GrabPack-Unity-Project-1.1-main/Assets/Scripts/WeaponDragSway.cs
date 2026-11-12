using UnityEngine;

public class WeaponDragSway : MonoBehaviour
{
    public float swayAmount = 0.05f;
    public float smoothSpeed = 8f;

    public float rotationSwayAmount = 2f;
    public float rotationSmoothSpeed = 10f;

    private Vector3 currentOffset;
    private Vector3 velocity;

    private Quaternion currentRotation = Quaternion.identity;

    public Transform reference;
    public Rigidbody rb;

    public Vector3 restPosition;

    public MobileIcons mobileIcons;

    public float wallCheckDistance = 0.7f;
    public float sphereRadius = 0.15f;
    public float wallPushback = 0.5f;
    public float pushbackSmoothSpeed = 10f;
    public LayerMask wallLayers;
    public Transform cameraTransform;

    float currentPushback = 0f;

    void LateUpdate()
    {
        Vector3 localVelocity = reference.InverseTransformDirection(rb.linearVelocity);
        float forwardSpeed = Mathf.Max(0f, localVelocity.z);

        Vector3 targetOffset = new Vector3(
            0f,
            0f,
            -forwardSpeed * swayAmount
        );

        currentOffset = Vector3.SmoothDamp(
            currentOffset,
            targetOffset,
            ref velocity,
            1f / smoothSpeed
        );

        Vector3 animPos = transform.localPosition;
        Vector3 finalPosition = animPos + currentOffset;

        RaycastHit hit;
        float targetPushback = 0f;

        bool hitWall = Physics.SphereCast(
            cameraTransform.position,
            sphereRadius,
            cameraTransform.forward,
            out hit,
            wallCheckDistance,
            wallLayers
        );

        if (hitWall)
        {
            targetPushback = (wallCheckDistance - hit.distance) * wallPushback;
        }
        else
        {
            if (Physics.CheckSphere(
                cameraTransform.position + cameraTransform.forward * sphereRadius,
                sphereRadius,
                wallLayers))
            {
                targetPushback = wallCheckDistance * wallPushback;
            }
        }

        targetPushback = Mathf.Clamp(targetPushback, 0f, 0.4f);

        currentPushback = Mathf.Lerp(
            currentPushback,
            targetPushback,
            Time.deltaTime * pushbackSmoothSpeed
        );

        finalPosition.z -= currentPushback;

        transform.localPosition = finalPosition;

        if (!mobileIcons.isMobile)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            Vector3 rotationOffset = new Vector3(
                -mouseY,
                -mouseX,
                0f
            ) * rotationSwayAmount;

            Quaternion targetRotation = Quaternion.Euler(rotationOffset);

            currentRotation = Quaternion.Slerp(
                currentRotation,
                targetRotation,
                Time.deltaTime * rotationSmoothSpeed
            );

            transform.localRotation = transform.localRotation * currentRotation;
        }
    }

    public Vector3 GetCurrentOffset()
    {
        return currentOffset;
    }

    public Quaternion GetCurrentRotationOffset()
    {
        return currentRotation;
    }

    public Vector3 GetTotalPositionOffset()
    {
        return restPosition + currentOffset;
    }
}