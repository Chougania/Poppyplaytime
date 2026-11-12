using UnityEngine;

public class JumpscareTrigger : MonoBehaviour
{
    [SerializeField] private GameObject jumpscarePrefab;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Transform player = other.transform;

        Instantiate(
            jumpscarePrefab,
            transform.position,
            transform.rotation);

        player.gameObject.SetActive(false);
        transform.root.gameObject.SetActive(false);

    }
}