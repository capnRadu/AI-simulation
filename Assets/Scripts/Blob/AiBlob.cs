using UnityEngine;

/// <summary>
/// The main MonoBehaviour for AI-controlled blobs.
/// This class is responsible for:
/// 1. Holding the AI's configuration (ScriptableObjects).
/// 2. Setting up the Behavior Tree in 'SetupBlob'.
/// 3. Ticking the tree in 'Update'.
/// 4. Providing helper methods for the strategies (e.g., finding targets, setting state).
/// </summary>
public class AiBlob : Blob
{
    // --- Behavior Tree ---
    private BehaviourTree tree;
    private PhysicsCommandBuffer physicsBuffer;

    // --- Configuration Assets ---
    [Header("AI Configuration Assets")]
    [Tooltip("Defines behavior priorities and timing")]
    [SerializeField] private AiBehaviorConfig behaviorConfig;
    [Tooltip("Defines *initial* detection radius")]
    [SerializeField] private AiPerceptionConfig perceptionConfig;
    [Tooltip("Defines movement, stopping distance, and sprinting")]
    [SerializeField] private MovementConfig movementConfig;
    [Tooltip("Defines how the blob behaves when fleeing")]
    [SerializeField] private FleeConfig fleeConfig;
    [Tooltip("Defines how the blob throws food as bait")]
    [SerializeField] private ThrowFoodConfig throwFoodConfig;

    // --- AI State ---
    [Header("Blob State (Internal)")]
    [Tooltip("Friendly name set from the config")]
    public string blobName;

    // This blob's *local* detection radius (can be modified at runtime)
    private float detectionRadius;
    public float DetectionRadius => detectionRadius;

    // State variables for the behavior tree
    private Transform chaseTarget;
    private Vector3 wanderTarget;
    private Vector3 fleeTarget;
    private Vector3 currentTargetPos; // The *actual* target the 'MoveTo' strategy will use
    private float lastThrowCheckTime;

    #region Setup

    /// <summary>
    /// Overrides the base setup to initialize the AI and build the Behavior Tree.
    /// </summary>
    protected override void SetupBlob()
    {
        base.SetupBlob();

        if (behaviorConfig == null || perceptionConfig == null || movementConfig == null || fleeConfig == null || throwFoodConfig == null)
        {
            Debug.LogError($"AI Blob '{name}' is missing one or more configuration assets! Please assign them in the inspector.", this);
            return;
        }

        blobName = behaviorConfig.configName;
        gameObject.name = blobName;
        detectionRadius = perceptionConfig.detectionRadius;

        physicsBuffer = GetComponent<PhysicsCommandBuffer>();
        tree = new BehaviourTree("Blob Behaviour");

        PrioritySelector rootSelector = new PrioritySelector("RootSelector");

        // Flee sequence
        if (behaviorConfig.enableFlee)
        {
            var fleeSequence = new Sequence("FleeSequence", behaviorConfig.fleePriority);
            fleeSequence.AddChild(new Leaf("FindThreat", new FindAndSetFleeTargetStrategy(preyMask, FindBiggestThreat, transform, () => detectionRadius, () => ArenaColBounds, SetFleeTarget, fleeConfig)));
            fleeSequence.AddChild(new Leaf("Flee", new MoveToTargetStrategy(rb, physicsBuffer, transform, () => currentTargetPos, () => speed, () => mass, (m) => mass = m, scaleFactor, wobble.UpdateScale, movementConfig)));
            rootSelector.AddChild(fleeSequence);
        }

        // Chase food sequence
        if (behaviorConfig.enableChaseFood)
        {
            var chaseFoodSequence = new Sequence("ChaseFoodSequence", behaviorConfig.chaseFoodPriority);
            chaseFoodSequence.AddChild(new Leaf("FindFood", new FindAndSetChaseTargetStrategy(foodMask, false, FindClosestChaseTarget, () => mass, SetChaseTarget)));
            chaseFoodSequence.AddChild(new Leaf("MoveToFood", new MoveToTargetStrategy(rb, physicsBuffer, transform, () => currentTargetPos, () => speed, () => mass, (m) => mass = m,
                scaleFactor, wobble.UpdateScale, movementConfig)));
            chaseFoodSequence.AddChild(new Leaf("ResetTarget", new ActionStrategy(ClearChaseTarget)));
            rootSelector.AddChild(chaseFoodSequence);
        }

        // Chase prey sequence
        if (behaviorConfig.enableChasePrey)
        {
            var chasePreySequence = new Sequence("ChasePreySequence", behaviorConfig.chasePreyPriority);
            chasePreySequence.AddChild(new Leaf("FindPrey", new FindAndSetChaseTargetStrategy(preyMask, true, FindClosestChaseTarget, () => mass, SetChaseTarget)));
            chasePreySequence.AddChild(new Leaf("CheckMass", new Condition(() => mass > 1f)));
            chasePreySequence.AddChild(new Leaf("CheckRandomness", new Condition(() => ShouldThrowFood(behaviorConfig.throwFoodChance))));
            chasePreySequence.AddChild(new Leaf("ThrowBaitFood", new ThrowFoodStrategy(scaleFactor, speedFactor, baseSpeed, foodPrefabMass, () => GetChaseTargetMass(), () => mass, EjectFood,
                ScaleDetectionRadius, (s) => transform.localScale = s, wobble.UpdateScale, (s) => speed = s, transform, throwFoodConfig)));
            chasePreySequence.AddChild(new Leaf("MoveToPrey", new MoveToTargetStrategy(rb, physicsBuffer, transform, () => currentTargetPos, () => speed, () => mass, (m) => mass = m,
                scaleFactor, wobble.UpdateScale, movementConfig)));
            chasePreySequence.AddChild(new Leaf("ResetTarget", new ActionStrategy(ClearChaseTarget)));
            rootSelector.AddChild(chasePreySequence);
        }

        // Wander sequence
        if (behaviorConfig.enableWander)
        {
            var wanderSequence = new Sequence("WanderSequence", behaviorConfig.wanderPriority);
            wanderSequence.AddChild(new Leaf("FindWanderTarget", new FindAndSetWanderTargetStrategy(FindRandomPointInBounds, () => ArenaColBounds, SetWanderTarget)));
            wanderSequence.AddChild(new Leaf("MoveToWanderTarget", new MoveToTargetStrategy(rb, physicsBuffer, transform, () => currentTargetPos, () => speed, () => mass, (m) => mass = m,
                scaleFactor, wobble.UpdateScale, movementConfig)));
            rootSelector.AddChild(wanderSequence);
        }

        if (rootSelector.children.Count == 0)
        {
            Debug.LogWarning($"AI Blob '{name}' has no behaviors enabled in its Behavior Config! It will do nothing.", this);
        }

        tree.AddChild(rootSelector);
    }

    #endregion

    private void Update()
    {
        tree?.Process();
    }

    #region AI Helper Methods (Used by Strategies)

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
        if (chaseTarget == null) return 10000f;

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

    /// <summary>
    /// Checks if enough time has passed to 'roll the dice' for throwing food.
    /// </summary>
    private bool ShouldThrowFood(float probability)
    {
        if (Time.time - lastThrowCheckTime < behaviorConfig.throwFoodCheckInterval) return false;

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
        ScaleDetectionRadius(0.1f);
    }

    public void ScaleDetectionRadius(float scaleAmount)
    {
        detectionRadius += scaleAmount;
        detectionRadius = Mathf.Max(detectionRadius, 1f);
    }

    #endregion

    #region Debugging

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

    #endregion
}