using UnityEngine;

/// <summary>
/// The base class for all 'blob' entities (Player and AI).
/// Manages core properties like mass, speed, scale, and handles consumption.
/// </summary>
public class Blob : MonoBehaviour
{
    // --- Core Components ---
    protected Rigidbody2D rb;
    protected Wobble wobble;
    protected BoxCollider2D arenaCol;

    // --- Serialized Fields (Base Stats) ---
    [Header("Base Stats")]
    [SerializeField] protected float mass = 1f;
    [SerializeField] protected float baseSpeed = 15f;
    [Tooltip("How much speed decreases per unit mass")]
    [SerializeField] protected float speedFactor = 0.003f;
    [Tooltip("How much scale increases per unit mass")]
    [SerializeField] protected float scaleFactor = 0.1f;

    [Header("Object References")]
    [SerializeField] protected LayerMask foodMask;
    [SerializeField] protected LayerMask preyMask;
    [SerializeField] protected GameObject massPrefab;

    // --- Public Properties ---
    public float Mass
    {
        get { return mass; }
        set { mass = value; }
    }
    protected float speed; // The *current* speed, affected by mass
    public float Speed
    {
        get { return speed; }
        set { speed = value; }
    }
    public float BaseSpeed => baseSpeed;
    public float SpeedFactor => speedFactor;
    public float ScaleFactor => scaleFactor;
    public Bounds ArenaColBounds => arenaCol.bounds;
    protected float foodPrefabMass; // The mass of one ejected food piece

    private void Awake()
    {
        SetupBlob();
    }

    private void Start()
    {
        arenaCol = FindFirstObjectByType<GameManager>().GetComponent<BoxCollider2D>();
    }

    protected virtual void SetupBlob()
    {
        rb = GetComponent<Rigidbody2D>();
        wobble = GetComponent<Wobble>();
        speed = baseSpeed;
        foodPrefabMass = massPrefab.GetComponent<MassForce>().Mass;
    }

    public void EjectFood()
    {
        GameObject ejectedMass = Instantiate(massPrefab, transform.position, Quaternion.identity);
        MassForce massForce = ejectedMass.GetComponent<MassForce>();
        massForce.SetupEjectedMass(true, GetInstatiateDirection(), ArenaColBounds);
        mass -= massForce.Mass;
    }

    /// <summary>
    /// Gets the direction to eject food.
    /// Overridden by AI (target).
    /// </summary>
    protected virtual Vector3 GetInstatiateDirection()
    {
        // Player default: eject towards mouse
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject other = collision.gameObject;
        int otherLayer = other.layer;

        if (otherLayer == LayerMask.NameToLayer("Food"))
        {
            HandleConsumption(other, other.GetComponent<MassForce>().Mass);
        }
        else if (otherLayer == LayerMask.NameToLayer("Prey"))
        {
            Blob prey = other.GetComponent<Blob>();

            if (prey != null && prey.Mass < mass)
            {
                HandleConsumption(other, prey.Mass);
            }
        }
    }

    protected virtual void HandleConsumption(GameObject target, float foodValue)
    {
        ConsumeFood(foodValue);
        Destroy(target);
    }

    private void ConsumeFood(float foodValue)
    {
        mass += foodValue;

        float newScale = 1f + mass * scaleFactor;
        transform.localScale = new Vector3(newScale, newScale, 1f);
        wobble.UpdateScale(transform); // Tell wobble component the new base scale

        // Update speed (getting bigger makes us slower)
        speed = baseSpeed / (1f + mass * speedFactor);
    }
} 