using System;
using System.Collections.Generic;
using UnityEngine;

// Inventario LIFO de torretas: el ultimo elemento agregado es siempre el tope
// y, por lo tanto, la proxima torreta que el jugador puede colocar.
public class TowerStackController : MonoBehaviour
{
    // Referencia a la torreta basica ya existente en la escena. Se clona al colocarla.
    [SerializeField] private TowerController basicTowerPrefab;
    // Cantidad de torretas basicas que se cargan inicialmente en la pila.
    [SerializeField, Min(1)] private int initialBasicTowers = 3;
    // Distancia minima entre torretas para que no se superpongan.
    [SerializeField, Min(0.1f)] private float minimumPlacementDistance = 0.9f;

    // Pila TDA existente en el proyecto. Almacena 0 para representar una torreta basica.
    private readonly IStackTDA availableTowers = new StackTF();
    // Posiciones ya ocupadas por torretas de la escena o colocadas por el jugador.
    private readonly List<Vector2> occupiedPositions = new List<Vector2>();

    // La colocacion actual usa la misma pila enlazada para recordar el tipo de
    // cada torre. Las referencias se mantienen en paralelo porque StackTF solo
    // admite enteros y su contrato no se modifica.
    private readonly IStackTDA placementHistory = new StackTF();
    private readonly List<TowerController> placedTowers = new List<TowerController>();
    private int[] placedTowerCounts = new int[0];

    // Cantidad actual de elementos almacenados en la pila.
    public int AvailableCount { get; private set; }
    // Indica si es seguro consultar o desapilar el tope.
    public bool HasTowers => !availableTowers.PilaVacia();
    public int PlacedTowerCount => placedTowers.Count;
    public bool CanUndo => !placementHistory.PilaVacia() && placedTowers.Count > 0;

    public event Action<int> TowerCountChanged;

    // Agrega torretas al inventario como recompensa de una oleada u otra accion.
    public void AddBasicTowers(int amount = 1)
    {
        if (amount <= 0)
            return;

        for (int i = 0; i < amount; i++)
        {
            availableTowers.Apilar(0); // 0 = torreta basica.
            AvailableCount++;
        }

        Debug.Log($"[PILA] Recompensa: {amount} torreta(s) basica(s) agregada(s). " +
                  $"Elementos: {AvailableCount}.", this);
    }

    private void Awake()
    {
        // Se reutiliza la torreta basica que ya existe en la escena como plantilla.
        if (basicTowerPrefab == null)
            basicTowerPrefab = FindFirstObjectByType<TowerController>();

        // Se reservan las posiciones de las torretas que ya estaban colocadas.
        foreach (TowerController tower in FindObjectsByType<TowerController>(FindObjectsSortMode.None))
            occupiedPositions.Add(tower.transform.position);

        // La pila comienza vacia antes de cargar las torretas iniciales.
        availableTowers.InicializarPila();
        placementHistory.InicializarPila();
        Debug.Log("[PILA] InicializarPila: pila vacia.", this);

        // Apilar tres valores 0 equivale a guardar tres torretas basicas.
        AddBasicTowers(initialBasicTowers);
    }

    // Intenta colocar la torreta que esta en el tope de la pila en una posicion del mapa.
    public bool TryPlaceTopTower(Vector2 position)
    {
        // No se puede desapilar si no hay torretas, falta la plantilla o el sitio esta ocupado.
        if (!HasTowers || basicTowerPrefab == null || !IsPositionFree(position))
        {
            Debug.Log("[PILA] No se coloco una torreta: pila vacia, plantilla faltante o posicion ocupada.", this);
            return false;
        }

        // Se consulta el tope antes de sacarlo. En esta primera version todos
        // los elementos son 0, que identifica a la torreta basica.
        if (availableTowers.Tope() != 0)
            return false;

        Debug.Log($"[PILA] Tope(): {availableTowers.Tope()}. Se colocara una torreta basica.", this);

        // Primero se crea la torreta. Solo si se pudo colocar se la quita de la pila.
        Instantiate(basicTowerPrefab, position, Quaternion.identity);
        availableTowers.Desapilar();
        AvailableCount--;
        occupiedPositions.Add(position);
        Debug.Log($"[PILA] Desapilar(): torreta basica retirada. Elementos restantes: {AvailableCount}." +
                  (HasTowers ? $" Nuevo tope: {availableTowers.Tope()}." : " La pila quedo vacia."), this);
        return true;
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
        occupiedPositions.Add(tower.transform.position);
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

        if (tower != null)
            occupiedPositions.Remove(tower.transform.position);

        TowerCountChanged?.Invoke(PlacedTowerCount);
        return true;
    }

    // Verifica que no haya otra torreta demasiado cerca del punto elegido.
    private bool IsPositionFree(Vector2 position)
    {
        float minimumDistanceSquared = minimumPlacementDistance * minimumPlacementDistance;
        foreach (Vector2 occupiedPosition in occupiedPositions)
        {
            if ((position - occupiedPosition).sqrMagnitude < minimumDistanceSquared)
                return false;
        }
        return true;
    }
}
