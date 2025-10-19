using UnityEngine;

public class AiBlob : Blob
{
    private BehaviourTree tree;
    private PhysicsCommandBuffer physicsBuffer;

    [SerializeField] private float detectionRadius = 10f;
    public float DetectionRadius => detectionRadius;

    private Transform chaseTarget;
    private Vector3 wanderTarget;
    private Vector3 fleeTarget;
    private Vector3 currentTargetPos;

    private float lastThrowCheckTime;
    private float throwCheckInterval = 2f;

    protected override void SetupBlob()
    {
        base.SetupBlob();

        physicsBuffer = GetComponent<PhysicsCommandBuffer>();
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
        chasePreySequence.AddChild(new Leaf("CheckMass", new Condition(() => mass > 1f)));
        chasePreySequence.AddChild(new Leaf("CheckRandomness", new Condition(() => ShouldThrowFood(0.9f))));
        chasePreySequence.AddChild(new Leaf("ThrowBaitFood", new ThrowFoodStrategy(this, scaleFactor, speedFactor, baseSpeed, massPrefabMass, () => GetChaseTargetMass(), wobble)));
        chasePreySequence.AddChild(new Leaf("MoveToPrey", new MoveToTargetStrategy(rb, physicsBuffer, transform, () => currentTargetPos, speed, 0.2f)));
        chasePreySequence.AddChild(new Leaf("ResetTarget", new ActionStrategy(ClearChaseTarget)));

        // Wander sequence
        var wanderSequence = new Sequence("WanderSequence", 1);

        wanderSequence.AddChild(new Leaf("FindWanderTarget", new FindAndSetWanderTargetStrategy(this, chaseTarget, () => currentTargetPos)));
        wanderSequence.AddChild(new Leaf("MoveToWanderTarget", new MoveToTargetStrategy(rb, physicsBuffer, transform, () => currentTargetPos, speed, 0.2f)));

        rootSelector.AddChild(fleeSequence);
        rootSelector.AddChild(chaseFoodSequence);
        rootSelector.AddChild(chasePreySequence);
        rootSelector.AddChild(wanderSequence);

        tree.AddChild(rootSelector);
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

    private float GetChaseTargetMass()
    {
        Blob targetBlob = chaseTarget.GetComponent<Blob>();
        return targetBlob.Mass;
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

    protected override Vector3 GetInstatiateDirection()
    {
        return currentTargetPos;
    }

    protected override void HandleConsumption(GameObject other, float otherMass)
    {
        base.HandleConsumption(other, otherMass);
        ClearChaseTarget();
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