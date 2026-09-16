using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private Transform destination;

    private EnemyHealth health;
    private float speedMultiplier = 1f;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
    }

    public void SetDestination(Transform target)
    {
        destination = target;
    }

    // La oleada configura este valor al crear cada enemigo.
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0f, multiplier);
    }

    private void Update()
    {
        // Sin destino, el enemigo queda quieto para practicar el combate por separado.
        if (destination == null || !health.IsAlive)
            return;

        Vector3 target = destination.position;
        target.z = transform.position.z;
        float currentSpeed = Mathf.Max(0f, speed) * speedMultiplier;
        transform.position = Vector3.MoveTowards(transform.position, target, currentSpeed * Time.deltaTime);

        if ((transform.position - target).sqrMagnitude <= 0.0025f)
            health.ReachDestination();
    }
}
