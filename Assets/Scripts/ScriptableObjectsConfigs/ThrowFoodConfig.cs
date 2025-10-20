using UnityEngine;

[CreateAssetMenu(fileName = "New Throw Food Config", menuName = "Blob/BT Strategies/Throw Food Config")]
public class ThrowFoodConfig : ScriptableObject
{
    [Header("Baiting Behavior")]
    [Tooltip("Blob will try to stay this much heavier than prey (e.g., 1.1 = 10% heavier)")]
    [Range(1.1f, 10.0f)]
    public float safetyMargin = 1.1f;

    [Tooltip("Min number of food pieces to throw")]
    [Range(1, 10)]
    public int minSpawnCount = 1;
    [Tooltip("Max number of food pieces to throw")]
    [Range(1, 50)]
    public int maxSpawnCount = 20;
}
