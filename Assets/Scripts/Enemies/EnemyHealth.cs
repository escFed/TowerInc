using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 30;

    private bool hasExited;
    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsAlive => !hasExited && CurrentHealth > 0 && isActiveAndEnabled;
    public event Action<EnemyHealth, ExitReason> Exited;

    private void Awake()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive || damage <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        if (CurrentHealth == 0)
            Exit(ExitReason.Killed);
    }

    public void ReachDestination()
    {
        if (IsAlive)
            Exit(ExitReason.ReachedDestination);
    }

    private void Exit(ExitReason reason)
    {
        // Notificar antes de Destroy evita contar dos veces una muerte en el mismo frame.
        NotifyExit(reason);
        Destroy(gameObject);
    }

    private void NotifyExit(ExitReason reason)
    {
        if (hasExited)
            return;

        hasExited = true;
        Exited?.Invoke(this, reason);
    }

    private void OnDestroy()
    {
        NotifyExit(ExitReason.Removed);
    }
}
