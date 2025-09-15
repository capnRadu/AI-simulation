using UnityEngine;

public class Blob : MonoBehaviour
{
    private Rigidbody2D rb;
    private BehaviourTree tree;
    private PhysicsCommandBuffer physicsBuffer;

    [SerializeField] private float mass = 1f;
    public float Mass => mass;

    private float speed;
    private float baseSpeed = 15f;
    private float speedFactor = 0.003f; // how much speed decreases per unit mass
    private float scaleFactor = 0.1f; // how much scale increases per unit mass

    [SerializeField] private BoxCollider2D arenaCol;
    public Bounds ArenaColBounds => arenaCol.bounds;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private LayerMask foodMask;
    [SerializeField] private LayerMask preyMask;

    private Transform chaseTarget;
    private Vector3 wanderTarget;
    private Vector3 currentTargetPos;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        physicsBuffer = GetComponent<PhysicsCommandBuffer>();
        speed = baseSpeed;

        tree = new BehaviourTree("Blob Behaviour");

        PrioritySelector rootSelector = new PrioritySelector("RootSelector");

        // Chase food sequence
        var chaseFoodSequence = new Sequence("ChaseFoodSequence", 2);

        chaseFoodSequence.AddChild(new Leaf("FindFood", new FindAndSetChaseTargetStrategy(this, foodMask, false)));
        chaseFoodSequence.AddChild(new Leaf("MoveToFood", new MoveToTargetStrategy(rb, physicsBuffer, transform, () => currentTargetPos, speed, 0.2f)));
        chaseFoodSequence.AddChild(new Leaf("ResetTarget", new ActionStrategy(ClearChaseTarget)));

        // Chase prey sequence
        Sequence chasePreySequence = new Sequence("ChasePreySequence", 2);

        chasePreySequence.AddChild(new Leaf("FindPrey", new FindAndSetChaseTargetStrategy(this, preyMask, true)));
        chasePreySequence.AddChild(new Leaf("MoveToPrey", new MoveToTargetStrategy(rb, physicsBuffer, transform, () => currentTargetPos, speed, 0.2f)));
        chasePreySequence.AddChild(new Leaf("ResetTarget", new ActionStrategy(ClearChaseTarget)));

        // Wander sequence
        var wanderSequence = new Sequence("WanderSequence", 1);

        wanderSequence.AddChild(new Leaf("FindWanderTarget", new FindAndSetWanderTargetStrategy(this, chaseTarget, () => currentTargetPos)));
        wanderSequence.AddChild(new Leaf("MoveToWanderTarget", new MoveToTargetStrategy(rb, physicsBuffer, transform, () => currentTargetPos, speed, 0.2f)));

        rootSelector.AddChild(chasePreySequence);
        rootSelector.AddChild(chaseFoodSequence);
        rootSelector.AddChild(wanderSequence);

        tree.AddChild(rootSelector);
    }

    private void Start()
    {
        arenaCol = FindFirstObjectByType<GameManager>().GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        tree.Process();
    }

    public Transform FindClosestChaseTarget(LayerMask mask)
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

    public void SetChaseTarget(Transform target)
    {
        chaseTarget = target;

        if (chaseTarget != null)
        {
            currentTargetPos = chaseTarget.position;
        }
    }

    public void ClearChaseTarget()
    {
        chaseTarget = null;
    }

    public Vector3 FindRandomPointInBounds(Bounds bounds)
    {
        return new Vector3(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y), 0f);
    }

    public void SetWanderTarget(Vector3 target)
    {
        wanderTarget = target;
        currentTargetPos = wanderTarget;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject other = collision.gameObject;
        int otherLayer = other.layer;

        if (otherLayer == LayerMask.NameToLayer("Food"))
        {
            HandleConsumption(other, 1f);
        }
        else if (otherLayer == LayerMask.NameToLayer("Prey"))
        {
            Blob prey = other.GetComponent<Blob>();

            if (prey != null && prey.Mass < mass)
            {
                HandleConsumption(other, 5f);
            }
        }
    }

    private void HandleConsumption(GameObject target, float foodValue)
    {
        ConsumeFood(foodValue);
        Destroy(target);

        ClearChaseTarget();
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
        DrawWireCircle(transform.position, detectionRadius, 64);
    }

    private void DrawWireCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(Mathf.Cos(0f), Mathf.Sin(0f), 0f) * radius;

        for (int i = 1; i <= segments; i++)
        {
            float rad = Mathf.Deg2Rad * angleStep * i;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
} 