using UnityEngine;

public class MassForce : MonoBehaviour
{
    private Collider2D col;
    private Bounds arenaBounds;

    private float mass = 0.7f;
    public float Mass => mass;

    private bool applyForce = false;
    private Vector3 currentTargetPos;

    private float speed = 60f;
    private float loseSpeed = 140f;
    private float randomRotation = 10f;
    private float randomForce = 5f;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    void Start()
    {
        if (applyForce == false)
        {
            enabled = false;
            return;
        }

        Vector2 direction = transform.position - currentTargetPos;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
        angle += Random.Range(-randomRotation, randomRotation);
        transform.rotation = Quaternion.Euler(0, 0, angle);
        speed += Random.Range(-randomForce, randomForce);
    }

    void Update()
    {
        transform.Translate(speed * Time.deltaTime * Vector2.up);
        speed -= loseSpeed * Time.deltaTime;

        if (speed <= 0)
        {
            enabled = false;
        }

        if (!arenaBounds.Contains(transform.position))
        {
            Destroy(gameObject);
        }
    }

    public void SetupEjectedMass(bool _applyForce, Vector3 _currentTargetPos, Bounds _arenaColBounds)
    {
        applyForce = _applyForce;
        currentTargetPos = _currentTargetPos;
        arenaBounds = _arenaColBounds;

        gameObject.layer = LayerMask.NameToLayer("Default");
        col.enabled = false;

        Invoke(nameof(EnableMassInteraction), 0.2f);
    }

    private void EnableMassInteraction()
    {
        gameObject.layer = LayerMask.NameToLayer("Food");
        col.enabled = true;
    }
}