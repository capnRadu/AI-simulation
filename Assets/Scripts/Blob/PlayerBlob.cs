using UnityEngine;

public class PlayerBlob : Blob
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) && mass > 1f)
        {
            EjectFood();
        }
    }

    private void FixedUpdate()
    {
        Vector2 lookDir = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        lookDir.Normalize();
        rb.linearVelocity = lookDir * speed;

        ClampPosition();
    }

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