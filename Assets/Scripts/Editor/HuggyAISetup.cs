#if UNITY_EDITOR
using System;
using System.Reflection;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Menu Poppy > Configurer Huggy IA : crée le prefab réseau du monstre IA, l'enregistre, l'assigne au GameManager
// et prépare le NavMesh. À lancer une fois, avec la scène Lobby ouverte (puis enregistre la scène).
public static class HuggyAISetup
{
	private const string PrefabPath = "Assets/Prefabs/Huggy IA.prefab";
	private const string NetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";

	[MenuItem("Poppy/Configurer Huggy IA (prefab, GameManager, NavMesh)")]
	public static void Setup()
	{
		NetworkManager nm = UnityEngine.Object.FindObjectOfType<NetworkManager>();
		GameManager gm = UnityEngine.Object.FindObjectOfType<GameManager>();
		if (nm == null || gm == null)
		{
			EditorUtility.DisplayDialog("Huggy IA", "Ouvre d'abord la scène Lobby : il faut un NetworkManager et un GameManager dans la scène.", "OK");
			return;
		}
		GameObject playerPrefab = nm.NetworkConfig != null ? nm.NetworkConfig.PlayerPrefab : null;
		DbUnityMovment mv = playerPrefab != null ? playerPrefab.GetComponent<DbUnityMovment>() : null;
		if (mv == null || mv.monsterModelContainer == null)
		{
			EditorUtility.DisplayDialog("Huggy IA", "Le Player Prefab du NetworkManager n'a pas de 'monsterModelContainer' : impossible de copier le modèle de Huggy.", "OK");
			return;
		}

		GameObject prefab = BuildPrefab(mv.monsterModelContainer);
		if (prefab == null)
		{
			return;
		}
		RegisterNetworkPrefab(prefab);

		SerializedObject so = new SerializedObject(gm);
		SerializedProperty aiProp = so.FindProperty("aiMonsterPrefab");
		if (aiProp != null)
		{
			aiProp.objectReferenceValue = prefab.GetComponent<NetworkObject>();
		}
		SerializedProperty spawnProp = so.FindProperty("monsterSpawnPoint");
		if (spawnProp != null && spawnProp.objectReferenceValue == null)
		{
			GameObject spawn = GameObject.Find("killer spawn");
			if (spawn == null)
			{
				spawn = GameObject.Find("MonsterSpawn");
			}
			if (spawn != null)
			{
				spawnProp.objectReferenceValue = spawn.transform;
			}
		}
		so.ApplyModifiedProperties();
		EditorUtility.SetDirty(gm);

		string navResult = BakeNavMesh();
		EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
		AssetDatabase.SaveAssets();

		bool spawnOk = spawnProp != null && spawnProp.objectReferenceValue != null;
		EditorUtility.DisplayDialog("Huggy IA",
			"Prefab créé : " + PrefabPath + "\n" +
			"Assigné au GameManager : oui\n" +
			"Point de spawn du monstre : " + (spawnOk ? "OK" : "À ASSIGNER (champ Monster Spawn Point du GameManager)") + "\n" +
			"NavMesh : " + navResult + "\n\nEnregistre la scène (Ctrl+S).", "OK");
	}

