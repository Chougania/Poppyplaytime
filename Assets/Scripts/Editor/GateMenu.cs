#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu Poppy > Créer une porte verrouillée : porte ouverte par un générateur, pour accéder à une nouvelle zone.
public static class GateMenu
{
	[MenuItem("Poppy/Créer une porte verrouillée (ouverte par un générateur)")]
	public static void CreateLockedGate()
	{
		Vector3 basePos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
		GameObject gate = new GameObject("Porte verrouillée");
		Undo.RegisterCreatedObjectUndo(gate, "Créer une porte verrouillée");
		gate.transform.position = basePos;

		GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
		visual.name = "Visuel";
		visual.transform.SetParent(gate.transform, false);
		visual.transform.localPosition = new Vector3(0f, 1.5f, 0f);
		visual.transform.localScale = new Vector3(2.4f, 3f, 0.25f);
		Material mat = new Material(Shader.Find("Standard"));
		mat.color = new Color(0.35f, 0.07f, 0.07f);
		visual.GetComponent<Renderer>().sharedMaterial = mat;

		GeneratorGate script = gate.AddComponent<GeneratorGate>();
		script.generatorNumber = 1;
		script.slideDown = true;

		Selection.activeGameObject = gate;
		EditorSceneManager.MarkSceneDirty(gate.scene);
		Debug.Log("Porte verrouillée créée : place-la dans un passage et choisis le générateur qui l'ouvre (1 à 4) dans l'Inspector. Relance 'Poppy > Reconstruire le NavMesh' ensuite.");
	}
}
#endif
