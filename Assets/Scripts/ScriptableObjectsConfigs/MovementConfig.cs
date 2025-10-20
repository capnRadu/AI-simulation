using UnityEngine;

[CreateAssetMenu(fileName = "New Movement Config", menuName = "Blob/BT Strategies/Movement Config")]
public class MovementConfig : ScriptableObject
{
    [Header("Standard Movement")]
    [Tooltip("How close the blob needs to get to its target to succeed")]
    [Range(0.5f, 2f)]
    public float stoppingDistance = 0.5f;

    [Header("Sprint Settings")]
    [Tooltip("Multiplier to apply to speed when sprinting")]
    [Range(1.5f, 4f)]
    public float sprintMultiplier = 2f;
    [Tooltip("Mass loss rate per second while sprinting")]
    [Range(5f, 20f)]
    public float sprintMassLossRate = 10f;
    [Tooltip("Cooldown time in seconds between sprints")]
    [Range(1f, 10f)]
    public float sprintCooldown = 5f;
    [Tooltip("Minimum duration in seconds for a sprint")]
    [Range(0.5f, 5f)]
    public float minSprintDuration = 1.5f;
    [Tooltip("Maximum duration in seconds for a sprint")]
    [Range(1f, 10f)]
    public float maxSprintDuration = 3f;
}