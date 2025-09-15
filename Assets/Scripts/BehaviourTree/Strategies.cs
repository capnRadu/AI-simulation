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

public class FindAndSetWanderTargetStrategy : IStrategy
{
    private readonly Blob blob;
    private readonly Transform chaseTarget;
    private readonly Func<Vector3> getTargetPos;

    public FindAndSetWanderTargetStrategy(Blob blob, Transform chaseTarget, Func<Vector3> getTargetPos)
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
}

public class FindAndSetChaseTargetStrategy : IStrategy
{
    private readonly Blob blob;
    private readonly LayerMask mask;
    private readonly bool preyCheck;

    public FindAndSetChaseTargetStrategy(Blob blob, LayerMask mask, bool preyCheck = false)
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

        // Assign safely
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