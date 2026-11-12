using UnityEngine;
using UnityEngine.UI;

public class RuntimePrefabPlacer : MonoBehaviour
{
    public PrefabCatalogEntry[] prefabs;
    public LayerMask placeMask;
    public LayerMask deleteMask;

    public Material ghostMaterial;

    public float rotateAmount = 45f;
    float rotateTimer = 0f;
    public float rotateRepeatDelay = 0.15f;


    GameObject grabbedObject;
    bool isMovingObject = false;

    GameObject ghostObject;
    int currentIndex = 0;
    Vector3 currentRotation = Vector3.zero;
    public Image crosshair;


    public float placementOffset = 0f;
    public float offsetSpeed = 2f;

    float[] gridSizes = { 0f, 0.5f, 1f, 2f };
    int gridIndex = 0;

    enum Mode
    {
        Place,
        Destroy,
        Edit
    }

    Mode currentMode = Mode.Place;

    enum RotationAxis
    {
        X,
        Y,
        Z
    }

    RotationAxis currentAxis = RotationAxis.Y;

    void Start()
    {
        CreateGhost();
        UpdateCrosshair();
    }



    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 18;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;

        float width = 300;
        float height = 50;

        string text =
        "Axis: " + currentAxis +
        " | Rotation: " + currentRotation.ToString("F1") +
        " | Grid: " + (gridSizes[gridIndex] == 0 ? "Off" : gridSizes[gridIndex].ToString());


        GUI.Label(
            new Rect((Screen.width - width) / 2, Screen.height - height - 50, width, height),
            "Mode: " + currentMode,
            style
        );

