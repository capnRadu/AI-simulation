using UnityEngine;

public class Blob : MonoBehaviour
{
    protected Rigidbody2D rb;
    protected Wobble wobble;

    [SerializeField] protected float mass = 1f;
    public float Mass
    {
        get { return mass; }
        set { mass = value; }
    }

    protected float speed;
    public float Speed
    {
        get { return speed; }
        set { speed = value; }
    }
    protected float baseSpeed = 15f;
    protected float speedFactor = 0.003f; // how much speed decreases per unit mass
    protected float scaleFactor = 0.1f; // how much scale increases per unit mass

    protected BoxCollider2D arenaCol;
    public Bounds ArenaColBounds => arenaCol.bounds;

    [SerializeField] protected LayerMask foodMask;
    [SerializeField] protected LayerMask preyMask;
    [SerializeField] protected GameObject massPrefab;

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
    }

    public void EjectFood()
    {
        GameObject ejectedMass = Instantiate(massPrefab, transform.position, Quaternion.identity);
        MassForce massForce = ejectedMass.GetComponent<MassForce>();
        massForce.SetupEjectedMass(true, GetInstatiateDirection());
        mass -= massForce.Mass;
    }

    protected virtual Vector3 GetInstatiateDirection()
    {
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
        wobble.UpdateScale(transform);

        speed = baseSpeed / (1f + mass * speedFactor);
    }
} 