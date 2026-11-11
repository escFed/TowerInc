using System;

public sealed class LevelDifficultyData
{
    private readonly int[] fixedCounts;
    public int FirstLevelEnemies { get; }
    public int AdditionalEnemiesPerLevel { get; }

    public LevelDifficultyData(int firstLevelEnemies, int additionalEnemiesPerLevel)
    {
        if (firstLevelEnemies < 1)
            throw new ArgumentOutOfRangeException(nameof(firstLevelEnemies));
        if (additionalEnemiesPerLevel < 1)
            throw new ArgumentOutOfRangeException(nameof(additionalEnemiesPerLevel));

        FirstLevelEnemies = firstLevelEnemies;
        AdditionalEnemiesPerLevel = additionalEnemiesPerLevel;
    }

    public LevelDifficultyData(int[] enemiesPerLevel)
    {
        if (enemiesPerLevel == null || enemiesPerLevel.Length == 0)
            throw new ArgumentException("Debe haber al menos un nivel.", nameof(enemiesPerLevel));
        foreach (int count in enemiesPerLevel)
        {
            if (count < 1)
                throw new ArgumentException("Cada nivel debe tener enemigos.", nameof(enemiesPerLevel));
        }

        fixedCounts = (int[])enemiesPerLevel.Clone();
        FirstLevelEnemies = fixedCounts[0];
    }

    public int GetEnemyCount(int level)
    {
        if (level < 1)
            throw new ArgumentOutOfRangeException(nameof(level));

        if (fixedCounts != null)
        {
            if (level > fixedCounts.Length)
                throw new ArgumentOutOfRangeException(nameof(level));
            return fixedCounts[level - 1];
        }

        return checked(FirstLevelEnemies + AdditionalEnemiesPerLevel * (level - 1));
    }

    // Valores iniciales para balancear en Play Mode.
    public static LevelDifficultyData CreateDefault()
    {
        return new LevelDifficultyData(10, 5);
    }
}
