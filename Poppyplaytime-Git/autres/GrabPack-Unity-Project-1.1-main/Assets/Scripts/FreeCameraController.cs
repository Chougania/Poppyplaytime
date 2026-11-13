using UnityEngine;

public class FreeCameraController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float boostMultiplier = 3f;
    public float lookSensitivity = 2.5f;
    public float scrollSpeed = 5f;

    float yaw;
    float pitch;

    void Start()
    {
        Vector3 rot = transform.eulerAngles;
        yaw = rot.y;
        pitch = rot.x;
    }

    void Update()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0 && !Input.GetKey(KeyCode.LeftControl))
        {
            moveSpeed += scroll * scrollSpeed;
            moveSpeed = Mathf.Clamp(moveSpeed, 1f, 100f);
        }

        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * lookSensitivity * 100f * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * lookSensitivity * 100f * Time.deltaTime;

            pitch = Mathf.Clamp(pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        float currentSpeed = moveSpeed;

        if (Input.GetKey(KeyCode.LeftShift))
            currentSpeed *= boostMultiplier;

        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(KeyCode.D)) move += transform.right;

        if (Input.GetKey(KeyCode.E)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;

        transform.position += move * currentSpeed * Time.deltaTime;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 18;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;

        float width = 300;
        float height = 30;

        GUI.Label(
            new Rect((Screen.width - width) / 2, Screen.height - height - 10, width, height),
            "Camera Speed: " + moveSpeed.ToString("F1"),
            style
        );
    }
}