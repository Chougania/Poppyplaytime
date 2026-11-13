using UnityEngine;

public class BlacklightRay : MonoBehaviour
{
    public float range = 15f;
    public LayerMask uvLayer;

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range, uvLayer))
        {
            UVReveal reveal = hit.collider.GetComponent<UVReveal>();

            if (reveal != null)
            {
                reveal.Reveal();
            }
        }
    }
}