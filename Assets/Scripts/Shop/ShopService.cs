using System;
using System.Collections.Generic;
using System.IO;

public enum PurchaseResult
{
    Success,
    ShopClosed,
    NotFound,
    InvalidProduct,
    TowerLocked,
    PrerequisiteLocked,
    MaxLevel,
    InsufficientCoins,
    InconsistentState,
    SaveFailed
}

public sealed class ShopService
{
    private readonly ShopCatalog catalog;
    private readonly IProgressStore store;
    private ProgressSaveData progress;

    public int Coins => progress.coins;
    public int HighestUnlockedLevel => progress.highestUnlockedLevel;
    public bool IsShopOpen { get; private set; }
    public Exception LastSaveError { get; private set; }
    public event Action Changed;

    public ShopService(ShopCatalog catalog, IProgressStore store)
    {
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        this.store = store ?? throw new ArgumentNullException(nameof(store));

        foreach (ShopItem item in catalog.ObtenerPorId())
        {
            if (item.NivelActual != 0)
                throw new ArgumentException("El catálogo debe construirse sin compras previas.", nameof(catalog));
        }

        ProgressSaveData loaded = store.Load();
        ValidateLoaded(loaded);
        progress = loaded.Clone();

        foreach (ProductPurchaseSaveData purchase in progress.purchases)
        {
            ShopItem item = catalog.Buscar(purchase.id);
            for (int i = 0; i < purchase.count; i++)
                item.SubirNivel();
        }
    }

    public void OpenShop()
    {
        IsShopOpen = true;
        Changed?.Invoke();
    }

    public void CloseShop()
    {
        IsShopOpen = false;
        Changed?.Invoke();
    }

    public ShopItem[] GetItemsByPrice()
    {
        return catalog.ObtenerPorPrecio();
    }

    public bool IsVisible(int productId)
    {
        ShopItem item = catalog.Buscar(productId);
        return item != null && (item.PrerequisiteId < 0 ||
            progress.GetPurchaseCount(item.PrerequisiteId) > 0 ||
            progress.GetPurchaseCount(productId) > 0);
    }

    public ProgressSaveData GetProgressSnapshot()
    {
        return progress.Clone();
    }

    public int GetPurchaseCount(int productId)
    {
        return progress.GetPurchaseCount(productId);
    }

    public int TowerSlots
    {
        get
        {
            int slots = 0;
            foreach (TowerKind tower in (TowerKind[])Enum.GetValues(typeof(TowerKind)))
                slots += GetTowerSlots(tower);
            return slots;
        }
    }

    public int GetTowerSlots(TowerKind tower)
    {
        int slots = tower == TowerKind.Cannon || IsTowerUnlocked(tower) ? 1 : 0;
        foreach (ShopItem item in catalog.ObtenerPorId())
        {
            if (item.Efecto == ShopEffectType.TowerSlot && item.Torre == tower)
                slots += progress.GetPurchaseCount(item.Id);
        }
        return slots;
    }

    public bool IsTowerUnlocked(TowerKind tower)
    {
        return IsTowerUnlocked(progress, tower);
    }

    public int GetTowerLevel(TowerKind tower)
    {
        int level = 1;
        foreach (ShopItem item in catalog.ObtenerPorId())
        {
            if (item.Efecto == ShopEffectType.TowerLevel && item.Torre == tower)
                level += progress.GetPurchaseCount(item.Id);
        }

        return level;
    }

    public int GetStatUpgradeCount(TowerKind tower, ShopEffectType effect)
    {
        int count = 0;
        foreach (ShopItem item in catalog.ObtenerPorId())
        {
            if (item.Efecto == effect && item.Torre == tower)
                count += progress.GetPurchaseCount(item.Id);
        }

        return count;
    }