	private static GameObject BuildPrefab(GameObject monsterModelSource)
	{
		GameObject root = new GameObject("Huggy IA");
		try
		{
			GameObject model = UnityEngine.Object.Instantiate(monsterModelSource);
			if (model.GetComponent<NetworkObject>() != null)
			{
				// Unity a cloné le prefab joueur entier : on ne garde que le conteneur du monstre.
				Transform wanted = model.transform.Find(monsterModelSource.name);
				if (wanted != null)
				{
					wanted.SetParent(null, false);
					UnityEngine.Object.DestroyImmediate(model);
					model = wanted.gameObject;
				}
			}
			model.name = "Model";
			model.transform.SetParent(root.transform, false);
			model.transform.localPosition = Vector3.zero;
			model.transform.localRotation = Quaternion.identity;
			model.SetActive(true);

			// le modèle ne doit contenir aucun script réseau / collision du joueur
			foreach (NetworkBehaviour nb in model.GetComponentsInChildren<NetworkBehaviour>(true))
			{
				UnityEngine.Object.DestroyImmediate(nb);
			}
			foreach (NetworkObject no in model.GetComponentsInChildren<NetworkObject>(true))
			{
				UnityEngine.Object.DestroyImmediate(no);
			}
			foreach (Collider c in model.GetComponentsInChildren<Collider>(true))
			{
				UnityEngine.Object.DestroyImmediate(c);
			}
			foreach (Rigidbody rb in model.GetComponentsInChildren<Rigidbody>(true))
			{
				UnityEngine.Object.DestroyImmediate(rb);
			}
			foreach (Animator an in model.GetComponentsInChildren<Animator>(true))
			{
				an.applyRootMotion = false;
				an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
			}

			root.AddComponent<NetworkObject>();
			NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
			agent.radius = 0.55f;
			agent.height = 2.8f;
			agent.speed = 3.5f;
			agent.acceleration = 14f;
			agent.angularSpeed = 360f;
			agent.stoppingDistance = 0.2f;
			agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
			root.AddComponent<NetworkTransform>(); // autorité serveur par défaut
			CapsuleCollider cap = root.AddComponent<CapsuleCollider>();
			cap.center = new Vector3(0f, 1.4f, 0f);
			cap.radius = 0.5f;
			cap.height = 2.8f;
			AudioSource audio = root.AddComponent<AudioSource>();
			audio.spatialBlend = 1f;
			audio.playOnAwake = false;
			HuggyHunterAI ai = root.AddComponent<HuggyHunterAI>();
			ai.animator = model.GetComponentInChildren<Animator>();
			ai.audioSource = audio;

			GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			return saved;
		}
		catch (Exception ex)
		{
			Debug.LogError("Huggy IA : échec de la création du prefab : " + ex.Message);
			return null;
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	private static void RegisterNetworkPrefab(GameObject prefab)
	{
		NetworkPrefabsList list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NetworkPrefabsPath);
		if (list == null)
		{
			Debug.LogWarning("Huggy IA : " + NetworkPrefabsPath + " introuvable. Ajoute 'Huggy IA' à ta liste de Network Prefabs.");
			return;
		}
		if (!list.Contains(prefab))
		{
			list.Add(new NetworkPrefab { Prefab = prefab });
			EditorUtility.SetDirty(list);
		}
	}

	// Les portes ne doivent pas être cuites dans le NavMesh : c'est leur NavMeshObstacle (actif quand la porte
	// est fermée) qui bloque Huggy, ce qui lui permet de passer dès qu'un survivant a descendu la porte.
	private static void ExcludeDoorsFromBake()
	{
		Type modType = Type.GetType("Unity.AI.Navigation.NavMeshModifier, Unity.AI.Navigation");
		if (modType == null)
		{
			return;
		}
		foreach (GrabpackDraggableDoor door in UnityEngine.Object.FindObjectsOfType<GrabpackDraggableDoor>())
		{
			Component mod = door.GetComponent(modType);
			if (mod == null)
			{
				mod = door.gameObject.AddComponent(modType);
			}
			PropertyInfo ignore = modType.GetProperty("ignoreFromBuild");
			if (ignore != null)
			{
				ignore.SetValue(mod, true, null);
			}
			PropertyInfo children = modType.GetProperty("applyToChildren");
			if (children != null)
			{
				children.SetValue(mod, true, null);
			}
			EditorUtility.SetDirty(mod);
		}
	}

	[MenuItem("Poppy/Reconstruire le NavMesh de Huggy IA (portes exclues)")]
	public static void RebuildNavMeshOnly()
	{
		EditorUtility.DisplayDialog("NavMesh Huggy IA", "NavMesh : " + BakeNavMesh() + "\n\nEnregistre la scène (Ctrl+S).", "OK");
	}

	// NavMeshSurface (package AI Navigation) appelé par réflexion pour ne pas dépendre de sa version.
	private static string BakeNavMesh()
	{
		try
		{
			Type surfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
			if (surfaceType == null)
			{
				return "package AI Navigation absent : crée un NavMeshSurface à la main et clique Bake.";
			}
			GameObject go = GameObject.Find("NavMesh Surface (Huggy IA)");
			Component surface = go != null ? go.GetComponent(surfaceType) : null;
			if (surface == null)
			{
				go = new GameObject("NavMesh Surface (Huggy IA)");
				Undo.RegisterCreatedObjectUndo(go, "NavMesh Huggy IA");
				surface = go.AddComponent(surfaceType);
			}
			ExcludeDoorsFromBake();
			Type collectType = surfaceType.Assembly.GetType("Unity.AI.Navigation.CollectObjects");
			PropertyInfo collect = surfaceType.GetProperty("collectObjects");
			if (collect != null && collectType != null)
			{
				collect.SetValue(surface, Enum.Parse(collectType, "All"), null);
			}
			PropertyInfo geom = surfaceType.GetProperty("useGeometry");
			if (geom != null)
			{
				geom.SetValue(surface, NavMeshCollectGeometry.PhysicsColliders, null);
			}
			surfaceType.GetMethod("BuildNavMesh").Invoke(surface, null);

			PropertyInfo dataProp = surfaceType.GetProperty("navMeshData");
			NavMeshData data = dataProp != null ? dataProp.GetValue(surface, null) as NavMeshData : null;
			if (data != null && !AssetDatabase.Contains(data))
			{
				AssetDatabase.CreateAsset(data, "Assets/Scenes/NavMesh-Huggy-IA.asset");
				dataProp.SetValue(surface, data, null);
			}
			EditorUtility.SetDirty(surface);
			return data != null ? "construit (Assets/Scenes/NavMesh-Huggy-IA.asset)" : "construit, mais non sauvegardé : clique 'Bake' sur le NavMeshSurface";
		}
		catch (Exception ex)
		{
			return "échec automatique (" + ex.Message + ") : sélectionne 'NavMesh Surface (Huggy IA)' puis clique Bake.";
		}
	}
}
#endif
