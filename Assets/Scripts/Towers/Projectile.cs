using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 9f;
    [SerializeField] private float hitDistance = 0.15f;
    [SerializeField] private float lifetime = 5f;

    private EnemyHealth target;
    private int damage;
    private float remainingLifetime;

    public void Initialize(EnemyHealth enemy, int attackDamage)
    {
        target = enemy;
        damage = Mathf.Max(0, attackDamage);
        remainingLifetime = lifetime;
    }

    private void Update()
    {
        remainingLifetime -= Time.deltaTime;
        if (target == null || !target.IsAlive || remainingLifetime <= 0)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 destination = target.transform.position;
        destination.z = transform.position.z;
        transform.position = Vector3.MoveTowards(transform.position, destination, Mathf.Max(0.1f, speed) * Time.deltaTime);

        // MoveTowards no sobrepasa el objetivo, aun con proyectiles rapidos.
        if ((transform.position - destination).sqrMagnitude <= hitDistance * hitDistance)
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
