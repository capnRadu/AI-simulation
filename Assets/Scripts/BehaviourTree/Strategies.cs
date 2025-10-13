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
    private AiBlob blob;
    private float scaleFactor;
    private float speedFactor;
    private float baseSpeed;
    private Wobble wobble;

    public ThrowFoodStrategy(AiBlob blob, float scaleFactor, float speedFactor, float baseSpeed, Wobble wobble)
    {
        this.blob = blob;
        this.scaleFactor = scaleFactor;
        this.speedFactor = speedFactor;
        this.baseSpeed = baseSpeed;
        this.wobble = wobble;
    }

    public Node.Status Process()
    {
        if (blob.Mass <= 1f) return Node.Status.Failure;

        int spawnCount = UnityEngine.Random.Range(1, 15);

        for (int i = 0; i < spawnCount; i++)
        {
            blob.EjectFood();

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
    readonly Rigidbody2D rb;
    readonly PhysicsCommandBuffer physicsBuffer;
    readonly Transform self;
    readonly Func<Vector3> getTargetPos;
    readonly float speed;
    readonly float stoppingDistance;

    public MoveToTargetStrategy(Rigidbody2D rb, PhysicsCommandBuffer physicsBuffer, Transform self, Func<Vector3> getTargetPos, float speed, float stoppingDistance)
    {
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

        if (targetPos == null)
        {
            Debug.LogWarning("Target position is null.");
            return Node.Status.Failure;
        }

        var direction = (targetPos - self.position).normalized;
        var distance = Vector3.Distance(self.position, targetPos);

        if (distance <= stoppingDistance)
        {
            return Node.Status.Success;
        }

        physicsBuffer.Queue(() =>
        {
            rb.MovePosition(self.position + speed * Time.fixedDeltaTime * direction);
        });

        return Node.Status.Running;
    }

    public void Reset()
    {
        // No state to reset in this strategy
    }
}