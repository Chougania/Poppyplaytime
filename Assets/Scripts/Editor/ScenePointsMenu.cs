#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu Poppy > Créer les points de scène : crée tous les points optionnels reconnus par le jeu.
public static class ScenePointsMenu
{
	private struct Point
	{
		public string name;
		public string label;
		public Color color;

		public Point(string n, string l, Color c)
		{
			name = n;
			label = l;
			color = c;
		}
	}

	[MenuItem("Poppy/Créer le node boutique monstre (affichage en temps réel + skins)")]
	public static void CreateMonsterShopNode()
	{
		MonsterShopNode existing = Object.FindObjectOfType<MonsterShopNode>();
		if (existing != null)
		{
			Selection.activeGameObject = existing.gameObject;
			Debug.Log("Un MonsterShopNode existe déjà dans la scène : il est sélectionné.");
			return;
		}
		Vector3 basePos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
		GameObject node = new GameObject("MonsterShopNode");
		Undo.RegisterCreatedObjectUndo(node, "Créer le node boutique monstre");
		node.transform.position = basePos;
		MonsterShopNode comp = node.AddComponent<MonsterShopNode>();

		GameObject cam = new GameObject("ShopCameraAnchor");
		cam.transform.SetParent(node.transform, false);
		cam.transform.position = basePos + new Vector3(0f, 1.7f, 4.2f);
		cam.transform.rotation = Quaternion.LookRotation((basePos + Vector3.up * 1.4f) - cam.transform.position);
		comp.cameraAnchor = cam.transform;

		Selection.activeGameObject = node;
		EditorSceneManager.MarkSceneDirty(node.scene);
		Debug.Log("MonsterShopNode créé. Déplace le node (le monstre se tient dessus, face à l'axe bleu) et la ShopCameraAnchor (la caméra).");
	}

	[MenuItem("Poppy/Créer les points de scène (menu, boutique, joueurs, générateurs)")]
	public static void CreatePoints()
	{
		Color menu = new Color(1f, 0.82f, 0.1f);
		Color shop = new Color(0.3f, 0.8f, 1f);
		Color game = new Color(0.4f, 1f, 0.4f);
		Point[] points =
		{
			new Point("MenuCameraPoint", "Caméra du menu principal", menu),
			new Point("SpawnPlayerMenu", "Joueur dans le menu principal", menu),
			new Point("SpawnHuggyMenu", "Huggy dans le menu principal", menu),
			new Point("ShopPlayerSpawn", "Joueur dans la boutique", shop),
			new Point("ShopPlayerCamera", "Caméra boutique joueur", shop),
			new Point("Player spawn", "Spawn de tous les joueurs survivants", game),
			new Point("Player Monster spawn", "Spawn du joueur monstre", new Color(1f, 0.25f, 0.25f)),
			new Point("GeneratorSpawn1", "Générateur 1", game),
			new Point("GeneratorSpawn2", "Générateur 2", game),
			new Point("GeneratorSpawn3", "Générateur 3", game),
			new Point("GeneratorSpawn4", "Générateur 4", game),
			new Point("ExitSpawn", "Sortie", game)
		};

		GameObject root = GameObject.Find("POPPY_SCENE_POINTS");
		if (root == null)
		{
			root = new GameObject("POPPY_SCENE_POINTS");
			Undo.RegisterCreatedObjectUndo(root, "Créer les points de scène");
		}
		Vector3 basePos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
		int created = 0;
		for (int i = 0; i < points.Length; i++)
		{
			if (root.transform.Find(points[i].name) != null)
			{
				continue;
			}
			GameObject g = new GameObject(points[i].name);
			g.transform.SetParent(root.transform, false);
			g.transform.position = basePos + new Vector3((i % 4) * 2f, 0f, (i / 4) * 2f);
			ScenePointGizmo gizmo = g.AddComponent<ScenePointGizmo>();
			gizmo.label = points[i].label;
			gizmo.color = points[i].color;
			Undo.RegisterCreatedObjectUndo(g, "Créer les points de scène");
			created++;
		}
		Selection.activeGameObject = root;
		EditorSceneManager.MarkSceneDirty(root.scene);
		Debug.Log("Points de scène : " + created + " créé(s). Place notamment « Player spawn » et « Player Monster spawn » dans la zone de jeu, puis oriente-les (flèche bleue = avant).");
	}
}
#endif
