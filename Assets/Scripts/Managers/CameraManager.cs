using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private Camera mainCamera;

    private Vector3 initialPos = new Vector3(22.5f, -22.5f, -10);
    private float initialSize = 45f;
    private float targetSize = 33f;

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