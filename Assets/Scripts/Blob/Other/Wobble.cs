using UnityEngine;

public class Wobble : MonoBehaviour
{
    public float wobbleSpeed = 4f;
    public float wobbleAmount = 0.05f;

    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

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