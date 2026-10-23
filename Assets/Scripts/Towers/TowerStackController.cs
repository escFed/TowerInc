using System;
using System.Collections.Generic;
using UnityEngine;

// Historial LIFO de construcciones: el ultimo elemento agregado es siempre el
// tope y, por lo tanto, la proxima torre que el jugador puede deshacer.
public class TowerStackController : MonoBehaviour
{
    // Pila enlazada que guarda los tipos de torre en el mismo orden en el que
    // fueron colocadas. Por ser LIFO, el tope es siempre la ultima torre que
    // el jugador construyo y, por lo tanto, la que se deshace con la tecla Z.
    private readonly IStackTDA placementHistory = new StackTF();

    // Las referencias visuales se guardan en paralelo porque StackTF almacena
    // enteros. El ultimo indice corresponde al tipo que esta en el tope.
    private readonly List<TowerController> placedTowers = new List<TowerController>();
    private int[] placedTowerCounts = new int[0];

    public int PlacedTowerCount => placedTowers.Count;
    public bool CanUndo => !placementHistory.PilaVacia() && placedTowers.Count > 0;

    public event Action<int> TowerCountChanged;

    private void Awake()
    {
        placementHistory.InicializarPila();
    }

    // Adapta el controlador anterior al sistema de colocacion actual sin
    // cambiar la implementacion ni el comportamiento LIFO de StackTF.
    public void ConfigureTowerTypes(int towerTypeCount)
    {
        int safeCount = Mathf.Max(0, towerTypeCount);
        if (placedTowerCounts.Length == safeCount)
            return;

        int[] resizedCounts = new int[safeCount];
        int copiedCount = Mathf.Min(placedTowerCounts.Length, resizedCounts.Length);
        for (int index = 0; index < copiedCount; index++)
            resizedCounts[index] = placedTowerCounts[index];

        placedTowerCounts = resizedCounts;
    }

    public int GetPlacedTowerCount(int towerType)
    {
        if (towerType < 0 || towerType >= placedTowerCounts.Length)
            return 0;

        return placedTowerCounts[towerType];
    }

    public bool RegisterPlacement(TowerController tower, int towerType)
    {
        if (tower == null || towerType < 0 || towerType >= placedTowerCounts.Length)
            return false;

        placementHistory.Apilar(towerType);
        placedTowers.Add(tower);
        placedTowerCounts[towerType]++;
        TowerCountChanged?.Invoke(PlacedTowerCount);
        return true;
    }

    public bool TryPopLastPlacement(out TowerController tower, out int towerType)
    {
        tower = null;
        towerType = -1;
        if (!CanUndo)
            return false;

        towerType = placementHistory.Tope();
        placementHistory.Desapilar();

        int lastIndex = placedTowers.Count - 1;
        tower = placedTowers[lastIndex];
        placedTowers.RemoveAt(lastIndex);

        if (towerType >= 0 && towerType < placedTowerCounts.Length)
            placedTowerCounts[towerType] = Mathf.Max(0, placedTowerCounts[towerType] - 1);

        TowerCountChanged?.Invoke(PlacedTowerCount);
        return true;
    }
}
