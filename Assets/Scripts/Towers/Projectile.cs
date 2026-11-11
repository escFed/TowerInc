using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 9f;
    [SerializeField] private float hitDistance = 0.15f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField, Min(0f)] private float blastRadius;

    private EnemyHealth target;
    private int damage;
    private float remainingLifetime;
    private float effectiveBlastRadius;
    private Vector3 lastDestination;
    private Vector2 firingDirection;

    public float BlastRadius => blastRadius;

    public void Initialize(EnemyHealth enemy, int attackDamage, float attackBlastRadius = -1f)
    {
        target = enemy;
        damage = Mathf.Max(0, attackDamage);
        effectiveBlastRadius = attackBlastRadius < 0f ? blastRadius : attackBlastRadius;
        remainingLifetime = lifetime;
        if (enemy != null)
            lastDestination = enemy.transform.position;
    }

    public void Initialize(EnemyHealth enemy, int attackDamage, float attackBlastRadius,
        Vector2 direction)
    {
        Initialize(enemy, attackDamage, attackBlastRadius);
        firingDirection = direction.normalized;
    }

    private void Update()
    {
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0)
        {
            if (effectiveBlastRadius > 0f)
                Explode(transform.position);
            Destroy(gameObject);
            return;
        }

        if (firingDirection.sqrMagnitude > 0f)
        {
            MoveAlongFiringDirection();
            return;
        }

        if (target != null && target.IsAlive)
            lastDestination = target.transform.position;
        else if (effectiveBlastRadius <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 destination = lastDestination;
        destination.z = transform.position.z;
        transform.position = Vector3.MoveTowards(transform.position, destination, Mathf.Max(0.1f, speed) * Time.deltaTime);

        // MoveTowards no sobrepasa el objetivo, aun con proyectiles rapidos.
        if ((transform.position - destination).sqrMagnitude <= hitDistance * hitDistance)
        {
            if (effectiveBlastRadius > 0f)
                Explode(destination);
            else if (target != null)
                target.TakeDamage(damage);
            Destroy(gameObject);
        }
    }

    private void MoveAlongFiringDirection()
    {
        float distance = Mathf.Max(0.1f, speed) * Time.deltaTime;
        Vector2 position = transform.position;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(position, hitDistance,
            firingDirection, distance);
        foreach (RaycastHit2D hit in hits)
        {
            if (!hit.collider.TryGetComponent(out EnemyHealth enemy) || !enemy.IsAlive)
                continue;

            transform.position = new Vector3(hit.centroid.x, hit.centroid.y,
                transform.position.z);
            if (effectiveBlastRadius > 0f)
                Explode(hit.point);
            else
                enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(firingDirection * distance);
    }

    private void Explode(Vector2 position)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, effectiveBlastRadius);
        HashSet<EnemyHealth> damaged = new HashSet<EnemyHealth>();
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out EnemyHealth enemy) && damaged.Add(enemy))
                enemy.TakeDamage(damage);
        }
    }
}
