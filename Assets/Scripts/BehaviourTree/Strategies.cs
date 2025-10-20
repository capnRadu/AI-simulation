using System;
using UnityEngine;

public interface IStrategy
{
    Node.Status Process();
    void Reset()
    {
        // Noop
    }
}

public class ActionStrategy : IStrategy
{
    readonly Action doSomething;

    public ActionStrategy(Action doSomething)
    {
        this.doSomething = doSomething;
    }

    public Node.Status Process()
    {
        doSomething();
        return Node.Status.Success;
    }
}

public class Condition : IStrategy
{
    readonly Func<bool> predicate;

    public Condition(Func<bool> predicate)
    {
        this.predicate = predicate;
    }

    public Node.Status Process()
    {
        return predicate() ? Node.Status.Success : Node.Status.Failure;
    }
}

public class ThrowFoodStrategy : IStrategy
{
    private readonly float scaleFactor;
    private readonly float speedFactor;
    private readonly float baseSpeed;
    private readonly float foodPrefabMass;
    private readonly Func<float> getChaseTargetMass;
    private readonly Func<float> getMass;
    private readonly Action ejectFood;
    private readonly Action<float> scaleDetectionRadius;
    private readonly Action<Vector3> setScale;
    private readonly Action<Transform> updateScale;
    private readonly Action<float> setSpeed;
    private readonly Transform self;
    private readonly ThrowFoodConfig config;

    public ThrowFoodStrategy(float scaleFactor, float speedFactor, float baseSpeed, float foodPrefabMass, Func<float> getChaseTargetMass, Func<float> getMass, Action ejectFood,
        Action<float> scaleDetectionRadius, Action<Vector3> setScale, Action<Transform> updateScale, Action<float> setSpeed, Transform self, ThrowFoodConfig config)
    {
        this.scaleFactor = scaleFactor;
        this.speedFactor = speedFactor;
        this.baseSpeed = baseSpeed;
        this.foodPrefabMass = foodPrefabMass;
        this.getChaseTargetMass = getChaseTargetMass;
        this.getMass = getMass;
        this.ejectFood = ejectFood;
        this.scaleDetectionRadius = scaleDetectionRadius;
        this.setScale = setScale;
        this.updateScale = updateScale;
        this.setSpeed = setSpeed;
        this.self = self;
        this.config = config;
    }

    public Node.Status Process()
    {
        if (getMass() <= 1f) return Node.Status.Failure;

        int spawnCount = UnityEngine.Random.Range(config.minSpawnCount, config.maxSpawnCount + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            float predictedSelfMass = getMass() - foodPrefabMass;
            float predictedPreyMass = getChaseTargetMass() + foodPrefabMass;

            if (predictedSelfMass <= predictedPreyMass * config.safetyMargin)
            {
                break;
            }

            ejectFood();
            scaleDetectionRadius(-0.1f);

            float currentMass = getMass();
            float newScaleValue = 1f + currentMass * scaleFactor;
            setScale(new Vector3(newScaleValue, newScaleValue, 1f));
            updateScale(self);
            setSpeed(baseSpeed / (1f + currentMass * speedFactor));
        }

        return Node.Status.Success;
    }
}

public class FindAndSetFleeTargetStrategy : IStrategy
{
    private readonly LayerMask mask;
    private readonly Func<LayerMask, Transform> findBiggestThreat;
    private readonly Transform self;
    private readonly Func<float> getDetectionRadius;
    private readonly Func<Bounds> getArenaBounds;
    private readonly Action<Vector3> setFleeTarget;
    private readonly FleeConfig config;

    public FindAndSetFleeTargetStrategy(LayerMask mask, Func<LayerMask, Transform> findBiggestThreat, Transform self, Func<float> getDetectionRadius, Func<Bounds> getArenaBounds,
        Action<Vector3> setFleeTarget, FleeConfig config)
    {
        this.mask = mask;
        this.findBiggestThreat = findBiggestThreat;
        this.self = self;
        this.getDetectionRadius = getDetectionRadius;
        this.getArenaBounds = getArenaBounds;
        this.setFleeTarget = setFleeTarget;
        this.config = config;
    }

    public Node.Status Process()
    {
        Transform biggestThreat = findBiggestThreat(mask);
        if (biggestThreat == null)  return Node.Status.Failure;

        Vector3 directionAway = (self.position - biggestThreat.position).normalized;
        Vector3 fleePoint = self.position + config.fleeDistanceMultiplier * getDetectionRadius() * directionAway;

        var bounds = getArenaBounds();
        fleePoint.x = Mathf.Clamp(fleePoint.x, bounds.min.x + config.arenaEdgeMargin, bounds.max.x - config.arenaEdgeMargin);
        fleePoint.y = Mathf.Clamp(fleePoint.y, bounds.min.y + config.arenaEdgeMargin, bounds.max.y - config.arenaEdgeMargin);

        if (Vector3.Distance(fleePoint, self.position) < config.minFleeDistance)
        {
            Vector3 inward = (bounds.center - self.position).normalized;
            fleePoint += inward * config.wallFleeBoost;
        }

        setFleeTarget(fleePoint);
        return Node.Status.Success;
    }
}

