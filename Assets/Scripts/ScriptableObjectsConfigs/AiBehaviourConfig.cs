using UnityEngine;

/// <summary>
/// A ScriptableObject to define an AI's personality.
/// It controls which behaviors are enabled, their priority, and other specific tweaks.
/// </summary>
[CreateAssetMenu(fileName = "New AI Behavior Config", menuName = "Blob/AI/AI Behavior Config")]
public class AiBehaviorConfig : ScriptableObject
{
    [Header("Configuration Name")]
    [Tooltip("Used for UI")]
    public string configName = "Default Blob";

    [Header("Enabled Behaviors")]
    [Tooltip("Can the blob flee from threats? (If true, then 'Enable Chase Food' or 'Enable Chase Prey' should be true)")]
    public bool enableFlee = true;
    [Tooltip("Can the blob chase food?")]
    public bool enableChaseFood = true;
    [Tooltip("Can the blob chase smaller prey?")]
    public bool enableChasePrey = true;
    [Tooltip("Can the blob wander when idle? (Recommended to keep this true as a fallback behavior)")]
    public bool enableWander = true;

    [Header("Behavior Priorities (Higher Wins)")]
    [Range(1, 10)]
    public int fleePriority = 3;
    [Range(1, 10)]
    public int chaseFoodPriority = 2;
    [Range(1, 10)]
    public int chasePreyPriority = 2;
    [Range(1, 10)]
    public int wanderPriority = 1;

    [Header("Behavior Toggles")]
    [Tooltip("How often the blob can check if it should throw food")]
    [Range(0.1f, 10f)]
    public float throwFoodCheckInterval = 2f;
    [Tooltip("The random chance (0.0 to 1.0) to throw food when chasing")]
    [Range(0f, 1f)]
    public float throwFoodChance = 0.9f;
}