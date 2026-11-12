using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnCritters : MonoBehaviour
{
    public GameObject[] enemyPrefabs;

    public Transform spawnPoint;
    public int minSpawn = 3;
    public int maxSpawn = 8;
    public float spawnDelay = 0.5f;

    private List<GameObject> aliveEnemies = new List<GameObject>();
    private bool isActive = false;

    void OnTriggerEnter(Collider other)
    {
        if (isActive) return;

        if (other.CompareTag("Player"))
        {
            StartCoroutine(SpawnRoutine());
        }
    }

    IEnumerator SpawnRoutine()
    {
        isActive = true;

        int spawnCount = Random.Range(minSpawn, maxSpawn + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnDelay);
        }

        yield return new WaitUntil(() => aliveEnemies.Count == 0);

        isActive = false;
    }

    void SpawnEnemy()
    {
        if (enemyPrefabs.Length == 0) return;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        GameObject enemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        aliveEnemies.Add(enemy);

        StartCoroutine(TrackEnemy(enemy));
    }

    IEnumerator TrackEnemy(GameObject enemy)
    {
        yield return new WaitUntil(() => enemy == null);

        aliveEnemies.Remove(enemy);
    }
}