using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private BoxCollider2D col;

    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private GameObject blobPrefab;

    private float foodSpawnInterval = 0.5f;
    private int initialFoodCount = 60;
    private int blobsCount = 3;

    private void Start()
    {
        col = GetComponent<BoxCollider2D>();

        SpawnMultipleGameObjects(foodPrefab, initialFoodCount);
        SpawnMultipleGameObjects(blobPrefab, blobsCount);
        StartCoroutine(SpawnFoodRoutine());
    }

    private void SpawnMultipleGameObjects(GameObject prefab, int count)
    {
        for (int i = 0; i < count; i++)
        {
            InstantiateGameObject(prefab);
        }
    }

    private IEnumerator SpawnFoodRoutine()
    {
        while (true)
        {
            InstantiateGameObject(foodPrefab);
            yield return new WaitForSeconds(foodSpawnInterval);
        }
    }

    private void InstantiateGameObject(GameObject prefab)
    {
        Vector3 spawnPos = new Vector3(Random.Range(col.bounds.min.x, col.bounds.max.x), Random.Range(col.bounds.min.y, col.bounds.max.y), 0f);
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }
}