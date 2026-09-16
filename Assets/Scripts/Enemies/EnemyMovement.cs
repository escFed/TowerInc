using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private Transform destination;

    private EnemyHealth health;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
    }

    public void SetDestination(Transform target)
    {
        destination = target;
    }

    private void Update()
    {
        // Sin destino, el enemigo queda quieto para practicar el combate por separado.
        if (destination == null || !health.IsAlive)
            return;

        Vector3 target = destination.position;
        target.z = transform.position.z;
        transform.position = Vector3.MoveTowards(transform.position, target, Mathf.Max(0, speed) * Time.deltaTime);

        if ((transform.position - target).sqrMagnitude <= 0.0025f)
            health.ReachDestination();
    }
}
