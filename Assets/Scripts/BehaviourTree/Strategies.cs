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
    private readonly AiBlob blob;
    private readonly float scaleFactor;
    private readonly float speedFactor;
    private readonly float baseSpeed;
    private readonly float massPrefabMass;
    private readonly Func<float> getChaseTargetMass;
    private readonly Wobble wobble;

    private float safetyMargin = 1.1f; // Keep 10% heavier than prey

    public ThrowFoodStrategy(AiBlob blob, float scaleFactor, float speedFactor, float baseSpeed, float massPrefabMass, Func<float> getChaseTargetMass, Wobble wobble)
    {
        this.blob = blob;
        this.scaleFactor = scaleFactor;
        this.speedFactor = speedFactor;
        this.baseSpeed = baseSpeed;
        this.massPrefabMass = massPrefabMass;
        this.getChaseTargetMass = getChaseTargetMass;
        this.wobble = wobble;
    }

    public Node.Status Process()
    {
        if (blob.Mass <= 1f) return Node.Status.Failure;

        int spawnCount = UnityEngine.Random.Range(1, 20);

        for (int i = 0; i < spawnCount; i++)
        {
            float predictedSelfMass = blob.Mass - massPrefabMass;
            float predictedPreyMass = getChaseTargetMass() + massPrefabMass;

            if (predictedSelfMass <= predictedPreyMass * safetyMargin)
            {
                break;
            }

            blob.EjectFood();
            blob.ScaleDetectionRadius(-0.1f);

            float newScale = 1f + blob.Mass * scaleFactor;
            blob.transform.localScale = new Vector3(newScale, newScale, 1f);
            wobble.UpdateScale(blob.transform);
            blob.Speed = baseSpeed / (1f + blob.Mass * speedFactor);
        }

        return Node.Status.Success;
    }

    public void Reset()
    {
        // nothing to reset
    }
}

public class FindAndSetFleeTargetStrategy : IStrategy
{
    private readonly AiBlob blob;
    private readonly LayerMask mask;

    public FindAndSetFleeTargetStrategy(AiBlob blob, LayerMask blobMask)
    {
        this.blob = blob;
        this.mask = blobMask;
    }

    public Node.Status Process()
    {
        Transform biggestThreat = blob.FindBiggestThreat(mask);

        if (biggestThreat == null)
        {
            return Node.Status.Failure;
        }

        Vector3 directionAway = (blob.transform.position - biggestThreat.position).normalized;
        Vector3 fleePoint = blob.transform.position + 0.8f * blob.DetectionRadius * directionAway;

        var bounds = blob.ArenaColBounds;
        fleePoint.x = Mathf.Clamp(fleePoint.x, bounds.min.x + 1f, bounds.max.x - 1f);
        fleePoint.y = Mathf.Clamp(fleePoint.y, bounds.min.y + 1f, bounds.max.y - 1f);

        if (Vector3.Distance(fleePoint, blob.transform.position) < 0.5f)
        {
            Vector3 inward = (bounds.center - blob.transform.position).normalized;
            fleePoint += inward * 2f;
        }

        blob.SetFleeTarget(fleePoint);
        return Node.Status.Success;
    }

    public void Reset()
    {
        // nothing to reset
    }
}

public class FindAndSetWanderTargetStrategy : IStrategy
{
    private readonly AiBlob blob;
    private readonly Transform chaseTarget;
    private readonly Func<Vector3> getTargetPos;

    public FindAndSetWanderTargetStrategy(AiBlob blob, Transform chaseTarget, Func<Vector3> getTargetPos)
    {
        this.blob = blob;
        this.chaseTarget = chaseTarget;
        this.getTargetPos = getTargetPos;
    }

    public Node.Status Process()
    {
        Vector3 targetPos = getTargetPos();

        if (chaseTarget == null || Vector3.Distance(blob.transform.position, targetPos) <= 0.2f)
        {
            Vector3 target = blob.FindRandomPointInBounds(blob.ArenaColBounds);
            blob.SetWanderTarget(target);
            return Node.Status.Success;
        }

        return Node.Status.Failure;
    }

