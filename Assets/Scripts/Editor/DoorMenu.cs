#if UNITY_EDITOR
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu Poppy > Créer une porte qui se ferme : fabrique une porte-herse prête à l'emploi dans la scène.
public static class DoorMenu
{
	[MenuItem("Poppy/Créer une porte qui se ferme (herse)")]
	public static void CreateDoor()
	{
		Vector3 basePos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;

		GameObject door = new GameObject("Porte herse");
		Undo.RegisterCreatedObjectUndo(door, "Créer une porte");
		// position de départ = porte levée (ouverte) : place-la au-dessus du passage, elle descendra pour le fermer
		door.transform.position = basePos + Vector3.up * 3f;

		int doorLayer = LayerMask.NameToLayer("GrabDoor");
		if (doorLayer >= 0)
		{
			door.layer = doorLayer;
		}
		try
		{
			door.tag = "Door";
		}
		catch (UnityException)
		{
			Debug.LogWarning("Le tag 'Door' n'existe pas : ajoute-le dans Tags & Layers.");
		}

		GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
		visual.name = "Visuel";
		visual.transform.SetParent(door.transform, false);
		visual.transform.localPosition = new Vector3(0f, 1.5f, 0f);
		visual.transform.localScale = new Vector3(2.4f, 3f, 0.2f);
		Material mat = new Material(Shader.Find("Standard"));
		mat.color = new Color(0.16f, 0.17f, 0.2f);
		visual.GetComponent<Renderer>().sharedMaterial = mat;
		if (doorLayer >= 0)
		{
			visual.layer = doorLayer;
		}
		visual.tag = door.tag;

		Rigidbody rb = door.AddComponent<Rigidbody>();
		rb.isKinematic = true;
		rb.useGravity = false;
		door.AddComponent<NetworkObject>();
		GrabpackDraggableDoor script = door.AddComponent<GrabpackDraggableDoor>();
		script.maxPullDistance = 3f;

		Selection.activeGameObject = door;
		EditorSceneManager.MarkSceneDirty(door.scene);
		Debug.Log("Porte créée : elle est LEVÉE (ouverte). Place-la au-dessus d'un passage de 3 m de haut : maintenir le clic avec le Grabpack la descend (ferme), de nouveau la remonte. Relance 'Poppy > Reconstruire le NavMesh' ensuite.");
	}
}
#endif