    public PurchaseResult Buy(int productId)
    {
        PurchaseResult status = CanBuy(productId);
        if (status != PurchaseResult.Success)
            return status;

        ShopItem item = catalog.Buscar(productId);
        int price = item.PrecioActual;
        ProgressSaveData next = progress.Clone();
        next.coins -= price;
        next.SetPurchaseCount(productId, item.NivelActual + 1);
        if (!TrySave(next))
            return PurchaseResult.SaveFailed;

        progress = next;
        item.SubirNivel();
        Changed?.Invoke();
        return PurchaseResult.Success;
    }

    public PurchaseResult CanBuy(int productId)
    {
        if (!IsShopOpen)
            return PurchaseResult.ShopClosed;

        ShopItem item = catalog.Buscar(productId);
        if (item == null)
            return PurchaseResult.NotFound;
        if (item.Efecto == ShopEffectType.None)
            return PurchaseResult.InvalidProduct;
        if (item.Efecto != ShopEffectType.UnlockTower &&
            !IsTowerUnlocked(item.Torre))
        {
            return PurchaseResult.TowerLocked;
        }
        if (!IsVisible(productId))
            return PurchaseResult.PrerequisiteLocked;
        if (!item.PuedeMejorarse)
            return PurchaseResult.MaxLevel;
        if (progress.GetPurchaseCount(productId) != item.NivelActual)
            return PurchaseResult.InconsistentState;

        if (progress.coins < item.PrecioActual)
            return PurchaseResult.InsufficientCoins;
        return PurchaseResult.Success;
    }

    // GameManager llamará este método solo al recibir ExitReason.Killed.
    public bool RegisterEnemyKilled()
    {
        if (progress.coins == int.MaxValue)
            return false;

        ProgressSaveData next = progress.Clone();
        next.coins++;
        if (!TrySave(next))
            return false;

        progress = next;
        Changed?.Invoke();
        return true;
    }

    public bool CompleteLevel(int level)
    {
        if (level != progress.highestUnlockedLevel || level == int.MaxValue)
            return false;

        ProgressSaveData next = progress.Clone();
        next.highestUnlockedLevel = level + 1;
        if (!TrySave(next))
            return false;

        progress = next;
        Changed?.Invoke();
        return true;
    }

    private bool IsTowerUnlocked(ProgressSaveData state, TowerKind tower)
    {
        if (tower == TowerKind.Cannon)
            return true;

        foreach (ShopItem item in catalog.ObtenerPorId())
        {
            if (item.Efecto == ShopEffectType.UnlockTower && item.Torre == tower &&
                state.GetPurchaseCount(item.Id) > 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool TrySave(ProgressSaveData next)
    {
        try
        {
            store.Save(next);
            LastSaveError = null;
            return true;
        }
        catch (Exception error)
        {
            LastSaveError = error;
            return false;
        }
    }

    private void ValidateLoaded(ProgressSaveData loaded)
    {
        if (loaded == null || loaded.version != ProgressSaveData.CurrentVersion ||
            loaded.coins < 0 || loaded.highestUnlockedLevel < 1 || loaded.purchases == null)
        {
            throw new InvalidDataException("El progreso guardado tiene datos inválidos o una versión no compatible.");
        }

        HashSet<int> seen = new HashSet<int>();
        foreach (ProductPurchaseSaveData purchase in loaded.purchases)
        {
            ShopItem item = purchase == null ? null : catalog.Buscar(purchase.id);
            if (item == null || item.Efecto == ShopEffectType.None ||
                purchase.count < 1 || purchase.count > item.NivelMaximo ||
                !seen.Add(purchase.id))
            {
                throw new InvalidDataException("El progreso guardado contiene una compra inválida.");
            }
        }

        foreach (ProductPurchaseSaveData purchase in loaded.purchases)
        {
            ShopItem item = catalog.Buscar(purchase.id);
            if (item.Efecto != ShopEffectType.UnlockTower &&
                !IsTowerUnlocked(loaded, item.Torre))
            {
                throw new InvalidDataException("El progreso guardado contiene una mejora de una torre bloqueada.");
            }
        }
    }
}
