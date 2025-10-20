using UnityEngine;

/// <summary>
/// Manages the main camera, making it follow the player blob
/// and zoom in/out based on whether the player is alive.
/// </summary>
public class CameraManager : MonoBehaviour
{
    private Camera mainCamera;

    [Header("Camera States")]
    [Tooltip("Position to reset to when the player dies")]
    [SerializeField] private Vector3 initialPos = new Vector3(22.5f, -22.5f, -10);
    [Tooltip("Orthographic size (zoom) when player is dead")]
    [SerializeField] private float initialSize = 45f;
    [Tooltip("Orthographic size (zoom) when following the player")]
    [SerializeField] private float targetSize = 33f;

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
    }

    private void FixedUpdate()
    {
        PlayerBlob player = FindFirstObjectByType<PlayerBlob>();

        if (player != null)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(player.transform.position.x, player.transform.position.y, -10), Time.deltaTime * 5f);
            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetSize, Time.deltaTime * 5f);
        }
        else
        {
            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, initialSize, Time.deltaTime * 5f);
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, initialPos, Time.deltaTime * 5f);
        }
    }
}