public class FindAndSetWanderTargetStrategy : IStrategy
{
    private readonly Func<Bounds, Vector3> findRandomPointInBounds;
    private readonly Func<Bounds> getArenaBounds;
    private readonly Action<Vector3> setWanderTarget;

    public FindAndSetWanderTargetStrategy(Func<Bounds, Vector3> findRandomPointInBounds, Func<Bounds> getArenaBounds, Action<Vector3> setWanderTarget)
    {
        this.findRandomPointInBounds = findRandomPointInBounds;
        this.getArenaBounds = getArenaBounds;
        this.setWanderTarget = setWanderTarget;
    }

    public Node.Status Process()
    {
        Vector3 target = findRandomPointInBounds(getArenaBounds());
        setWanderTarget(target);

        return Node.Status.Success;
    }
}

public class FindAndSetChaseTargetStrategy : IStrategy
{
    private readonly LayerMask mask;
    private readonly bool preyCheck;
    private readonly Func<LayerMask, Transform> findClosestChaseTarget;
    private readonly Func<float> getMass;
    private readonly Action<Transform> setChaseTarget;

    public FindAndSetChaseTargetStrategy(LayerMask mask, bool preyCheck, Func<LayerMask, Transform> findClosestChaseTarget, Func<float> getMass, Action<Transform> setChaseTarget)
    {
        this.mask = mask;
        this.preyCheck = preyCheck;
        this.findClosestChaseTarget = findClosestChaseTarget;
        this.getMass = getMass;
        this.setChaseTarget = setChaseTarget;
    }

    public Node.Status Process()
    {
        Transform target = findClosestChaseTarget(mask);

        if (target == null)
        {
            return Node.Status.Failure;
        }
        else
        {
            Blob targetBlob = target.GetComponent<Blob>();

            if (preyCheck && targetBlob)
            {
                if (targetBlob.Mass >= getMass())
                {
                    setChaseTarget(null);
                    return Node.Status.Failure;
                }
            }
        }

        setChaseTarget(target);
        return Node.Status.Success;
    }
}

public class MoveToTargetStrategy : IStrategy
{
    private readonly Rigidbody2D rb;
    private readonly PhysicsCommandBuffer physicsBuffer;
    private readonly Transform self;
    private readonly Func<Vector3> getTargetPos;
    private readonly Func<float> getCurrentSpeed;
    private readonly Func<float> getMass;
    private readonly Action<float> setMass;
    private readonly float scaleFactor;
    private readonly Action<Transform> updateScale;
    private readonly MovementConfig config;

    private bool isSprinting = false;
    private float sprintEndTime = 0f;
    private float nextSprintTime = 0f;
    private float sprintStopMassThreshold;

    public MoveToTargetStrategy(Rigidbody2D rb, PhysicsCommandBuffer physicsBuffer, Transform self, Func<Vector3> getTargetPos, Func<float> getCurrentSpeed, Func<float> getMass,
        Action<float> setMass, float scaleFactor, Action<Transform> updateScale, MovementConfig config)
    {
        this.rb = rb;
        this.physicsBuffer = physicsBuffer;
        this.self = self;
        this.getTargetPos = getTargetPos;
        this.getCurrentSpeed = getCurrentSpeed;
        this.getMass = getMass;
        this.setMass = setMass;
        this.scaleFactor = scaleFactor;
        this.updateScale = updateScale;
        this.config = config;
    }

    public Node.Status Process()
    {
        Vector3 targetPos = getTargetPos();
        var direction = (targetPos - self.position).normalized;
        var distance = Vector3.Distance(self.position, targetPos);

        if (distance <= config.stoppingDistance)
        {
            StopSprint();
            return Node.Status.Success;
        }
        else
        {
            HandleSprintLogic();

            float currentSpeed = getCurrentSpeed() * (isSprinting ? config.sprintMultiplier : 1f);

            physicsBuffer.Queue(() =>
            {
                rb.MovePosition(self.position + currentSpeed * Time.fixedDeltaTime * direction);
            });

            return Node.Status.Running;
        }
    }

    private void HandleSprintLogic()
    {
        if (!isSprinting && Time.time > nextSprintTime && UnityEngine.Random.value < 0.9f)
        {
            StartSprint();
        }

        if (isSprinting)
        {
            float currentMass = getMass();
            currentMass -= config.sprintMassLossRate * Time.deltaTime;
            setMass(Mathf.Max(currentMass, 0.5f));

            self.transform.localScale = Vector3.one * (1f + getMass() * scaleFactor);
            updateScale(self);

            if (Time.time > sprintEndTime || getMass() <= sprintStopMassThreshold)
            {
                StopSprint();
            }
        }
    }

    private void StartSprint()
    {
        isSprinting = true;
        sprintEndTime = Time.time + UnityEngine.Random.Range(config.minSprintDuration, config.maxSprintDuration);
        sprintStopMassThreshold = getMass() * UnityEngine.Random.Range(0.8f, 0.95f);
    }

    private void StopSprint()
    {
        isSprinting = false;
        nextSprintTime = Time.time + config.sprintCooldown;
    }

    public void Reset()
    {
        isSprinting = false;
    }
}