using UnityEngine;

/// <summary>
/// A ScriptableObject to define an AI's perception settings.
/// </summary>
[CreateAssetMenu(fileName = "New AI Perception Config", menuName = "Blob/AI/AI Perception Config")]
public class AiPerceptionConfig : ScriptableObject
{
    [Header("Sensing")]
    [Tooltip("How far the blob can sense food, prey, and threats")]
    [Range(5f, 15f)]
    public float detectionRadius = 10f;
}