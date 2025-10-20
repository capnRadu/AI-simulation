using UnityEngine;

/// <summary>
/// A simple visual effect script that makes the blob "wobble"
/// by adjusting its scale over time using a sine wave.
/// </summary>
public class Wobble : MonoBehaviour
{
    [Header("Wobble Settings")]
    public float wobbleSpeed = 4f;
    public float wobbleAmount = 0.05f;

    // The blob's "true" scale, which we wobble around
    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    /// <summary>
    /// Call this from the Blob script whenever its scale changes
    /// (e.g., after eating) to update the base scale.
    /// </summary>
    public void UpdateScale(Transform transform)
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float wobble = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount;

        // Squash and stretch
        transform.localScale = new Vector3(
            baseScale.x + wobble,
            baseScale.y - wobble,
            baseScale.z
        );
    }
}