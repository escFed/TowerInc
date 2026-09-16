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

    [Header("Placement area")]
    [SerializeField] private Sprite placementAreaSprite;
    [SerializeField] private Color placementAreaColor = new Color(0.2f, 1f, 0.35f, 0.16f);
    [SerializeField] private int placementAreaSortingOrder = -2;

    private int unlockedTowerCount = int.MaxValue;
    private int[] towerLimits;
    private GameObject placementAreaVisual;

    public int SelectedTowerIndex { get; private set; }
    public int PlacedTowerCount => towerStackController == null
        ? 0
        : towerStackController.PlacedTowerCount;
    public bool CanUndo => towerStackController != null && towerStackController.CanUndo;
    public int UnlockedTowerCount => Mathf.Min(
        unlockedTowerCount,
        towerPrefabs == null ? 0 : towerPrefabs.Length);
    public bool IsPlacementAreaVisible =>
        placementAreaVisual != null && placementAreaVisual.activeSelf;

    public event Action<int> TowerCountChanged;
    public event Action<int> TowerSelectionChanged;

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

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        TryPlaceTowerAtScreenPosition(mouse.position.ReadValue());
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
        unlockedTowerCount = Mathf.Clamp(
            count,
            0,
            towerPrefabs == null ? 0 : towerPrefabs.Length);
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

    public int GetPlacedTowerCount(int towerIndex)
    {
        EnsureReferences();
        towerStackController.ConfigureTowerTypes(towerPrefabs == null ? 0 : towerPrefabs.Length);
        return towerStackController.GetPlacedTowerCount(towerIndex);
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
        if (towerPrefabs == null || towerIndex < 0 || towerIndex >= UnlockedTowerCount ||
            towerIndex >= towerPrefabs.Length || towerPrefabs[towerIndex] == null)
        {
            return false;
        }

        return GetPlacedTowerCount(towerIndex) < GetTowerLimit(towerIndex);
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

        if (tower != null)
            Destroy(tower.gameObject);

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
        Vector2 point = position;
        if (!placementBounds.Contains(point) || blockedPath.Contains(point))
            return false;

        float minimumDistanceSquared = minimumTowerSpacing * minimumTowerSpacing;
        foreach (TowerController tower in
                 FindObjectsByType<TowerController>(FindObjectsSortMode.None))
        {
            if (tower == null)
                continue;

            Vector2 difference = tower.transform.position - position;
            if (difference.sqrMagnitude < minimumDistanceSquared)
                return false;
        }

        return true;
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
    }

    private void HandleTowerCountChanged(int towerCount)
    {
        TowerCountChanged?.Invoke(towerCount);
    }

    private void SelectFirstAvailableTower()
    {
        for (int index = 0; index < UnlockedTowerCount; index++)
        {
            if (CanPlaceTowerType(index))
            {
                SelectTower(index);
                return;
            }
        }
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
