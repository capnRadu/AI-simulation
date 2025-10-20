using UnityEngine;

/// <summary>
/// A ScriptableObject to define parameters for the 'Flee' behavior.
/// </summary>
[CreateAssetMenu(fileName = "New Flee Config", menuName = "Blob/BT Strategies/Flee Config")]
public class FleeConfig : ScriptableObject
{
    [Header("Flee Behavior")]
    [Tooltip("How far to flee, as a multiplier of detection radius")]
    [Range(0.1f, 2f)]
    public float fleeDistanceMultiplier = 0.8f;
    [Tooltip("How far from the arena edge to stop")]
    [Range(0.1f, 2f)]
    public float arenaEdgeMargin = 1f;
    [Tooltip("How much to boost away from the wall if trapped")]
    [Range(1f, 5f)]
    public float wallFleeBoost = 2f;
    [Tooltip("The minimum distance to flee if trapped")]
    [Range(0.1f, 2f)]
    public float minFleeDistance = 0.5f;
}