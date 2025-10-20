using UnityEngine;

/// <summary>
/// This script is attached to the 'food' prefab.
/// It manages the food's mass and its behavior when ejected from a blob.
/// </summary>
public class MassForce : MonoBehaviour
{
    private Collider2D col;
    private Bounds arenaBounds;

    [Header("Food Properties")]
    [SerializeField] private float mass = 0.7f;
    public float Mass => mass;

    [Header("Ejection Physics")]
    [Tooltip("Initial speed when ejected")]
    [SerializeField] private float speed = 60f;
    [Tooltip("How quickly the food piece loses speed (drag)")]
    [SerializeField] private float loseSpeed = 140f;
    [Tooltip("Random variance added to ejection angle")]
    [SerializeField] private float randomRotation = 10f;
    [Tooltip("Random variance added to ejection speed")]
    [SerializeField] private float randomForce = 5f;

    // --- State ---
    private bool applyForce = false; // Is this an ejected piece of food?
    private Vector3 currentTargetPos; // The direction *away* from which to fly

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    void Start()
    {
        // If 'applyForce' is false, this is just a static food piece
        // Disable this script for performance
        if (applyForce == false)
        {
            enabled = false;
            return;
        }

        // --- If we are here, we were ejected ---
        // Calculate ejection angle
        Vector2 direction = transform.position - currentTargetPos;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;

        // Add randomness
        angle += Random.Range(-randomRotation, randomRotation);
        transform.rotation = Quaternion.Euler(0, 0, angle);
        speed += Random.Range(-randomForce, randomForce);
    }

    void Update()
    {
        // Move forward (Vector2.up is relative to our rotation)
        transform.Translate(speed * Time.deltaTime * Vector2.up);

        // Apply drag
        speed -= loseSpeed * Time.deltaTime;

        // Stop updating when speed runs out
        if (speed <= 0)
        {
            enabled = false;
        }

        // Destroy if we fly out of bounds
        if (!arenaBounds.Contains(transform.position))
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// This is called by the Blob when it ejects food.
    /// It "activates" the script and sets its physics properties.
    /// </summary>
    public void SetupEjectedMass(bool _applyForce, Vector3 _currentTargetPos, Bounds _arenaColBounds)
    {
        applyForce = _applyForce;
        currentTargetPos = _currentTargetPos;
        arenaBounds = _arenaColBounds;

        // Disable collision temporarily so the blob doesn't immediately re-consume the food it just ejected.
        gameObject.layer = LayerMask.NameToLayer("Default");
        col.enabled = false;

        // Re-enable collision after a short delay
        Invoke(nameof(EnableMassInteraction), 0.2f);
    }

    private void EnableMassInteraction()
    {
        gameObject.layer = LayerMask.NameToLayer("Food");
        col.enabled = true;
    }
}