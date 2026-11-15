using UnityEngine;

// Affiche un point de scène (nom + flèche d'orientation) dans l'éditeur. Aucun effet en jeu.
public class ScenePointGizmo : MonoBehaviour
{
	public string label = "";
	public Color color = new Color(1f, 0.82f, 0.1f);

#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		Gizmos.color = color;
		Gizmos.DrawWireSphere(transform.position, 0.3f);
		Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
		Gizmos.DrawWireCube(transform.position + transform.forward * 1.5f, Vector3.one * 0.15f);
		UnityEditor.Handles.color = color;
		UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, string.IsNullOrEmpty(label) ? name : label);
	}
#endif
}
