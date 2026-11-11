using System;
using System.Collections.Generic;

[Serializable]
public sealed class ProductPurchaseSaveData
{
    public int id;
    public int count;
}

[Serializable]
public sealed class ProgressSaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public int coins;
    public int highestUnlockedLevel = 1;
    public List<ProductPurchaseSaveData> purchases = new List<ProductPurchaseSaveData>();

    public int GetPurchaseCount(int id)
    {
        foreach (ProductPurchaseSaveData purchase in purchases)
        {
            if (purchase.id == id)
                return purchase.count;
        }

        return 0;
    }

    public void SetPurchaseCount(int id, int count)
    {
        foreach (ProductPurchaseSaveData purchase in purchases)
        {
            if (purchase.id != id)
                continue;

            purchase.count = count;
            return;
        }

        purchases.Add(new ProductPurchaseSaveData { id = id, count = count });
    }

    public ProgressSaveData Clone()
    {
        ProgressSaveData copy = new ProgressSaveData
        {
            version = version,
            coins = coins,
            highestUnlockedLevel = highestUnlockedLevel
        };

        foreach (ProductPurchaseSaveData purchase in purchases)
        {
            copy.purchases.Add(new ProductPurchaseSaveData
            {
                id = purchase.id,
                count = purchase.count
            });
        }

        return copy;
    }
}
