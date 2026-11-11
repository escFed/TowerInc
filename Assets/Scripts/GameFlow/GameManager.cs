using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] private string progressKey = "TowerDefense.Progress";

    private bool isConfiguringWave;
    private bool lastAttemptWon;
    private LevelDifficultyData levelDifficulty;

    public int CurrentWave { get; private set; } = 1;
    public int RemainingLives { get; private set; }
    public int RemainingEnemies => waveController == null
        ? 0
        : waveController.PendingCount + waveController.ActiveCount;
    public int EnemiesInCurrentWave => levelDifficulty == null ? 0 :
        levelDifficulty.GetEnemyCount(CurrentWave);
    public int Coins => Shop == null ? 0 : Shop.Coins;
    public int TowerSlots => Shop == null ? 1 : Shop.TowerSlots;
    public int GetTowerSlots(TowerKind tower) => Shop == null
        ? (tower == TowerKind.Cannon ? 1 : 0)
        : Shop.GetTowerSlots(tower);
    public bool IsMachineGunUnlocked => Shop != null && Shop.IsTowerUnlocked(TowerKind.MG);
    public bool IsMissileUnlocked => Shop != null && Shop.IsTowerUnlocked(TowerKind.Missile);
    public int GetTowerLevel(TowerKind tower) => Shop == null ? 1 : Shop.GetTowerLevel(tower);
    public Sprite GetTowerPreviewSprite(int towerIndex)
    {
        if (towerPlacement == null)
            return null;
        return towerPlacement.GetTowerPreviewSprite(towerIndex,
            GetTowerLevel((TowerKind)towerIndex));
    }
    public bool LastAttemptWon => lastAttemptWon;
    public bool HasNextWave => CurrentWave < totalWaves;
    public ShopService Shop { get; private set; }
    public bool HasStarted { get; private set; }
    public bool CanBeginWave => State == GameFlowState.Preparing &&
        towerPlacement != null && towerPlacement.PlacedTowerCount > 0;
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
        {
            towerPlacement.TowerCountChanged += HandleTowerCountChanged;
            towerPlacement.TowerPlaced += HandleTowerPlaced;
        }

        if (Shop != null)
            Shop.Changed += NotifyChanged;
    }

    private void Start()
    {
        if (!ValidateConfiguration())
        {
            enabled = false;
            return;
        }

        try
        {
            Shop = new ShopService(ShopCatalog.CrearEjemplo(),
                new PlayerPrefsProgressStore(progressKey));
            Shop.Changed += NotifyChanged;
            levelDifficulty = new LevelDifficultyData(enemiesPerWave);
        }
        catch (Exception error)
        {
            Debug.LogError($"No se pudo cargar el progreso: {error}", this);
            enabled = false;
            return;
        }

        RemainingLives = Mathf.Max(1, startingLives);
        CurrentWave = Mathf.Clamp(Shop.HighestUnlockedLevel, 1, totalWaves);
        HasStarted = false;
        PrepareCurrentWave();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.rKey.wasPressedThisFrame)
            return;

        bool control = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        if (control && shift)
            ResetProgress();
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
        {
            towerPlacement.TowerCountChanged -= HandleTowerCountChanged;
            towerPlacement.TowerPlaced -= HandleTowerPlaced;
        }

        if (Shop != null)
            Shop.Changed -= NotifyChanged;
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

    public ShopItem[] GetShopItems()
    {
        if (Shop == null)
            return Array.Empty<ShopItem>();

        // Mostrar productos con efecto jugable y prefab disponible.
        ShopItem[] sorted = Shop.GetItemsByPrice();
        System.Collections.Generic.List<ShopItem> available =
            new System.Collections.Generic.List<ShopItem>();
        foreach (ShopItem item in sorted)
        {
            if (towerPlacement != null &&
                towerPlacement.HasTowerPrefab((int)item.Torre))
            {
                available.Add(item);
            }
        }

        return available.ToArray();
    }

    public PurchaseResult CanBuyShopItem(int productId)
    {
        if (State != GameFlowState.Shop || Shop == null)
            return PurchaseResult.ShopClosed;
        if (!IsImplementedShopItem(productId))
            return PurchaseResult.InvalidProduct;
        return Shop.CanBuy(productId);
    }

    public PurchaseResult BuyShopItem(int productId)
    {
        PurchaseResult status = CanBuyShopItem(productId);
        if (status != PurchaseResult.Success)
            return status;

        PurchaseResult result = Shop.Buy(productId);
        if (result == PurchaseResult.Success)
        {
            RefreshPlacedTowers();
            NotifyChanged();
        }
        return result;
    }

    // Atajo de desarrollo: Ctrl + Shift + R.
    public bool ResetProgress()
    {
        if (Shop == null)
            return false;

        ShopService fresh;
        try
        {
            IProgressStore store = new PlayerPrefsProgressStore(progressKey);
            store.Save(new ProgressSaveData());
            fresh = new ShopService(ShopCatalog.CrearEjemplo(), store);
        }
        catch (Exception error)
        {
            Debug.LogError($"No se pudo reiniciar el progreso: {error}", this);
            return false;
        }

        waveController.StopWave();
        isConfiguringWave = true;
        towerPlacement.ClearPlacedTowers();
        isConfiguringWave = false;
        Shop.Changed -= NotifyChanged;
        Shop = fresh;
        Shop.Changed += NotifyChanged;
        CurrentWave = 1;
        RemainingLives = Mathf.Max(1, startingLives);
        HasStarted = false;
        lastAttemptWon = false;
        PrepareCurrentWave();
        return true;
    }

    public void ContinueFromShop()
    {
        if (State != GameFlowState.Shop || Shop == null)
            return;

        Shop.CloseShop();
        State = lastAttemptWon ? GameFlowState.Victory : GameFlowState.Defeat;
        NotifyChanged();
    }

    public void OpenShopFromResult()
    {
        if (State != GameFlowState.Victory && State != GameFlowState.Defeat)
            return;

        State = GameFlowState.Shop;
        Shop.OpenShop();
        NotifyChanged();
    }

    public void ContinueFromResult()
    {
        if (State != GameFlowState.Defeat &&
            (State != GameFlowState.Victory || !HasNextWave))
            return;

        isConfiguringWave = true;
        towerPlacement.ClearPlacedTowers();
        isConfiguringWave = false;

        if (State == GameFlowState.Victory)
            CurrentWave++;

        RemainingLives = Mathf.Max(1, startingLives);
        lastAttemptWon = false;
        PrepareCurrentWave();
    }

    private bool IsImplementedShopItem(int productId)
    {
        foreach (ShopItem item in GetShopItems())
        {
            if (item.Id == productId)
                return true;
        }

        return false;
    }

    private void PrepareCurrentWave()
    {
        State = GameFlowState.Preparing;
        isConfiguringWave = true;
        towerPlacement.SetGlobalTowerLimit(int.MaxValue);
        towerPlacement.SetTowerLimits(new[]
        {
            GetTowerSlots(TowerKind.Cannon),
            GetTowerSlots(TowerKind.MG),
            GetTowerSlots(TowerKind.Missile)
        });
        towerPlacement.SetUnlockedTowerTypes(new[]
        {
            true,
            Shop.IsTowerUnlocked(TowerKind.MG),
            Shop.IsTowerUnlocked(TowerKind.Missile)
        });
        isConfiguringWave = false;
        towerPlacement.enabled = true;

        NotifyChanged();
    }

    private void HandleTowerCountChanged(int towerCount)
    {
        if (!isConfiguringWave)
            NotifyChanged();
    }

    private void HandleTowerPlaced(TowerController tower, int towerIndex)
    {
        if (tower == null || Shop == null || towerIndex < 0 || towerIndex > 2)
            return;

        tower.ApplyProgress((TowerKind)towerIndex, Shop);
    }

    private void RefreshPlacedTowers()
    {
        foreach (TowerController tower in FindObjectsByType<TowerController>(FindObjectsSortMode.None))
        {
            if (tower != null && tower.HasAppliedProgress)
                tower.ApplyProgress(tower.Kind, Shop);
        }
    }

    private void HandleEnemyExited(ExitReason reason)
    {
        if (State != GameFlowState.WaveRunning)
            return;

        if (reason == ExitReason.Killed && !Shop.RegisterEnemyKilled())
            Debug.LogError($"No se pudo guardar la moneda de la baja: {Shop.LastSaveError}", this);

        if (reason == ExitReason.ReachedDestination)
        {
            RemainingLives = Mathf.Max(0, RemainingLives - 1);
            if (RemainingLives == 0)
            {
                waveController.StopWave();
                ShowResult(false);
                return;
            }
        }

        NotifyChanged();
    }

    private void HandleWaveCompleted()
    {
        if (State != GameFlowState.WaveRunning)
            return;

        if (Shop.HighestUnlockedLevel <= CurrentWave && !Shop.CompleteLevel(CurrentWave))
        {
            Debug.LogError($"No se pudo guardar el nivel completado: {Shop.LastSaveError}", this);
            ShowResult(false);
            return;
        }

        ShowResult(true);
    }

    private void ShowResult(bool won)
    {
        lastAttemptWon = won;
        State = won ? GameFlowState.Victory : GameFlowState.Defeat;
        towerPlacement.enabled = false;
        NotifyChanged();
    }

    private bool ValidateConfiguration()
    {
        if (waveController != null && towerPlacement != null && IsWaveConfigurationValid() &&
            !string.IsNullOrWhiteSpace(progressKey))
            return true;

        Debug.LogError("GameManager necesita referencias de oleadas y construccion validas.", this);
        return false;
    }

    private bool IsWaveConfigurationValid()
    {
        if (totalWaves <= 0 || enemiesPerWave == null || enemiesPerWave.Length < totalWaves)
        {
            return false;
        }

        for (int waveIndex = 0; waveIndex < totalWaves; waveIndex++)
        {
            if (enemiesPerWave[waveIndex] <= 0)
            {
                return false;
            }
        }

        return true;
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
