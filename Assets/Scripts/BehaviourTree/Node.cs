using System.Collections.Generic;
using System.Linq;

public class Repeat : Node
{
    readonly int repetitions;
    int count;

    public Repeat(string name, int repetitions) : base(name)
    {
        this.repetitions = repetitions;
    }
    
    public override Status Process()
    {
        if (children[0].Process() == Status.Success)
        {
            count++;

            if (count >= repetitions)
            {
                Reset();
                return Status.Success;
            }
        }

        return Status.Running;
    }

    public override void Reset()
    {
        base.Reset();
        count = 0;
    }
}

public class UntilSuccess : Node
{
    public UntilSuccess(string name) : base(name)
    {
    }
    
    public override Status Process()
    {
        if (children[0].Process() == Status.Success)
        {
            Reset();
            return Status.Success;
        }

        return Status.Running;
    }
}

public class UntilFail : Node
{
    public UntilFail(string name) : base(name)
    {
    }
    
    public override Status Process()
    {
        if (children[0].Process() == Status.Failure)
        {
            Reset();
            return Status.Failure;
        }

        return Status.Running;
    }
}

public class Inverter : Node
{
    public Inverter(string name) : base(name)
    {
    }
    
    public override Status Process()
    {
        switch (children[0].Process())
        {
            case Status.Running:
                return Status.Running;
            case Status.Failure:
                return Status.Success;
            default:
                return Status.Failure;
        }
    }
}

public class RandomSelector : PrioritySelector
{
    protected override List<Node> SortChildren()
    {
        return children.Shuffle().ToList();
    }

    public RandomSelector(string name) : base(name)
    {
    }
}

public class PrioritySelector : Selector
{
    List<Node> sortedChildren;
    List<Node> SortedChildren => sortedChildren ??= SortChildren();

    protected virtual List<Node> SortChildren()
    {
        return children.OrderByDescending(child => child.priority).ToList();
    }

    public PrioritySelector(string name) : base(name)
    {
    }

    public override void Reset()
    {
        base.Reset();
        sortedChildren = null;
    }

    public override Status Process()
    {
        foreach (var child in SortedChildren)
        {
            switch (child.Process())
            {
                case Status.Running:
                    return Status.Running;
                case Status.Success:
                    return Status.Success;
                default:
                    continue;
            }
        }

        return Status.Failure;
    }
}

public class Selector : Node
{
    public Selector(string name, int priority = 0) : base(name, priority)
    {
    }

    public override Status Process ()
    {
        if (currentChild < children.Count)
        {
            switch (children[currentChild].Process())
            {
                case Status.Running:
                    return Status.Running;
                case Status.Success:
                    Reset();
                    return Status.Success;
                default:
                    currentChild++;
                    return Status.Running;
            }
        }

        Reset();
        return Status.Failure;
    }
}

public class Sequence : Node
{
    public Sequence(string name, int priority = 0) : base(name, priority)
    {
    }

    public override Status Process()
    {
        if (currentChild < children.Count)
        {
            switch (children[currentChild].Process())
            {
                case Status.Running:
                    return Status.Running;
                case Status.Failure:
                    Reset();
                    return Status.Failure;
                default:
                    currentChild++;
                    return currentChild == children.Count ? Status.Success : Status.Running;
            }
        }

        Reset();
        return Status.Success;
    }
}

public class Leaf : Node
{
    readonly IStrategy strategy;

    public Leaf(string name, IStrategy strategy, int priority = 0) : base(name, priority)
    {
        this.strategy = strategy;
    }

    public override Status Process()
    {
        return strategy.Process();
    }

    public override void Reset()
    {
        strategy.Reset();
    }
}

public class Node
{
    public enum Status
    {
        Success,
        Failure,
        Running
    }

    public readonly string name;
    public readonly int priority;

    public readonly List<Node> children = new List<Node>();
    protected int currentChild;

    public Node(string name = "Node", int priority = 0)
    {
        this.name = name;
        this.priority = priority;
    }
    
    public void AddChild(Node child)
    {
        children.Add(child);
    }

    public virtual Status Process()
    {
        return children[currentChild].Process();
    }

    public virtual void Reset()
    {
        currentChild = 0;

        foreach (var child in children)
        {
            child.Reset();
        }
    }
}

public class BehaviourTree : Node
{
    public BehaviourTree(string name) : base(name)
    {
    }

    public override Status Process()
    {
        while (currentChild < children.Count)
        {
            var status = children[currentChild].Process();

            if (status != Status.Success)
            {
                return status;
            }

            currentChild++;
        }

        Reset();
        return Status.Success;
    }
}