            GUI.Label(
        new Rect((Screen.width - width) / 2, Screen.height - 700, width, height),
        text,
        style
    );
    }

    void OnEnable()
    {
        CreateGhost();
    }

    void OnDisable()
    {
        if (ghostObject != null)
            Destroy(ghostObject);
    }

    void Update()
    {
        HandlePrefabSwitch();

        if (currentMode == Mode.Place)
        {
            UpdateGhost();
        }

        if (currentMode == Mode.Edit)
        {
            HandleEditMode();
        }

        float scroll = Input.mouseScrollDelta.y;

        if (Input.GetKey(KeyCode.LeftControl))
        {
            placementOffset += scroll * offsetSpeed;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            currentRotation = Vector3.zero;
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            gridIndex++;
            if (gridIndex >= gridSizes.Length)
                gridIndex = 0;
        }

        if (Input.GetKey(KeyCode.R))
        {
            rotateTimer -= Time.deltaTime;

            if (rotateTimer <= 0f)
            {
                float direction = Input.GetKey(KeyCode.LeftShift) ? -1f : 1f;

                switch (currentAxis)
                {
                    case RotationAxis.X:
                        currentRotation.x = (currentRotation.x + rotateAmount * direction) % 360f;
                        break;

                    case RotationAxis.Y:
                        currentRotation.y = (currentRotation.y + rotateAmount * direction) % 360f;
                        break;

                    case RotationAxis.Z:
                        currentRotation.z = (currentRotation.z + rotateAmount * direction) % 360f;
                        break;
                }
                rotateTimer = rotateRepeatDelay;
            }
        }
        else
        {
            rotateTimer = 0f;
        }


        if (Input.GetMouseButtonDown(0))
        {
            if (currentMode == Mode.Place)
            {
                Instantiate(prefabs[currentIndex].prefab, ghostObject.transform.position, ghostObject.transform.rotation);
            }
            else if (currentMode == Mode.Destroy)
            {
                TryDelete();
            }
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            int next = ((int)currentMode + 1) % 3;
            currentMode = (Mode)next;

            if (ghostObject != null)
                ghostObject.SetActive(currentMode == Mode.Place || isMovingObject);

            UpdateCrosshair();
        }

        if (Input.GetKeyDown(KeyCode.X))
            currentAxis = RotationAxis.X;

        if (Input.GetKeyDown(KeyCode.Y))
            currentAxis = RotationAxis.Y;

        if (Input.GetKeyDown(KeyCode.Z))
            currentAxis = RotationAxis.Z;
    }

    void HandlePrefabSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetPrefab(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetPrefab(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetPrefab(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetPrefab(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetPrefab(4);
    }

    void UpdateCrosshair()
    {
        if (currentMode == Mode.Place)
        {
            crosshair.color = Color.white;
        }
        if (currentMode == Mode.Destroy)
        {
            crosshair.color = Color.red;
        }
        if (currentMode == Mode.Edit)
        {
            crosshair.color = Color.yellow;
        }
    }

    public void SetPrefab(int index)
    {
        if (index >= prefabs.Length) return;

        currentIndex = index;
        currentRotation = Vector3.zero;

        if (ghostObject != null)
            Destroy(ghostObject);

        CreateGhost();
    }

    void CreateGhost()
    {
        if (ghostObject != null)
            Destroy(ghostObject);

        ghostObject = Instantiate(prefabs[currentIndex].prefab);

        foreach (Collider c in ghostObject.GetComponentsInChildren<Collider>())
            Destroy(c);

        ghostObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        foreach (Transform t in ghostObject.GetComponentsInChildren<Transform>())
        {
            t.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        foreach (MonoBehaviour script in ghostObject.GetComponentsInChildren<MonoBehaviour>())
            script.enabled = false;

        Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            Material[] mats = new Material[r.materials.Length];

            for (int i = 0; i < mats.Length; i++)
                mats[i] = ghostMaterial;

            r.materials = mats;

            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    void UpdateGhost()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, placeMask))
        {
            Vector3 pos = hit.point + hit.normal * placementOffset;
            ghostObject.transform.position = SnapPosition(pos);

            Quaternion surfaceRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Vector3 axisRot = currentRotation;

            ghostObject.transform.rotation = surfaceRot * Quaternion.Euler(axisRot);
        }
    }

    void TryDelete()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, deleteMask))
        {
            Destroy(hit.transform.root.gameObject);
        }
    }

    void HandleEditMode()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (!isMovingObject)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, deleteMask))
                {
                    grabbedObject = hit.transform.gameObject;

                    Vector3 startPos = grabbedObject.transform.position;
                    Quaternion startRot = grabbedObject.transform.rotation;

                    if (ghostObject != null)
                        Destroy(ghostObject);

                    ghostObject = Instantiate(grabbedObject);
                    ghostObject.transform.position = startPos;
                    ghostObject.transform.rotation = startRot;

                    foreach (Collider c in ghostObject.GetComponentsInChildren<Collider>())
                    {
                        Destroy(c);
                    }

                    foreach (Transform t in ghostObject.GetComponentsInChildren<Transform>())
                    {
                        t.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                    }

                    grabbedObject.SetActive(false);

                    isMovingObject = true;
                }
            }
        }
        else
        {
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, placeMask))
            {
                Vector3 pos = hit.point + hit.normal * placementOffset;
                ghostObject.transform.position = SnapPosition(pos);

                Quaternion surfaceRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Vector3 axisRot = currentRotation;

                ghostObject.transform.rotation = surfaceRot * Quaternion.Euler(axisRot);
            }

            if (Input.GetMouseButtonDown(0))
            {
                grabbedObject.SetActive(true);

                grabbedObject.transform.position = ghostObject.transform.position;
                grabbedObject.transform.rotation = ghostObject.transform.rotation;

                Destroy(ghostObject);

                isMovingObject = false;
                grabbedObject = null;

                CreateGhost();
            }
        }
    }

    Vector3 SnapPosition(Vector3 pos)
    {
        float grid = gridSizes[gridIndex];

        if (grid == 0f)
            return pos;

        pos.x = Mathf.Round(pos.x / grid) * grid;
        pos.y = Mathf.Round(pos.y / grid) * grid;
        pos.z = Mathf.Round(pos.z / grid) * grid;

        return pos;
    }
}