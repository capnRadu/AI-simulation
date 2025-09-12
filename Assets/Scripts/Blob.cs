using UnityEngine;

public class Blob : MonoBehaviour
{
    private Rigidbody2D rb;

    [SerializeField] private float mass = 1f;
    public float Mass => mass;
    private float speed;
    private float baseSpeed = 20f;
    private float speedFactor = 0.001f; // how much speed decreases per unit mass
    private float scaleFactor = 0.1f; // how much scale increases per unit mass

    [SerializeField] private BoxCollider2D arenaCol;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private LayerMask foodMask;
    [SerializeField] private LayerMask preyMask;

    private Transform currentTarget;
    private Vector3 wanderTarget;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        arenaCol = FindFirstObjectByType<GameManager>().GetComponent<BoxCollider2D>();
        speed = baseSpeed;
    }

    private void Update()
    {
        // Decide target based on chase priority
        ChaseBehavior();

        // Wander if no target
        if (currentTarget == null && Vector3.Distance(transform.position, wanderTarget) <= 0.2f)
        {
            wanderTarget = RandomPointInBounds(arenaCol.bounds);
        }
    }

    private void FixedUpdate()
    {
        Vector3 targetPos = currentTarget != null ? currentTarget.position : wanderTarget;
        MoveTowards(targetPos);
    }

    // Chase food or prey
    private void ChaseBehavior()
    {
        // If current target is gone, find a new one
        if (currentTarget == null)
        {
            Transform foodTarget = FindClosestTarget(foodMask);
            Transform preyTarget = FindClosestTarget(preyMask);

            // Example priority: always go for prey first if exists
            if (preyTarget != null)
            {
                currentTarget = preyTarget;
            }
            else if (foodTarget != null)
            {
                currentTarget = foodTarget;
            }
        }
    }

    private Transform FindClosestTarget(LayerMask mask)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, mask);

        if (hits.Length == 0) return null;

        Transform closest = null;
        float minDist = Mathf.Infinity;

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue; // ignore self

            float dist = Vector3.Distance(transform.position, hit.transform.position);

            if (dist < minDist)
            {
                closest = hit.transform;
                minDist = dist;
            }
        }

        return closest;
    }

    private void MoveTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        rb.MovePosition(transform.position + speed * Time.fixedDeltaTime * direction);
    }

    private Vector3 RandomPointInBounds(Bounds bounds)
    {
        return new Vector3(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y), 0f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Food"))
        {
            ConsumeFood(1f);
            Destroy(collision.gameObject);
            currentTarget = null;
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Prey") && collision.gameObject.GetComponent<Blob>().Mass < mass)
        {
            ConsumeFood(5f);
            Destroy(collision.gameObject);
            currentTarget = null;
        }
    }

    private void ConsumeFood(float foodValue)
    {
        mass += foodValue;

        float newScale = 1f + mass * scaleFactor;
        transform.localScale = new Vector3(newScale, newScale, 1f);

        speed = baseSpeed / (1f + mass * speedFactor);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
} 