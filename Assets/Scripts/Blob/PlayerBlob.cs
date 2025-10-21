using UnityEngine;

/// <summary>
/// The MonoBehaviour for the player-controlled blob.
/// </summary>
public class PlayerBlob : Blob
{
    [Header("Sprint Settings")]
    [Tooltip("The speed multiplier to apply when sprinting")]
    [SerializeField] private float sprintMultiplier = 2f;
    [Tooltip("Mass lost per second while sprinting")]
    [SerializeField] private float sprintMassLossRate = 10f;
    [Tooltip("The minimum mass required to be able to sprint")]
    [SerializeField] private float minSprintMass = 0.5f;

    private bool isSprinting = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) && mass > 1f)
        {
            EjectFood();
        }

        if (Input.GetMouseButtonDown(0) && mass > minSprintMass)
        {
            isSprinting = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isSprinting = false;
        }

        if (isSprinting)
        {
            mass -= sprintMassLossRate * Time.deltaTime;
            mass = Mathf.Max(mass, minSprintMass); // Clamp mass

            float newScale = 1f + mass * scaleFactor;
            transform.localScale = new Vector3(newScale, newScale, 1f);
            wobble.UpdateScale(transform);
            speed = baseSpeed / (1f + mass * speedFactor);

            if (mass <= minSprintMass)
            {
                isSprinting = false;
            }
        }
    }

    private void FixedUpdate()
    {
        Vector2 lookDir = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        lookDir.Normalize();

        float currentSpeedMultiplier = isSprinting ? sprintMultiplier : 1f;
        rb.linearVelocity = currentSpeedMultiplier * speed * lookDir;

        ClampPosition();
    }

    /// <summary>
    /// Prevents the player from moving outside the arena bounds.
    /// </summary>
    private void ClampPosition()
    {
        if (arenaCol == null) return;

        Bounds bounds = ArenaColBounds;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, bounds.min.x, bounds.max.x);
        pos.y = Mathf.Clamp(pos.y, bounds.min.y, bounds.max.y);

        transform.position = pos;
    }
}