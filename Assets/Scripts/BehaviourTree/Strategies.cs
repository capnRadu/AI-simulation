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

public class FindAndSetTargetStrategy : IStrategy
{
    private readonly Blob blob;
    private readonly LayerMask mask;
    private readonly bool preyCheck;

    public FindAndSetTargetStrategy(Blob blob, LayerMask mask, bool preyCheck = false)
    {
        this.blob = blob;
        this.mask = mask;
        this.preyCheck = preyCheck;
    }

    public Node.Status Process()
    {
        Transform target = blob.FindClosestTarget(mask);

        if (target == null)
        {
            blob.ClearTarget();
            return Node.Status.Failure;
        }
        else 
        {
            Blob targetBlob = target.GetComponent<Blob>();

            if (preyCheck && targetBlob)
            {
                if (targetBlob.Mass >= blob.Mass)
                {
                    blob.ClearTarget();
                    return Node.Status.Failure;
                }
            }
        }

        // Assign safely
        blob.SetTarget(target);
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
    readonly Transform entity;
    readonly Func<Vector3> getTargetPos;
    readonly float speed;
    readonly float stoppingDistance;

    public MoveToTargetStrategy(Rigidbody2D rb, PhysicsCommandBuffer physicsBuffer, Transform entity, Func<Vector3> getTargetPos, float speed, float stoppingDistance)
    {
        this.rb = rb;
        this.physicsBuffer = physicsBuffer;
        this.entity = entity;
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

        var direction = (targetPos - entity.position).normalized;
        var distance = Vector3.Distance(entity.position, targetPos);

        if (distance <= stoppingDistance)
        {
            return Node.Status.Success;
        }

        physicsBuffer.Queue(() =>
        {
            rb.MovePosition(entity.position + speed * Time.fixedDeltaTime * direction);
        });

        return Node.Status.Running;
    }

    public void Reset()
    {
        // No state to reset in this strategy
    }
}