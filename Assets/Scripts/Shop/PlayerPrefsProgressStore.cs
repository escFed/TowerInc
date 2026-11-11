using System;
using System.IO;
using UnityEngine;

public sealed class PlayerPrefsProgressStore : IProgressStore
{
    private const string DefaultKey = "TowerDefense.Progress";
    private readonly string key;

    public PlayerPrefsProgressStore(string key = DefaultKey)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("La clave del guardado no puede estar vacía.", nameof(key));
        this.key = key;
    }

    public ProgressSaveData Load()
    {
        if (!PlayerPrefs.HasKey(key))
            return new ProgressSaveData();

        string json = PlayerPrefs.GetString(key);
        try
        {
            ProgressSaveData progress = JsonUtility.FromJson<ProgressSaveData>(json);
            if (progress == null)
                throw new InvalidDataException("El progreso guardado está vacío.");
            return progress;
        }
        catch (Exception error)
        {
            throw new InvalidDataException("No se pudo leer el progreso guardado.", error);
        }
    }

    public void Save(ProgressSaveData progress)
    {
        if (progress == null)
            throw new ArgumentNullException(nameof(progress));

        PlayerPrefs.SetString(key, JsonUtility.ToJson(progress));
        PlayerPrefs.Save();
    }
}
