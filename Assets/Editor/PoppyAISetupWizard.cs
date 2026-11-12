#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using Unity.Netcode.Components;

/// <summary>
/// Select the Huggy model root in the Lobby scene, then run:
/// Tools > Poppy Playtime > Create AI Network Prefab From Selected Huggy.
/// </summary>
public static class PoppyAISetupWizard
{
    [MenuItem("Tools/Poppy Playtime/Create AI Network Prefab From Selected Huggy")]
    private static void CreateEnemyPrefab()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Poppy AI Setup", "Select the root Huggy model in the Hierarchy first.", "OK");
            return;
        }

        const string folder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        GameObject temporary = Object.Instantiate(selected);
        temporary.name = "Huggy AI Network";
        temporary.transform.position = Vector3.zero;
        temporary.transform.rotation = Quaternion.identity;

        if (temporary.GetComponent<NetworkObject>() == null)
            temporary.AddComponent<NetworkObject>();

        if (temporary.GetComponent<NetworkTransform>() == null)
            temporary.AddComponent<NetworkTransform>();

        NavMeshAgent agent = temporary.GetComponent<NavMeshAgent>();
        if (agent == null)
            agent = temporary.AddComponent<NavMeshAgent>();

        agent.speed = 2.2f;
        agent.angularSpeed = 420f;
        agent.acceleration = 14f;
        agent.stoppingDistance = 1.25f;
        agent.radius = 0.45f;
        agent.height = 3.2f;
        agent.autoBraking = true;

        if (temporary.GetComponent<HuggyHunterAI>() == null)
            temporary.AddComponent<HuggyHunterAI>();

        if (temporary.GetComponent<Collider>() == null)
        {
            CapsuleCollider capsule = temporary.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 1.6f, 0f);
            capsule.height = 3.2f;
            capsule.radius = 0.45f;
            capsule.isTrigger = true;
        }

        string path = folder + "/Huggy AI Network.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temporary, path);
        Object.DestroyImmediate(temporary);

        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Poppy AI Setup", "Unity could not create the prefab.", "OK");
            return;
        }

        GameManager manager = Object.FindObjectOfType<GameManager>();
        if (manager != null)
        {
            SerializedObject serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("aiMonsterPrefab").objectReferenceValue =
                prefab.GetComponent<NetworkObject>();
            serializedManager.ApplyModifiedProperties();
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        EditorUtility.DisplayDialog(
            "AI prefab created",
            "Created Assets/Prefabs/Huggy AI Network.prefab.\n\nNext: add this prefab to NetworkManager > NetworkConfig > Network Prefabs, bake a NavMesh in the gameplay map, and assign a valid monster spawn point.",
            "OK");
    }

    [MenuItem("Tools/Poppy Playtime/Create AI Network Prefab From Selected Huggy", true)]
    private static bool ValidateCreateEnemyPrefab()
    {
        return Selection.activeGameObject != null;
    }
}
#endif
