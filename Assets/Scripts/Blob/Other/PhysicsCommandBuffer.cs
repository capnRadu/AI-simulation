using UnityEngine;

/// <summary>
/// A utility script to buffer AI movement commands.
/// The BT runs in the 'Update' loop, but physics runs in 'FixedUpdate'.
/// Strategies 'Queue' their movement commands (Actions) here.
/// This script then executes all buffered commands at once during FixedUpdate.
/// </summary>
public class PhysicsCommandBuffer : MonoBehaviour
{
    // A delegate holding all queued physics actions for this frame
    private System.Action physicsActions;

    public void Queue(System.Action action)
    {
        physicsActions += action;
    }

    private void FixedUpdate()
    {
        physicsActions?.Invoke();
        physicsActions = null; // clear for next frame
    }
}