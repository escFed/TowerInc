using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(TowerStackController))]
public sealed class TowerPlacement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private TowerController[] towerPrefabs;
    [SerializeField] private Transform towerParent;
    [SerializeField] private TowerStackController towerStackController;

    [Header("Placement")]
    [SerializeField] private Rect placementBounds = new Rect(-6.5f, -4.5f, 13f, 9f);
    [SerializeField] private Rect blockedPath = new Rect(-7f, -0.75f, 14f, 1.5f);
    [SerializeField, Min(0.1f)] private float gridSize = 0.5f;
    [SerializeField, Min(0.1f)] private float minimumTowerSpacing = 1f;

    [Header("Tower editing")]
    [SerializeField, Min(0.1f)] private float towerSelectionRadius = 0.55f;
    [SerializeField] private Color selectedTowerColor = new Color(1f, 0.85f, 0.3f, 1f);

    [Header("Placement area")]
    [SerializeField] private Sprite placementAreaSprite;
    [SerializeField] private Color placementAreaColor = new Color(0.2f, 1f, 0.35f, 0.16f);
    [SerializeField] private int placementAreaSortingOrder = -2;

    private int unlockedTowerCount = int.MaxValue;
    private bool[] unlockedTowerTypes;
    private int globalTowerLimit = int.MaxValue;
    private int[] towerLimits;
    private GameObject placementAreaVisual;
    private TowerController selectedPlacedTower;
    private SpriteRenderer selectedTowerRenderer;
    private Color originalTowerColor;

    public int SelectedTowerIndex { get; private set; }
    public int PlacedTowerCount => towerStackController == null
        ? 0
        : towerStackController.PlacedTowerCount;
    public bool CanUndo => towerStackController != null && towerStackController.CanUndo;
    public TowerController SelectedPlacedTower => selectedPlacedTower;
    public int GlobalTowerLimit => globalTowerLimit;
    public int UnlockedTowerCount
    {
        get
        {
            if (towerPrefabs == null)
                return 0;
            if (unlockedTowerTypes == null)
                return Mathf.Min(unlockedTowerCount, towerPrefabs.Length);

            int count = 0;
            for (int i = 0; i < unlockedTowerTypes.Length && i < towerPrefabs.Length; i++)
            {
                if (unlockedTowerTypes[i] && towerPrefabs[i] != null)
                    count++;
            }
            return count;
        }
    }
    public bool IsPlacementAreaVisible =>
        placementAreaVisual != null && placementAreaVisual.activeSelf;

    public event Action<int> TowerCountChanged;
    public event Action<int> TowerSelectionChanged;
    public event Action<TowerController, int> TowerPlaced;

    private void Awake()
    {
        EnsureReferences();
        towerStackController.ConfigureTowerTypes(towerPrefabs == null ? 0 : towerPrefabs.Length);
        CreatePlacementAreaVisual();
    }

    private void OnEnable()
    {
        EnsureReferences();
        towerStackController.TowerCountChanged -= HandleTowerCountChanged;
        towerStackController.TowerCountChanged += HandleTowerCountChanged;
        SetPlacementAreaVisible(true);
    }

    private void OnDisable()
    {
        ClearPlacedTowerSelection();
        if (towerStackController != null)
            towerStackController.TowerCountChanged -= HandleTowerCountChanged;

        SetPlacementAreaVisible(false);
    }

    private void Update()
    {
        HandleTowerSelectionInput();

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.zKey.wasPressedThisFrame)
            UndoLastPlacement();

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            ClearPlacedTowerSelection();

        if (keyboard != null && (keyboard.deleteKey.wasPressedThisFrame ||
            keyboard.backspaceKey.wasPressedThisFrame) && selectedPlacedTower != null)
            RemovePlacedTower(selectedPlacedTower);

        Mouse mouse = Mouse.current;
        if (mouse == null || (!mouse.leftButton.wasPressedThisFrame &&
            !mouse.rightButton.wasPressedThisFrame))
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (worldCamera == null)
            return;

        Vector3 worldPosition = worldCamera.ScreenToWorldPoint(mouse.position.ReadValue());
        worldPosition.z = 0f;
        TowerController clickedTower = towerStackController.FindPlacedTowerNear(
            worldPosition, towerSelectionRadius);

        if (mouse.rightButton.wasPressedThisFrame)
        {
            if (clickedTower != null)
                RemovePlacedTower(clickedTower);
            return;
        }

        if (clickedTower != null)
        {
            SelectPlacedTower(clickedTower);
            return;
        }

        if (selectedPlacedTower != null)
            TryMovePlacedTower(selectedPlacedTower, worldPosition);
        else
            TryPlaceTower(worldPosition);
    }

    public bool SelectTower(int towerIndex)
    {
        if (!CanPlaceTowerType(towerIndex))
            return false;

        SelectedTowerIndex = towerIndex;
        TowerSelectionChanged?.Invoke(SelectedTowerIndex);
        return true;
    }

    public void SetUnlockedTowerCount(int count)
    {
        unlockedTowerTypes = null;
        unlockedTowerCount = Mathf.Clamp(
            count,
            0,
            towerPrefabs == null ? 0 : towerPrefabs.Length);
        SelectFirstAvailableTower();
    }

    public void SetUnlockedTowerTypes(bool[] unlocked)
    {
        unlockedTowerTypes = unlocked == null ? null : (bool[])unlocked.Clone();
        SelectFirstAvailableTower();
    }

    public void SetTowerLimits(int[] limits)
    {
        EnsureReferences();
        towerStackController.ConfigureTowerTypes(towerPrefabs == null ? 0 : towerPrefabs.Length);
        towerLimits = limits == null ? null : (int[])limits.Clone();
        SelectFirstAvailableTower();
        TowerCountChanged?.Invoke(PlacedTowerCount);
    }

    public void SetGlobalTowerLimit(int limit)
    {
        globalTowerLimit = Mathf.Max(1, limit);
        SelectFirstAvailableTower();
        TowerCountChanged?.Invoke(PlacedTowerCount);
    }

    public void ClearPlacedTowers()
    {
        ClearPlacedTowerSelection();
        if (towerStackController == null)
            return;

        while (towerStackController.TryPopLastPlacement(out TowerController tower, out _))
        {
            if (tower != null)
            {
                tower.gameObject.SetActive(false);
                Destroy(tower.gameObject);
            }
        }

        SelectFirstAvailableTower();
    }

    public int GetPlacedTowerCount(int towerIndex)
    {
        EnsureReferences();
        towerStackController.ConfigureTowerTypes(towerPrefabs == null ? 0 : towerPrefabs.Length);
        return towerStackController.GetPlacedTowerCount(towerIndex);
    }

    public bool HasTowerPrefab(int towerIndex)
    {
        return towerPrefabs != null && towerIndex >= 0 &&
            towerIndex < towerPrefabs.Length && towerPrefabs[towerIndex] != null;
    }

    public Sprite GetTowerPreviewSprite(int towerIndex, int level)
    {
        return HasTowerPrefab(towerIndex)
            ? towerPrefabs[towerIndex].GetLevelSprite(level)
            : null;
    }

    public int GetTowerLimit(int towerIndex)
    {
        if (towerLimits == null || towerIndex < 0 || towerIndex >= towerLimits.Length)
            return int.MaxValue;

        return Mathf.Max(0, towerLimits[towerIndex]);
    }

    public int GetRemainingTowerCount(int towerIndex)
    {
        int limit = GetTowerLimit(towerIndex);
        return limit == int.MaxValue
            ? int.MaxValue
            : Mathf.Max(0, limit - GetPlacedTowerCount(towerIndex));
    }

    public bool CanPlaceTowerType(int towerIndex)
    {
        if (towerPrefabs == null || towerIndex < 0 || !IsTypeUnlocked(towerIndex) ||
            towerIndex >= towerPrefabs.Length || towerPrefabs[towerIndex] == null)
        {
            return false;
        }

        return PlacedTowerCount < globalTowerLimit &&
            GetPlacedTowerCount(towerIndex) < GetTowerLimit(towerIndex);
    }

    public bool HasReachedTowerLimits()
    {
        if (towerLimits == null || towerLimits.Length == 0)
            return false;

        for (int index = 0; index < towerLimits.Length; index++)
        {
            if (GetPlacedTowerCount(index) != Mathf.Max(0, towerLimits[index]))
                return false;
        }

        return true;
    }

    public bool TryPlaceTowerAtScreenPosition(Vector2 screenPosition)
    {
        if (worldCamera == null)
            return false;

        Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0f;
        return TryPlaceTower(worldPosition);
    }

    public bool TryPlaceTower(Vector3 worldPosition)
    {
        EnsureReferences();
        towerStackController.ConfigureTowerTypes(towerPrefabs == null ? 0 : towerPrefabs.Length);
        if (!CanPlaceTowerType(SelectedTowerIndex))
            return false;

        Vector3 placementPosition = SnapToGrid(worldPosition);
        if (!IsValidPlacement(placementPosition))
            return false;

        TowerController tower = Instantiate(
            towerPrefabs[SelectedTowerIndex],
            placementPosition,
            Quaternion.identity,
            towerParent);
        if (towerStackController.RegisterPlacement(tower, SelectedTowerIndex))
        {
            TowerPlaced?.Invoke(tower, SelectedTowerIndex);
            if (!CanPlaceTowerType(SelectedTowerIndex))
                SelectFirstAvailableTower();
            return true;
        }

        Destroy(tower.gameObject);
        return false;
    }

    public bool UndoLastPlacement()
    {
        if (towerStackController == null ||
            !towerStackController.TryPopLastPlacement(out TowerController tower, out _))
        {
            return false;
        }

        if (tower == selectedPlacedTower)
            ClearPlacedTowerSelection();

        if (tower != null)
        {
            tower.gameObject.SetActive(false);
            Destroy(tower.gameObject);
        }

        SelectFirstAvailableTower();
        return true;
    }

    public Vector3 SnapToGrid(Vector3 position)
    {
        float safeGridSize = Mathf.Max(0.1f, gridSize);
        position.x = Mathf.Round(position.x / safeGridSize) * safeGridSize;
        position.y = Mathf.Round(position.y / safeGridSize) * safeGridSize;
        position.z = 0f;
        return position;
    }

    public bool IsValidPlacement(Vector3 position)
    {
        return IsValidPlacement(position, null);
    }

    public bool IsValidPlacement(Vector3 position, TowerController ignoredTower)
    {
        Vector2 point = position;
        if (!placementBounds.Contains(point) || blockedPath.Contains(point))
            return false;

        float minimumDistanceSquared = minimumTowerSpacing * minimumTowerSpacing;
        foreach (TowerController tower in
                 FindObjectsByType<TowerController>(FindObjectsSortMode.None))
        {
            if (tower == null || tower == ignoredTower)
                continue;

            Vector2 difference = tower.transform.position - position;
            if (difference.sqrMagnitude < minimumDistanceSquared)
                return false;
        }

        return true;
    }

    public bool SelectPlacedTower(TowerController tower)
    {
        if (tower == null || towerStackController == null ||
            towerStackController.FindPlacedTowerNear(tower.transform.position, 0.01f) != tower)
            return false;

        ClearPlacedTowerSelection();
        selectedPlacedTower = tower;
        selectedTowerRenderer = tower.GetComponent<SpriteRenderer>();
        if (selectedTowerRenderer != null)
        {
            originalTowerColor = selectedTowerRenderer.color;
            selectedTowerRenderer.color = selectedTowerColor;
        }
        return true;
    }

    public bool TryMovePlacedTower(TowerController tower, Vector3 worldPosition)
    {
        if (tower == null || towerStackController == null ||
            towerStackController.FindPlacedTowerNear(tower.transform.position, 0.01f) != tower)
            return false;

        Vector3 destination = SnapToGrid(worldPosition);
        if (!IsValidPlacement(destination, tower))
            return false;

        tower.transform.position = destination;
        if (tower == selectedPlacedTower)
            ClearPlacedTowerSelection();
        return true;
    }

    public bool RemovePlacedTower(TowerController tower)
    {
        if (towerStackController == null || !towerStackController.TryRemovePlacement(tower))
            return false;

        if (tower == selectedPlacedTower)
            ClearPlacedTowerSelection();
        tower.gameObject.SetActive(false);
        Destroy(tower.gameObject);
        SelectFirstAvailableTower();
        return true;
    }

    public void ClearPlacedTowerSelection()
    {
        if (selectedTowerRenderer != null)
            selectedTowerRenderer.color = originalTowerColor;
        selectedPlacedTower = null;
        selectedTowerRenderer = null;
    }

    private void EnsureReferences()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
        if (towerParent == null)
            towerParent = transform;
        if (towerStackController == null)
            towerStackController = GetComponent<TowerStackController>();
        if (towerStackController == null)
            towerStackController = gameObject.AddComponent<TowerStackController>();
    }

    private void HandleTowerSelectionInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            SelectTower(0);
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            SelectTower(1);
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            SelectTower(2);
    }

    private void HandleTowerCountChanged(int towerCount)
    {
        TowerCountChanged?.Invoke(towerCount);
    }

    private void SelectFirstAvailableTower()
    {
        for (int index = 0; towerPrefabs != null && index < towerPrefabs.Length; index++)
        {
            if (CanPlaceTowerType(index))
            {
                SelectTower(index);
                return;
            }
        }
    }

    private bool IsTypeUnlocked(int towerIndex)
    {
        return unlockedTowerTypes == null
            ? towerIndex < UnlockedTowerCount
            : towerIndex < unlockedTowerTypes.Length && unlockedTowerTypes[towerIndex];
    }

    private void CreatePlacementAreaVisual()
    {
        if (placementAreaSprite == null || placementAreaVisual != null)
            return;

        placementAreaVisual = new GameObject("Area valida de colocacion");
        placementAreaVisual.transform.SetParent(transform, false);

        Rect overlap = Rect.MinMaxRect(
            Mathf.Max(placementBounds.xMin, blockedPath.xMin),
            Mathf.Max(placementBounds.yMin, blockedPath.yMin),
            Mathf.Min(placementBounds.xMax, blockedPath.xMax),
            Mathf.Min(placementBounds.yMax, blockedPath.yMax));

        if (!placementBounds.Overlaps(blockedPath) || overlap.width <= 0f || overlap.height <= 0f)
        {
            AddPlacementArea(placementBounds);
            return;
        }

        AddPlacementArea(Rect.MinMaxRect(
            placementBounds.xMin,
            placementBounds.yMin,
            placementBounds.xMax,
            overlap.yMin));
        AddPlacementArea(Rect.MinMaxRect(
            placementBounds.xMin,
            overlap.yMax,
            placementBounds.xMax,
            placementBounds.yMax));
        AddPlacementArea(Rect.MinMaxRect(
            placementBounds.xMin,
            overlap.yMin,
            overlap.xMin,
            overlap.yMax));
        AddPlacementArea(Rect.MinMaxRect(
            overlap.xMax,
            overlap.yMin,
            placementBounds.xMax,
            overlap.yMax));
    }

    private void AddPlacementArea(Rect area)
    {
        if (area.width <= 0f || area.height <= 0f)
            return;

        GameObject areaObject = new GameObject("Zona valida");
        areaObject.transform.SetParent(placementAreaVisual.transform, false);
        areaObject.transform.localPosition = new Vector3(area.center.x, area.center.y, 0f);
        areaObject.transform.localScale = new Vector3(area.width, area.height, 1f);

        SpriteRenderer renderer = areaObject.AddComponent<SpriteRenderer>();
        renderer.sprite = placementAreaSprite;
        renderer.color = placementAreaColor;
        renderer.sortingOrder = placementAreaSortingOrder;
    }

    private void SetPlacementAreaVisible(bool visible)
    {
        if (placementAreaVisual != null)
            placementAreaVisual.SetActive(visible);
    }

    private void OnDrawGizmosSelected()
    {
        DrawRect(placementBounds, Color.green);
        DrawRect(blockedPath, Color.red);
    }

    private static void DrawRect(Rect rect, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(
            new Vector3(rect.center.x, rect.center.y, 0f),
            new Vector3(rect.size.x, rect.size.y, 0f));
    }
}
