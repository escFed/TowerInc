using System.Collections.Generic;
using UnityEngine;

public class TowerController : MonoBehaviour
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float range = 3.5f;
    [SerializeField] private float attackInterval = 0.6f;
    [SerializeField] private int damage = 10;
    [SerializeField] private LayerMask enemyLayers = ~0;

    private List<Collider2D> candidates = new List<Collider2D>();
    private float nextAttackTime;

    public EnemyHealth CurrentTarget { get; private set; }
    public int ShotsFired { get; private set; }

    private void Start()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("TowerController necesita un prefab de proyectil.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (Time.time < nextAttackTime || projectilePrefab == null)
            return;

        CurrentTarget = FindTarget();
        if (CurrentTarget == null)
        {
            nextAttackTime = Time.time + 0.1f;
            return;
        }

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Projectile projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
        projectile.Initialize(CurrentTarget, damage);
        ShotsFired++;
        nextAttackTime = Time.time + Mathf.Max(0.05f, attackInterval);
    }

    private EnemyHealth FindTarget()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayers);
        filter.useTriggers = true;
        candidates.Clear();
        Physics2D.OverlapCircle(transform.position, range, filter, candidates);

        EnemyHealth nearest = null;
        float nearestDistance = range * range;
        foreach (Collider2D candidate in candidates)
        {
            if (!candidate.TryGetComponent(out EnemyHealth enemy) || !enemy.IsAlive)
                continue;

            float distance = ((Vector2)(enemy.transform.position - transform.position)).sqrMagnitude;
            if (distance <= nearestDistance)
            {
                nearest = enemy;
                nearestDistance = distance;
            }
        }
        return nearest;
    }

    private void OnDisable()
    {
        CurrentTarget = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
