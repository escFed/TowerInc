using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    [Header("Gameplay")]
    [SerializeField] private WaveController waveController;
    [SerializeField] private TowerPlacement towerPlacement;
    [SerializeField, Min(1)] private int startingLives = 5;
    [SerializeField, Min(1)] private int totalWaves = 3;
    [SerializeField] private int[] cannonTowersPerWave = { 3, 3, 4 };
    [SerializeField] private int[] machineGunTowersPerWave = { 0, 2, 3 };
    [SerializeField] private int[] enemiesPerWave = { 20, 30, 50 };

    private bool isConfiguringWave;

    public int CurrentWave { get; private set; } = 1;
    public int RemainingLives { get; private set; }
    public int RemainingEnemies => waveController == null
        ? 0
        : waveController.PendingCount + waveController.ActiveCount;
    public int RequiredCannonTowers => GetWaveValue(cannonTowersPerWave);
    public int RequiredMachineGunTowers => GetWaveValue(machineGunTowersPerWave);
    public int EnemiesInCurrentWave => GetWaveValue(enemiesPerWave);
    public bool HasStarted { get; private set; }
    public bool CanBeginWave => State == GameFlowState.Preparing &&
        towerPlacement != null && towerPlacement.HasReachedTowerLimits();
    public GameFlowState State { get; private set; }

    public event Action Changed;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        if (waveController != null)
        {
            waveController.EnemyExited += HandleEnemyExited;
            waveController.WaveCompleted += HandleWaveCompleted;
        }

        if (towerPlacement != null)
            towerPlacement.TowerCountChanged += HandleTowerCountChanged;
    }

    private void Start()
    {
        if (!ValidateConfiguration())
        {
            enabled = false;
            return;
        }

        RemainingLives = Mathf.Max(1, startingLives);
        CurrentWave = 1;
        HasStarted = false;
        PrepareCurrentWave();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        if (waveController != null)
        {
            waveController.EnemyExited -= HandleEnemyExited;
            waveController.WaveCompleted -= HandleWaveCompleted;
        }

        if (towerPlacement != null)
            towerPlacement.TowerCountChanged -= HandleTowerCountChanged;
    }

    public void BeginWave()
    {
        if (!CanBeginWave)
            return;

        if (!waveController.SetEnemyCount(EnemiesInCurrentWave))
        {
            Debug.LogError($"No se pudo configurar la oleada {CurrentWave} con {EnemiesInCurrentWave} enemigos.", this);
            return;
        }

        State = GameFlowState.WaveRunning;
        HasStarted = true;
        towerPlacement.enabled = false;

        if (!waveController.StartWave())
        {
            PrepareCurrentWave();
            return;
        }

        NotifyChanged();
    }

    public void SelectTower(int towerIndex)
    {
        if (State == GameFlowState.Preparing)
            towerPlacement.SelectTower(towerIndex);
    }

    public int GetPlacedTowerCount(int towerIndex)
    {
        return towerPlacement == null ? 0 : towerPlacement.GetPlacedTowerCount(towerIndex);
    }

    public bool CanPlaceTowerType(int towerIndex)
    {
        return State == GameFlowState.Preparing && towerPlacement != null &&
            towerPlacement.CanPlaceTowerType(towerIndex);
    }

    private void PrepareCurrentWave()
    {
        State = GameFlowState.Preparing;
        towerPlacement.enabled = true;

        isConfiguringWave = true;
        int unlockedTypes = RequiredMachineGunTowers > 0 ? 2 : 1;
        towerPlacement.SetUnlockedTowerCount(unlockedTypes);
        towerPlacement.SetTowerLimits(new[] { RequiredCannonTowers, RequiredMachineGunTowers });
        isConfiguringWave = false;

        NotifyChanged();
    }

    private void HandleTowerCountChanged(int towerCount)
    {
        if (!isConfiguringWave)
            NotifyChanged();
    }

    private void HandleEnemyExited(ExitReason reason)
    {
        if (State != GameFlowState.WaveRunning)
            return;

        if (reason == ExitReason.ReachedDestination)
        {
            RemainingLives = Mathf.Max(0, RemainingLives - 1);
            if (RemainingLives == 0)
            {
                ShowDefeat();
                return;
            }
        }

        NotifyChanged();
    }

    private void HandleWaveCompleted()
    {
        if (State != GameFlowState.WaveRunning)
            return;

        if (CurrentWave >= totalWaves)
        {
            ShowVictory();
            return;
        }

        RemainingLives = Mathf.Max(1, startingLives);
        CurrentWave++;
        PrepareCurrentWave();
    }

    private void ShowVictory()
    {
        State = GameFlowState.Victory;
        towerPlacement.enabled = false;
        NotifyChanged();
    }

    private void ShowDefeat()
    {
        State = GameFlowState.Defeat;
        towerPlacement.enabled = false;
        waveController.StopWave();
        NotifyChanged();
    }

    private bool ValidateConfiguration()
    {
        if (waveController != null && towerPlacement != null && IsWaveConfigurationValid())
            return true;

        Debug.LogError("GameManager necesita referencias de oleadas y construccion validas.", this);
        return false;
    }

    private bool IsWaveConfigurationValid()
    {
        if (totalWaves <= 0 || cannonTowersPerWave == null || machineGunTowersPerWave == null ||
            enemiesPerWave == null || cannonTowersPerWave.Length < totalWaves ||
            machineGunTowersPerWave.Length < totalWaves || enemiesPerWave.Length < totalWaves)
        {
            return false;
        }

        for (int waveIndex = 0; waveIndex < totalWaves; waveIndex++)
        {
            if (cannonTowersPerWave[waveIndex] < 0 || machineGunTowersPerWave[waveIndex] < 0 ||
                enemiesPerWave[waveIndex] <= 0)
            {
                return false;
            }
        }

        return true;
    }

    private int GetWaveValue(int[] values)
    {
        int waveIndex = Mathf.Clamp(CurrentWave - 1, 0, values.Length - 1);
        return values[waveIndex];
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
