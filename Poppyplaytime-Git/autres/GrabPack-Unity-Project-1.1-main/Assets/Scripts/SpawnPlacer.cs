using UnityEngine;

public class SpawnPlacer : MonoBehaviour
{
    public Transform spawnPoint;
    public LayerMask placeMask;

    public KeyCode placeKey = KeyCode.P;

    void Update()
    {
        if (Input.GetKeyDown(placeKey))
        {
            PlaceSpawn();
        }
    }

    void PlaceSpawn()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, placeMask))
        {
            spawnPoint.position = hit.point;
            spawnPoint.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }
    }
}