using UnityEngine;

public class Blob : MonoBehaviour
{
    private Rigidbody2D rb;
    private BehaviourTree tree;
    private PhysicsCommandBuffer physicsBuffer;
    private Wobble wobble;

    [SerializeField] private float mass = 1f;
    public float Mass
    {
        get { return mass; }
        set { mass = value; }
    }

    private float speed;
    public float Speed
    {
        get { return speed; }
        set { speed = value; }
    }
    private float baseSpeed = 15f;
    private float speedFactor = 0.003f; // how much speed decreases per unit mass
    private float scaleFactor = 0.1f; // how much scale increases per unit mass

    [SerializeField] private BoxCollider2D arenaCol;
    public Bounds ArenaColBounds => arenaCol.bounds;
    [SerializeField] private float detectionRadius = 10f;
    public float DetectionRadius => detectionRadius;
    [SerializeField] private LayerMask foodMask;
    [SerializeField] private LayerMask preyMask;
    [SerializeField] private GameObject massPrefab;

    private Transform chaseTarget;
    private Vector3 wanderTarget;
    private Vector3 fleeTarget;
    private Vector3 currentTargetPos;

    private float lastThrowCheckTime;
    private float throwCheckInterval = 2f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        physicsBuffer = GetComponent<PhysicsCommandBuffer>();
        wobble = GetComponent<Wobble>();
        speed = baseSpeed;

        tree = new BehaviourTree("Blob Behaviour");

        PrioritySelector rootSelector = new PrioritySelector("RootSelector");

        // Flee sequence
        var fleeSequence = new Sequence("FleeSequence", 3);

        fleeSequence.AddChild(new Leaf("FindThreat", new FindAndSetFleeTargetStrategy(this, preyMask)));
        fleeSequence.AddChild(new Leaf("Flee", new MoveToTargetStrategy(rb, physicsBuffer, transform, () => currentTargetPos, speed, 0.2f)));

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

        // Throw food sequence
        var throwFoodSequence = new Sequence("ThrowFoodSequence", 2);
        throwFoodSequence.AddChild(new Leaf("CheckMass", new Condition(() => mass > 1f)));
        throwFoodSequence.AddChild(new Leaf("CheckRandomness", new Condition(() => ShouldThrowFood(0.2f))));
        throwFoodSequence.AddChild(new Leaf("ThrowFood", new ThrowFoodStrategy(this, scaleFactor, speedFactor, baseSpeed, wobble)));

        // Wander sequence
        var wanderSequence = new Sequence("WanderSequence", 1);

        wanderSequence.AddChild(new Leaf("FindWanderTarget", new FindAndSetWanderTargetStrategy(this, chaseTarget, () => currentTargetPos)));
        wanderSequence.AddChild(new Leaf("MoveToWanderTarget", new MoveToTargetStrategy(rb, physicsBuffer, transform, () => currentTargetPos, speed, 0.2f)));

        rootSelector.AddChild(fleeSequence);
        rootSelector.AddChild(chaseFoodSequence);
        rootSelector.AddChild(chasePreySequence);
        rootSelector.AddChild(throwFoodSequence);
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

    public Transform FindBiggestThreat(LayerMask mask)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, mask);

        if (hits.Length == 0) return null;

        Transform biggestThreat = null;
        float closestDistance = Mathf.Infinity;

        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;

            Blob other = hit.GetComponent<Blob>();

            if (other != null && other.Mass > mass)
            {
                float dist = Vector3.Distance(transform.position, other.transform.position);

                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    biggestThreat = other.transform;
                }
            }
        }

        return biggestThreat;
    }

    public void SetFleeTarget(Vector3 target)
    {
        fleeTarget = target;
        currentTargetPos = fleeTarget;
    }

    private bool ShouldThrowFood(float probability)
    {
        if (Time.time - lastThrowCheckTime < throwCheckInterval) return false;
        
        lastThrowCheckTime = Time.time;
        return Random.value < probability;
    }

    public void EjectFood()
    {
        GameObject ejectedMass = Instantiate(massPrefab, transform.position, Quaternion.identity);
        MassForce massForce = ejectedMass.GetComponent<MassForce>();
        massForce.SetupEjectedMass(true, currentTargetPos);

        mass -= massForce.Mass;
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
        wobble.UpdateScale(transform);

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