using UnityEngine;

public class TerrainAssetChange : MonoBehaviour
{
    [SerializeField] private GameObject grassPrefab;
    [SerializeField] private Terrain terrain;

    [ContextMenu("Flush")]
    private void Flush()
    {
        Debug.Log("=== Flush Start ===");

        if (grassPrefab == null)
        {
            Debug.LogError("grassPrefab is NULL");
            return;
        }

        if (terrain == null)
        {
            Debug.LogError("terrain is NULL");
            return;
        }

        var terrainData = terrain.terrainData;

        if (terrainData == null)
        {
            Debug.LogError("terrainData is NULL");
            return;
        }

        var prototypes = terrainData.detailPrototypes;

        Debug.Log($"Prototype Count: {prototypes.Length}");

        if (prototypes.Length == 0)
        {
            Debug.LogError("No Detail Prototype found.");
            return;
        }

        var prototype = prototypes[0];

        Debug.Log($"Before:");
        Debug.Log($"- prototype prefab : {(prototype.prototype ? prototype.prototype.name : "NULL")}");
        Debug.Log($"- usePrototypeMesh: {prototype.usePrototypeMesh}");
        Debug.Log($"- renderMode      : {prototype.renderMode}");

        prototype.prototype = grassPrefab;
        prototypes[0] = prototype;

        terrainData.detailPrototypes = prototypes;

        Debug.Log("Assigned new prototype.");

        var check = terrainData.detailPrototypes[0];

        Debug.Log($"After:");
        Debug.Log($"- prototype prefab : {(check.prototype ? check.prototype.name : "NULL")}");
        Debug.Log($"- usePrototypeMesh: {check.usePrototypeMesh}");
        Debug.Log($"- renderMode      : {check.renderMode}");

        terrain.Flush();

        Debug.Log("=== Flush End ===");
    }
}
