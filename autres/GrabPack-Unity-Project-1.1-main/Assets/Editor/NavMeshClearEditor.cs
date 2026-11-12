using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class NavMeshTools
{
    [MenuItem("Tools/NavMesh/Clear All NavMesh")]
    static void ClearAllNavMeshes()
    {
        NavMesh.RemoveAllNavMeshData();
        Debug.Log("All NavMesh data removed.");
    }
}