    public void Reset()
    {
        // nothing to reset
    }
}

public class FindAndSetChaseTargetStrategy : IStrategy
{
    private readonly AiBlob blob;
    private readonly LayerMask mask;
    private readonly bool preyCheck;

    public FindAndSetChaseTargetStrategy(AiBlob blob, LayerMask mask, bool preyCheck = false)
    {
        this.blob = blob;
        this.mask = mask;
        this.preyCheck = preyCheck;
    }

    public Node.Status Process()
    {
        Transform target = blob.FindClosestChaseTarget(mask);

        if (target == null)
        {
            return Node.Status.Failure;
        }
        else 
        {
            Blob targetBlob = target.GetComponent<Blob>();

            if (preyCheck && targetBlob)
            {
                if (targetBlob.Mass >= blob.Mass)
                {
                    return Node.Status.Failure;
                }
            }
        }

        blob.SetChaseTarget(target);
        return Node.Status.Success;
    }

    public void Reset()
    {
        // nothing to reset
    }
}

public class MoveToTargetStrategy : IStrategy
{
    private readonly AiBlob aiBlob;
    private readonly Wobble wobble;

    private readonly Rigidbody2D rb;
    private readonly PhysicsCommandBuffer physicsBuffer;
    private readonly Transform self;
    private readonly Func<Vector3> getTargetPos;

    private readonly float speed;
    private readonly float stoppingDistance;

    private float sprintMultiplier = 2f;
    private float sprintMassLossRate = 10f;
    private float sprintCooldown = 5f;
    private float minSprintDuration = 1.5f;
    private float maxSprintDuration = 3f;

    private bool isSprinting = false;
    private float sprintEndTime = 0f;
    private float nextSprintTime = 0f;
    private float sprintStopMassThreshold;

    public MoveToTargetStrategy(AiBlob aiBlob, Wobble wobble, Rigidbody2D rb, PhysicsCommandBuffer physicsBuffer, Transform self, Func<Vector3> getTargetPos, float speed, float stoppingDistance)
    {
        this.aiBlob = aiBlob;
        this.wobble = wobble;
        this.rb = rb;
        this.physicsBuffer = physicsBuffer;
        this.self = self;
        this.getTargetPos = getTargetPos;
        this.speed = speed;
        this.stoppingDistance = stoppingDistance;
    }

    public Node.Status Process()
    {
        Vector3 targetPos = getTargetPos();

        var direction = (targetPos - self.position).normalized;
        var distance = Vector3.Distance(self.position, targetPos);

        if (distance <= stoppingDistance)
        {
            StopSprint();
            physicsBuffer.Clear();
            return Node.Status.Success;
        }
        else
        {
            HandleSprintLogic();

            float currentSpeed = speed * (isSprinting ? sprintMultiplier : 1f);

            physicsBuffer.Queue(() =>
            {
                rb.MovePosition(self.position + currentSpeed * Time.fixedDeltaTime * direction);
            });

            return Node.Status.Running;
        }
    }

    void HandleSprintLogic()
    {
        if (!isSprinting && Time.time > nextSprintTime && UnityEngine.Random.value < 0.9f)
        {
            StartSprint();
        }

        if (isSprinting)
        {
            aiBlob.Mass -= sprintMassLossRate * Time.deltaTime;
            aiBlob.Mass = Mathf.Max(aiBlob.Mass, 0.5f);

            self.transform.localScale = Vector3.one * (1f + aiBlob.Mass * aiBlob.ScaleFactor);
            wobble.UpdateScale(self);

            if (Time.time > sprintEndTime || aiBlob.Mass <= sprintStopMassThreshold)
            {
                StopSprint();
            }
        }
    }

    void StartSprint()
    {
        isSprinting = true;
        sprintEndTime = Time.time + UnityEngine.Random.Range(minSprintDuration, maxSprintDuration);
        sprintStopMassThreshold = aiBlob.Mass * UnityEngine.Random.Range(0.8f, 0.95f);
    }

    void StopSprint()
    {
        isSprinting = false;
        nextSprintTime = Time.time + sprintCooldown;
    }

    public void Reset()
    {
        isSprinting = false;
    }
}