using System;
using System.Collections.Generic;
using UnityEngine;

public class WaveController : MonoBehaviour
{
    [Tooltip("El indice del prefab es el entero almacenado en QueueTF: 0 = basico.")]
    [SerializeField] private EnemyMovement[] enemyPrefabs;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform destination;
    [SerializeField] private int[] enemySequence = { 0, 0, 0, 0, 0 };
    [SerializeField] private float spawnInterval = 1.2f;
    [Tooltip("Aumento porcentual de velocidad por cada oleada ya completada. 0.15 = 15%.")]
    [SerializeField, Min(0f)] private float speedIncreasePerWave = 0.15f;
    [SerializeField] private bool startAutomatically;

    private IQueueTDA pendingEnemies = new QueueTF();
    private HashSet<EnemyHealth> activeEnemies = new HashSet<EnemyHealth>();
    private float spawnTimer;

    public bool IsRunning { get; private set; }
    public int PendingCount { get; private set; }
    public int ActiveCount => activeEnemies.Count;
    public int KilledCount { get; private set; }
    public int EscapedCount { get; private set; }
    public int CompletedWaves { get; private set; }
    public event Action WaveCompleted;

    private void Awake()
    {
        pendingEnemies.InicializarCola();
    }

    private void Start()
    {
        if (startAutomatically)
            StartWave();
    }

    public bool StartWave()
    {
        if (!isActiveAndEnabled || IsRunning)
            return false;

        if (!ValidateConfiguration())
            return false;

        pendingEnemies.InicializarCola();
        foreach (int enemyType in enemySequence)
            pendingEnemies.Acolar(enemyType);

        PendingCount = enemySequence.Length;
        KilledCount = 0;
        EscapedCount = 0;
        spawnTimer = 0;
        IsRunning = true;
        return true;
    }

    private void Update()
    {
        if (!IsRunning || pendingEnemies.ColaVacia())
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0)
            return;

        // Leer primero, crear ese tipo y quitarlo de la cola: orden FIFO.
        int enemyType = pendingEnemies.Primero();
        EnemyMovement enemy = Instantiate(enemyPrefabs[enemyType], spawnPoint.position, Quaternion.identity);
        float speedMultiplier = 1f + CompletedWaves * speedIncreasePerWave;
        enemy.SetSpeedMultiplier(speedMultiplier);
        enemy.SetDestination(destination);
        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        activeEnemies.Add(health);
        health.Exited += HandleEnemyExit;
        pendingEnemies.Desacolar();
        PendingCount--;
        spawnTimer = Mathf.Max(0.05f, spawnInterval);
    }

    private void HandleEnemyExit(EnemyHealth enemy, ExitReason reason)
    {
        enemy.Exited -= HandleEnemyExit;
        if (!activeEnemies.Remove(enemy))
            return;

        if (reason == ExitReason.Killed)
            KilledCount++;
        else if (reason == ExitReason.ReachedDestination)
            EscapedCount++;

        // Cola vacia no implica oleada terminada: pueden quedar enemigos en el mapa.
        if (IsRunning && pendingEnemies.ColaVacia() && activeEnemies.Count == 0)
        {
            IsRunning = false;
            CompletedWaves++;
            WaveCompleted?.Invoke();
        }
    }

    private bool ValidateConfiguration()
    {
        if (spawnPoint == null || destination == null || enemyPrefabs == null ||
            enemySequence == null || enemySequence.Length == 0)
        {
            Debug.LogError("WaveController necesita entrada, destino, prefabs y una secuencia no vacia.", this);
            return false;
        }

        foreach (int enemyType in enemySequence)
        {
            if (enemyType < 0 || enemyType >= enemyPrefabs.Length ||
                enemyPrefabs[enemyType] == null || !enemyPrefabs[enemyType].gameObject.activeSelf)
            {
                Debug.LogError($"WaveController: tipo de enemigo invalido o prefab inactivo ({enemyType}).", this);
                return false;
            }
        }
        return true;
    }

    private void OnDestroy()
    {
        foreach (EnemyHealth enemy in activeEnemies)
        {
            if (enemy == null)
                continue;
            enemy.Exited -= HandleEnemyExit;
            Destroy(enemy.gameObject);
        }
        activeEnemies.Clear();
    }
}
