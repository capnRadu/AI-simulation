using UnityEngine;

public class PhysicsCommandBuffer : MonoBehaviour
{
    private System.Action physicsActions;

    public void Queue(System.Action action)
    {
        physicsActions += action;
    }

    public void Clear()
    {
        physicsActions = null;
    }

    private void FixedUpdate()
    {
        physicsActions?.Invoke();
        physicsActions = null; // clear for next frame
    }
}