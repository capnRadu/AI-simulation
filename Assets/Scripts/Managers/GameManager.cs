using System.Collections;
using UnityEngine;

/// <summary>
/// Manages the game state, including spawning initial food and blobs,
/// and continuously spawning food over time. Also holds the arena bounds.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private GameObject blobPrefab;

    [Space]
    [Tooltip("How often (in seconds) to spawn a new piece of food")]
    [SerializeField] private float foodSpawnInterval = 0.25f;
    [Tooltip("How much food to spawn at the start of the game")]
    [SerializeField] private int initialFoodCount = 100;
    [Tooltip("How many AI blobs to spawn at the start")]
    [SerializeField] private int blobsCount = 2;

    private BoxCollider2D col; // The arena bounds

    private void Start()
    {
        col = GetComponent<BoxCollider2D>();

        // Spawn initial objects
        SpawnMultipleGameObjects(foodPrefab, initialFoodCount);
        SpawnMultipleGameObjects(blobPrefab, blobsCount);

        // Start the food spawning coroutine
        StartCoroutine(SpawnFoodRoutine());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            InstantiateGameObject(blobPrefab);
        }
    }

    /// <summary>
    /// Spawns a given prefab 'count' times at random locations.
    /// </summary>
    private void SpawnMultipleGameObjects(GameObject prefab, int count)
    {
        for (int i = 0; i < count; i++)
        {
            InstantiateGameObject(prefab);
        }
    }

    /// <summary>
    /// Spawns one piece of food every 'foodSpawnInterval' seconds.
    /// </summary>
    private IEnumerator SpawnFoodRoutine()
    {
        while (true)
        {
            InstantiateGameObject(foodPrefab);
            yield return new WaitForSeconds(foodSpawnInterval);
        }
    }

    /// <summary>
    /// Instantiates a prefab at a random position within the arena bounds.
    /// </summary>
    private void InstantiateGameObject(GameObject prefab)
    {
        Vector3 spawnPos = new Vector3(Random.Range(col.bounds.min.x, col.bounds.max.x), Random.Range(col.bounds.min.y, col.bounds.max.y), 0f);
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }
}