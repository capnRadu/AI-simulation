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

    [Header("Time Control")]
    [Tooltip("The amount to increase/decrease time scale with each key press")]
    [SerializeField] private float timeScaleIncrement = 0.25f;
    [SerializeField] private float minTimeScale = 0.25f;
    [SerializeField] private float maxTimeScale = 4f;
    [SerializeField] private KeyCode speedUpKey = KeyCode.Equals;
    [SerializeField] private KeyCode slowDownKey = KeyCode.Minus;
    [SerializeField] private KeyCode resetSpeedKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode pauseKey = KeyCode.P;

    private bool isPaused = false;
    private float lastTimeScale = 1f;

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

        HandleTimeControl();
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

    /// <summary>
    /// Checks for user input to modify the simulation's Time.timeScale.
    /// </summary>
    private void HandleTimeControl()
    {
        if (Input.GetKeyDown(speedUpKey))
        {
            if (isPaused) isPaused = false;

            float newTimeScale = Time.timeScale + timeScaleIncrement;
            Time.timeScale = Mathf.Clamp(newTimeScale, minTimeScale, maxTimeScale);
            Debug.Log("Time Scale set to: " + Time.timeScale);
        }
        else if (Input.GetKeyDown(slowDownKey))
        {
            if (isPaused) isPaused = false;

            float newTimeScale = Time.timeScale - timeScaleIncrement;
            Time.timeScale = Mathf.Clamp(newTimeScale, minTimeScale, maxTimeScale);
            Debug.Log("Time Scale set to: " + Time.timeScale);
        }
        else if (Input.GetKeyDown(resetSpeedKey))
        {
            isPaused = false;
            Time.timeScale = 1f;
            Debug.Log("Time Scale reset to 1.0");
        }
        else if (Input.GetKeyDown(pauseKey))
        {
            isPaused = !isPaused;

            if (isPaused)
            {
                lastTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                Debug.Log("Simulation Paused");
            }
            else
            {
                Time.timeScale = lastTimeScale;
                Debug.Log("Simulation Resumed. Time Scale: " + Time.timeScale);
            }
        }
    }
}