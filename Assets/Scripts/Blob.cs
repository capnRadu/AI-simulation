using UnityEngine;

public class Blob : MonoBehaviour
{
    private Rigidbody2D rb;
    private BehaviourTree tree;
    private PhysicsCommandBuffer physicsBuffer;

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

    private Transform entityTarget;
    private Vector3 wanderTarget;
    private Vector3 currentTargetPos;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        physicsBuffer = GetComponent<PhysicsCommandBuffer>();
        speed = baseSpeed;

        tree = new BehaviourTree("Blob Behaviour");

        Selector rootSelector = new Selector("RootSelector");

        // Chase prey sequence
        Sequence chasePreySequence = new Sequence("ChasePreySequence");

        chasePreySequence.AddChild(new Leaf("PreyNearby?", new Condition(() =>
        {
            entityTarget = FindClosestTarget(preyMask);
            return entityTarget != null && entityTarget.GetComponent<Blob>().Mass < mass;
        })));

        chasePreySequence.AddChild(new Leaf("SetPreyTarget", new ActionStrategy(() =>
        {
            currentTargetPos = entityTarget.position;
            Debug.Log("Chasing prey at " + currentTargetPos);
        })));

        chasePreySequence.AddChild(new Leaf("MoveToPrey", new MoveToTargetStrategy(rb, physicsBuffer, transform, () => currentTargetPos, speed, 0.2f)));

        chasePreySequence.AddChild(new Leaf("ResetTarget", new ActionStrategy(() =>
        {
            entityTarget = null;
        })));


        // Chase food sequence
        var chaseFoodSequence = new Sequence("ChaseFoodSequence");

        chaseFoodSequence.AddChild(new Leaf("FoodNearby?", new Condition(() =>
        {
            entityTarget = FindClosestTarget(foodMask);
            return entityTarget != null;
        })));

        chaseFoodSequence.AddChild(new Leaf("SetFoodTarget", new ActionStrategy(() =>
        {
            currentTargetPos = entityTarget.position;
            Debug.Log("Chasing food at " + currentTargetPos);
        })));

        chaseFoodSequence.AddChild(new Leaf("MoveToFood", new MoveToTargetStrategy(rb, physicsBuffer, transform, () => currentTargetPos, speed, 0.2f)));

        chaseFoodSequence.AddChild(new Leaf("ResetTarget", new ActionStrategy(() =>
        {
            entityTarget = null;
        })));

        // Wander sequence
        var wanderSequence = new Sequence("WanderSequence");

        wanderSequence.AddChild(new Leaf("NeedWanderTarget?", new Condition(() =>
        {
            return entityTarget == null || Vector3.Distance(transform.position, wanderTarget) <= 0.2f;
        })));

        wanderSequence.AddChild(new Leaf("PickWanderTarget", new ActionStrategy(() =>
        {
            wanderTarget = RandomPointInBounds(arenaCol.bounds);
            currentTargetPos = wanderTarget;
            Debug.Log("New wander target: " + wanderTarget);
        })));

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
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Prey") && collision.gameObject.GetComponent<Blob>().Mass < mass)
        {
            ConsumeFood(5f);
            Destroy(collision.gameObject);
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