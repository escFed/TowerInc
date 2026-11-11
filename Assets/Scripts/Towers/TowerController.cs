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
    [SerializeField, Min(1f)] private float rotationSpeed = 540f;
    [Header("Sprites de evolución")]
    [SerializeField] private Sprite levelTwoSprite;
    [SerializeField] private Sprite levelThreeSprite;

    private List<Collider2D> candidates = new List<Collider2D>();
    private readonly List<EnemyHealth> targetCandidates = new List<EnemyHealth>();
    private float nextAttackTime;
    private SpriteRenderer spriteRenderer;
    private Sprite baseSprite;

    public EnemyHealth CurrentTarget { get; private set; }
    public int ShotsFired { get; private set; }
    public TowerKind Kind { get; private set; }
    public bool HasAppliedProgress { get; private set; }
    public int Level { get; private set; } = 1;
    public int ProjectileCount { get; private set; } = 1;
    public int EffectiveDamage { get; private set; }
    public float EffectiveRange { get; private set; }
    public float EffectiveAttackInterval { get; private set; }
    public float EffectiveBlastRadius { get; private set; }

    public Sprite GetLevelSprite(int level)
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (level >= 3 && levelThreeSprite != null)
            return levelThreeSprite;
        if (level >= 2 && levelTwoSprite != null)
            return levelTwoSprite;
        return baseSprite != null ? baseSprite : renderer == null ? null : renderer.sprite;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            baseSprite = spriteRenderer.sprite;
        EffectiveDamage = damage;
        EffectiveRange = range;
        EffectiveAttackInterval = attackInterval;
        EffectiveBlastRadius = projectilePrefab == null ? 0f : projectilePrefab.BlastRadius;
    }

    public void ApplyProgress(TowerKind kind, ShopService shop)
    {
        if (shop == null)
            return;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                baseSprite = spriteRenderer.sprite;
        }

        Kind = kind;
        HasAppliedProgress = true;
        Level = Mathf.Clamp(shop.GetTowerLevel(kind), 1, 3);
        ProjectileCount = Level;
        int damageUpgrades = shop.GetStatUpgradeCount(kind, ShopEffectType.Damage);
        int rangeUpgrades = shop.GetStatUpgradeCount(kind, ShopEffectType.Range);
        int speedUpgrades = shop.GetStatUpgradeCount(kind, ShopEffectType.AttackSpeed);
        EffectiveDamage = Mathf.Max(1, Mathf.RoundToInt(
            damage * (1f + 0.12f * (Level - 1) + 0.1f * damageUpgrades)));
        EffectiveRange = Mathf.Max(0.1f,
            range * (1f + 0.08f * (Level - 1) + 0.08f * rangeUpgrades));
        EffectiveAttackInterval = Mathf.Max(0.05f,
            attackInterval / (1f + 0.1f * (Level - 1) + 0.1f * speedUpgrades));
        float baseBlastRadius = projectilePrefab == null ? 0f : projectilePrefab.BlastRadius;
        EffectiveBlastRadius = baseBlastRadius * (1f + 0.08f * (Level - 1) +
            0.15f * shop.GetStatUpgradeCount(kind, ShopEffectType.BlastRadius));

        if (spriteRenderer != null)
        {
            Sprite upgraded = GetLevelSprite(Level);
            if (upgraded != null)
                spriteRenderer.sprite = upgraded;
        }
    }

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
        if (projectilePrefab == null)
            return;

        CollectTargets();
        CurrentTarget = targetCandidates.Count == 0 ? null : targetCandidates[0];
        if (CurrentTarget == null)
            return;

        Vector2 direction = CurrentTarget.transform.position - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                rotationSpeed * Time.deltaTime);
            if (Quaternion.Angle(transform.rotation, targetRotation) > 5f)
                return;
        }

        if (Time.time < nextAttackTime)
            return;

        Transform muzzle = firePoint != null ? firePoint : transform;
        Vector3 origin = muzzle.position;
        Vector2 firingDirection = muzzle.up;
        for (int i = 0; i < ProjectileCount; i++)
        {
            EnemyHealth target = targetCandidates[i % targetCandidates.Count];
            float offset = (i - (ProjectileCount - 1) * 0.5f) * 0.18f;
            Projectile projectile = Instantiate(projectilePrefab,
                origin + muzzle.right * offset, muzzle.rotation);
            projectile.Initialize(target, EffectiveDamage, EffectiveBlastRadius, firingDirection);
            ShotsFired++;
        }
        nextAttackTime = Time.time + EffectiveAttackInterval;
    }

    private void CollectTargets()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayers);
        filter.useTriggers = true;
        candidates.Clear();
        targetCandidates.Clear();
        Physics2D.OverlapCircle(transform.position, EffectiveRange, filter, candidates);

        foreach (Collider2D candidate in candidates)
        {
            if (!candidate.TryGetComponent(out EnemyHealth enemy) || !enemy.IsAlive)
                continue;

            float distance = ((Vector2)(enemy.transform.position - transform.position)).sqrMagnitude;
            int index = 0;
            while (index < targetCandidates.Count)
            {
                float otherDistance = ((Vector2)(targetCandidates[index].transform.position -
                    transform.position)).sqrMagnitude;
                if (distance < otherDistance)
                    break;
                index++;
            }
            if (!targetCandidates.Contains(enemy))
                targetCandidates.Insert(index, enemy);
        }
    }

    private void OnDisable()
    {
        CurrentTarget = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position,
            EffectiveRange > 0 ? EffectiveRange : range);
    }
}
