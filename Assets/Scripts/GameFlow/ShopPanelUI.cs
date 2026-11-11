using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class ShopPanelUI : MonoBehaviour
{
    [System.Serializable]
    private sealed class Node
    {
        public int Id;
        public UnityEngine.UI.Image Background;
        public UnityEngine.UI.Outline Border;
        public UnityEngine.UI.Button Button;
        public TMP_Text Name;
        public TMP_Text Status;
        public GameObject Glow;
        public GameObject Connection;
        [TextArea(2, 5)] public string Description = string.Empty;
        [System.NonSerialized] public UnityEngine.UI.Image Icon;
    }

    [System.Serializable]
    private sealed class Style
    {
        public Color Cannon;
        public Color MachineGun;
        public Color Missile;
        public Color Available;
        public Color NormalText;
        public Color Muted;
        public Color Card;
        public Color LockedCard;
        [Range(0, 1)] public float SelectedCardTint;
        [Range(0, 1)] public float SelectedBorderAlpha;
        [Range(0, 1)] public float NormalBorderAlpha;
        [Range(0, 1)] public float LockedBorderAlpha;
        [Range(0, 1)] public float VisibleLineAlpha;
        [Range(0, 1)] public float VisibleGlowAlpha;
        [Range(0, 1)] public float LockedLineAlpha;
        [Range(0, 1)] public float LockedGlowAlpha;
    }

    [System.Serializable]
    private sealed class Copy
    {
        public string ShopTitle = string.Empty;
        public string Back = string.Empty;
        public string Locked = string.Empty;
        public string Max = string.Empty;
        public string CoinPrice = string.Empty;
        public string TowerLockedPrice = string.Empty;
        public string NodeStatus = string.Empty;
        public string SortedEntry = string.Empty;
        public string NextCost = string.Empty;
        public string UpgradeComplete = string.Empty;
        public string LevelProgress = string.Empty;
        public string SlotProgress = string.Empty;
        public string AvailableStatus = string.Empty;
        public string TowerLockedStatus = string.Empty;
        public string PrerequisiteLockedStatus = string.Empty;
        public string InsufficientCoinsStatus = string.Empty;
        public string MaxStatus = string.Empty;
        public string UnavailableStatus = string.Empty;
        public string Buy = string.Empty;
        public string BuyUnavailable = string.Empty;
        public string EmptyDetail = string.Empty;
        public string SelectFeedback = string.Empty;
        public string PurchaseSuccess = string.Empty;
        public string PurchaseSaveFailed = string.Empty;
        public string PurchaseUnavailable = string.Empty;
    }

    [Header("Apariencia de la tienda")]
    [SerializeField] private Style style;
    [Header("Textos de la tienda")]
    [SerializeField] private Copy copy;
    [Header("Referencias del Canvas")]
    [SerializeField] private List<Node> productNodes = new List<Node>();
    [SerializeField] private GameObject overlay;
    [SerializeField] private TMP_Text hudCoins;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text balance;
    [SerializeField] private TMP_Text detailTitle;
    [SerializeField] private TMP_Text continueLabel;
    [SerializeField] private UnityEngine.UI.Button continueButton;

    private readonly Dictionary<int, Node> nodes = new Dictionary<int, Node>();
    private readonly Dictionary<ShopEffectType, Sprite> icons = new Dictionary<ShopEffectType, Sprite>();
    private GameManager gameManager;
    private RectTransform tooltip;
    private TMP_Text tooltipTitle;
    private TMP_Text tooltipProgress;
    private TMP_Text tooltipDescription;
    private TMP_Text tooltipPrice;
    private TMP_Text tooltipStatus;
    private int hoveredId = -1;

    public void Initialize(GameManager manager)
    {
        if (manager == null || style == null || copy == null || overlay == null ||
            hudCoins == null || title == null || balance == null || detailTitle == null || continueLabel == null || continueButton == null)
        {
            Debug.LogError("ShopPanelUI necesita sus referencias del Canvas configuradas.", this);
            return;
        }

        gameManager = manager;
        nodes.Clear();
        foreach (Node node in productNodes)
        {
            if (node == null || node.Background == null || node.Border == null ||
                node.Button == null || node.Name == null || node.Status == null ||
                nodes.ContainsKey(node.Id))
            {
                Debug.LogError("ShopPanelUI tiene un nodo de producto incompleto o duplicado.", this);
                return;
            }

            nodes.Add(node.Id, node);
            ConfigureNode(node);
        }

        BuildTooltip();
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(gameManager.ContinueFromShop);
        Refresh();
    }

    private void Update()
    {
        if (tooltip == null || !tooltip.gameObject.activeSelf || Mouse.current == null)
            return;

        RectTransform overlayRect = (RectTransform)overlay.transform;
        Vector2 mouse = Mouse.current.position.ReadValue();
        Canvas canvas = overlay.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRect, mouse, camera, out Vector2 local))
            return;

        float x = Mathf.Clamp(local.x + 18, overlayRect.rect.xMin + 8,
            overlayRect.rect.xMax - tooltip.rect.width - 8);
        float y = Mathf.Clamp(local.y - 18, overlayRect.rect.yMin + tooltip.rect.height + 8,
            overlayRect.rect.yMax - 8);
        tooltip.anchoredPosition = new Vector2(x, y);
    }

    private void OnDestroy()
    {
        foreach (Sprite icon in icons.Values)
        {
            if (icon == null) continue;
            Destroy(icon.texture);
            Destroy(icon);
        }
    }

    public void Refresh()
    {
        if (gameManager == null || overlay == null)
            return;

        hudCoins.text = gameManager.Coins.ToString();
        bool open = gameManager.State == GameFlowState.Shop;
        overlay.SetActive(open);
        if (!open || gameManager.Shop == null)
        {
            hoveredId = -1;
            if (tooltip != null) tooltip.gameObject.SetActive(false);
            return;
        }

        title.text = copy.ShopTitle;
        balance.text = gameManager.Coins.ToString();
        continueLabel.text = copy.Back;

        // El ABB y Quicksort siguen determinando el catálogo; el árbol muestra sus requisitos.
        foreach (ShopItem item in gameManager.GetShopItems())
        {
            if (!nodes.TryGetValue(item.Id, out Node node))
                continue;

            bool visible = gameManager.Shop.IsVisible(item.Id);
            bool preview = !visible && item.PrerequisiteId >= 0 &&
                gameManager.Shop.IsVisible(item.PrerequisiteId);
            bool shown = visible || preview;
            node.Background.gameObject.SetActive(shown);
            if (node.Glow != null) node.Glow.SetActive(shown);
            if (node.Connection != null) node.Connection.SetActive(shown);
            if (shown) RefreshNode(node, item, preview);
        }

        if (hoveredId >= 0)
            ShowTooltip(hoveredId);
    }

    private void ConfigureNode(Node node)
    {
        RectTransform rect = node.Background.rectTransform;
        rect.sizeDelta = new Vector2(94, 94);
        node.Name.gameObject.SetActive(false);

        RectTransform progress = node.Status.rectTransform;
        progress.anchorMin = progress.anchorMax = new Vector2(0.5f, 0);
        progress.pivot = new Vector2(0.5f, 0);
        progress.anchoredPosition = new Vector2(0, 8);
        progress.sizeDelta = new Vector2(84, 20);
        node.Status.alignment = TextAlignmentOptions.Center;
        node.Status.fontSize = 15;
        node.Status.raycastTarget = false;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        iconObject.transform.SetParent(rect, false);
        RectTransform iconRect = (RectTransform)iconObject.transform;
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(52, 52);
        iconRect.anchoredPosition = new Vector2(0, 10);
        node.Icon = iconObject.GetComponent<UnityEngine.UI.Image>();
        node.Icon.raycastTarget = false;
        node.Icon.preserveAspect = true;

        int id = node.Id;
        node.Button.onClick.RemoveAllListeners();
        node.Button.onClick.AddListener(() => Buy(id));
        EventTrigger trigger = node.Button.GetComponent<EventTrigger>();
        if (trigger == null) trigger = node.Button.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();
        AddPointerEvent(trigger, EventTriggerType.PointerEnter, () => ShowTooltip(id));
        AddPointerEvent(trigger, EventTriggerType.PointerExit, () => HideTooltip(id));
    }

    private static void AddPointerEvent(EventTrigger trigger, EventTriggerType type,
        UnityEngine.Events.UnityAction callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => callback());
        trigger.triggers.Add(entry);
    }

    private void RefreshNode(Node node, ShopItem item, bool preview)
    {
        PurchaseResult result = gameManager.CanBuyShopItem(item.Id);
        Color accent = TowerColor(item.Torre);
        node.Button.interactable = true;
        node.Background.color = preview ? style.LockedCard : style.Card;
        node.Border.effectColor = WithAlpha(preview ? style.Muted : accent,
            item.Id == hoveredId ? style.SelectedBorderAlpha :
            preview ? style.LockedBorderAlpha : style.NormalBorderAlpha);
        node.Icon.sprite = GetIcon(item.Efecto);
        node.Icon.color = preview ? style.Muted : accent;
        node.Status.text = preview ? "BLOQ." :
            item.NivelActual + "/" + item.NivelMaximo;
        node.Status.color = result == PurchaseResult.Success ? style.NormalText : style.Muted;
        SetLineOpacity(node, preview ? style.LockedLineAlpha : style.VisibleLineAlpha,
            preview ? style.LockedGlowAlpha : style.VisibleGlowAlpha);
        PositionConnection(node, item);
    }

    private void PositionConnection(Node node, ShopItem item)
    {
        if (item.PrerequisiteId < 0 || !nodes.TryGetValue(item.PrerequisiteId, out Node parent))
            return;

        Vector2 from = parent.Background.rectTransform.anchoredPosition;
        Vector2 to = node.Background.rectTransform.anchoredPosition;
        Vector2 delta = to - from;
        Vector2 midpoint = (from + to) * 0.5f;
        float length = Mathf.Max(0, delta.magnitude - 94);
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        PositionLine(node.Glow, node.Background.rectTransform.parent, midpoint, length, angle, 11);
        PositionLine(node.Connection, node.Background.rectTransform.parent, midpoint, length, angle, 3);
    }

    private static void PositionLine(GameObject line, Transform board, Vector2 midpoint,
        float length, float angle, float thickness)
    {
        if (line == null) return;
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.SetParent(board, false);
        rect.SetAsFirstSibling();
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = midpoint;
        rect.sizeDelta = new Vector2(length, thickness);
        rect.localRotation = Quaternion.Euler(0, 0, angle);
    }

    private static void SetLineOpacity(Node node, float lineAlpha, float glowAlpha)
    {
        if (node.Connection != null &&
            node.Connection.TryGetComponent(out UnityEngine.UI.Image line))
            line.color = WithAlpha(line.color, lineAlpha);
        if (node.Glow != null &&
            node.Glow.TryGetComponent(out UnityEngine.UI.Image glow))
            glow.color = WithAlpha(glow.color, glowAlpha);
    }

    private void BuildTooltip()
    {
        GameObject panel = new GameObject("UpgradeTooltip", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        panel.transform.SetParent(overlay.transform, false);
        tooltip = (RectTransform)panel.transform;
        tooltip.anchorMin = tooltip.anchorMax = new Vector2(0.5f, 0.5f);
        tooltip.pivot = new Vector2(0, 1);
        tooltip.sizeDelta = new Vector2(390, 214);
        UnityEngine.UI.Image background = panel.GetComponent<UnityEngine.UI.Image>();
        background.color = new Color(0.035f, 0.055f, 0.085f, 0.97f);
        background.raycastTarget = false;
        UnityEngine.UI.Outline outline = panel.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = style.Cannon;
        outline.effectDistance = new Vector2(2, -2);
        tooltipTitle = MakeTooltipText("Name", 22, 12, 30);
        tooltipProgress = MakeTooltipText("Progress", 15, 44, 25);
        tooltipDescription = MakeTooltipText("Description", 16, 74, 74);
        tooltipPrice = MakeTooltipText("Price", 16, 153, 25);
        tooltipStatus = MakeTooltipText("Availability", 15, 183, 25);
        tooltip.gameObject.SetActive(false);
    }

    private TMP_Text MakeTooltipText(string name, float size, float top, float height)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(tooltip, false);
        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.font = detailTitle.font;
        label.fontSize = size;
        label.color = style.NormalText;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.raycastTarget = false;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -top);
        rect.sizeDelta = new Vector2(-28, height);
        return label;
    }

    private void ShowTooltip(int id)
    {
        if (gameManager == null || gameManager.State != GameFlowState.Shop ||
            tooltip == null || !nodes.TryGetValue(id, out Node node) ||
            !node.Background.gameObject.activeInHierarchy)
            return;

        ShopItem item = null;
        foreach (ShopItem candidate in gameManager.GetShopItems())
            if (candidate.Id == id) { item = candidate; break; }
        if (item == null) return;

        hoveredId = id;
        PurchaseResult result = gameManager.CanBuyShopItem(id);
        Color accent = TowerColor(item.Torre);
        tooltipTitle.text = item.Nombre;
        tooltipTitle.color = accent;
        tooltipProgress.text = item.Efecto == ShopEffectType.TowerLevel
            ? copy.LevelProgress.Replace("{level}", gameManager.GetTowerLevel(item.Torre).ToString())
            : item.Efecto == ShopEffectType.TowerSlot
                ? copy.SlotProgress.Replace("{slots}", gameManager.GetTowerSlots(item.Torre).ToString())
                : item.NivelActual + "/" + item.NivelMaximo;
        tooltipDescription.text = node.Description;
        tooltipPrice.text = item.PuedeMejorarse
            ? FormatPrice(copy.NextCost, item.PrecioActual) : copy.UpgradeComplete;
        tooltipPrice.color = accent;
        tooltipStatus.text = StatusText(result);
        tooltipStatus.color = result == PurchaseResult.Success ? style.Available : style.Muted;
        tooltip.gameObject.SetActive(true);
        tooltip.SetAsLastSibling();
        foreach (Node candidate in productNodes)
            if (candidate.Border != null && candidate.Background.gameObject.activeSelf)
                candidate.Border.effectColor = WithAlpha(
                    gameManager.Shop.IsVisible(candidate.Id) ?
                        TowerColor(GetTower(candidate.Id)) : style.Muted,
                    candidate.Id == hoveredId ? style.SelectedBorderAlpha :
                    gameManager.Shop.IsVisible(candidate.Id) ?
                        style.NormalBorderAlpha : style.LockedBorderAlpha);
    }

    private TowerKind GetTower(int id)
    {
        foreach (ShopItem item in gameManager.GetShopItems())
            if (item.Id == id) return item.Torre;
        return TowerKind.Cannon;
    }

    private void HideTooltip(int id)
    {
        if (hoveredId != id) return;
        hoveredId = -1;
        if (tooltip != null) tooltip.gameObject.SetActive(false);
        Refresh();
    }

    private void Buy(int id)
    {
        gameManager.BuyShopItem(id);
        Refresh();
    }

    private string StatusText(PurchaseResult result)
    {
        return result == PurchaseResult.Success ? copy.AvailableStatus :
            result == PurchaseResult.TowerLocked ? copy.TowerLockedStatus :
            result == PurchaseResult.PrerequisiteLocked ? copy.PrerequisiteLockedStatus :
            result == PurchaseResult.InsufficientCoins ? copy.InsufficientCoinsStatus :
            result == PurchaseResult.MaxLevel ? copy.MaxStatus : copy.UnavailableStatus;
    }

    private Sprite GetIcon(ShopEffectType effect)
    {
        if (icons.TryGetValue(effect, out Sprite sprite)) return sprite;
        const int size = 64;
        Color32[] pixels = new Color32[size * size];
        DrawIcon(pixels, effect);
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.SetPixels32(pixels);
        texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        icons.Add(effect, sprite);
        return sprite;
    }

    private static void DrawIcon(Color32[] pixels, ShopEffectType effect)
    {
        switch (effect)
        {
            case ShopEffectType.TowerSlot:
                Line(pixels, 14, 14, 50, 14, 5);
                Line(pixels, 20, 14, 20, 39, 5);
                Line(pixels, 44, 14, 44, 39, 5);
                Line(pixels, 32, 29, 32, 58, 6);
                Line(pixels, 19, 44, 45, 44, 6);
                break;
            case ShopEffectType.UnlockTower:
                Line(pixels, 16, 10, 48, 10, 5);
                Line(pixels, 16, 10, 16, 34, 5);
                Line(pixels, 48, 10, 48, 34, 5);
                Line(pixels, 16, 34, 48, 34, 5);
                Ring(pixels, 32, 39, 13, 4, true);
                Line(pixels, 20, 38, 20, 46, 5);
                break;
            case ShopEffectType.TowerLevel:
                Line(pixels, 32, 10, 32, 48, 7);
                Line(pixels, 32, 49, 14, 32, 7);
                Line(pixels, 32, 49, 50, 32, 7);
                Line(pixels, 15, 13, 49, 13, 5);
                break;
            case ShopEffectType.Damage:
                Line(pixels, 37, 57, 20, 31, 8);
                Line(pixels, 20, 31, 34, 31, 8);
                Line(pixels, 34, 31, 25, 8, 8);
                Line(pixels, 25, 8, 47, 39, 8);
                break;
            case ShopEffectType.Range:
                Ring(pixels, 32, 32, 20, 4, false);
                Ring(pixels, 32, 32, 9, 4, false);
                Line(pixels, 32, 4, 32, 17, 4);
                Line(pixels, 32, 47, 32, 60, 4);
                Line(pixels, 4, 32, 17, 32, 4);
                Line(pixels, 47, 32, 60, 32, 4);
                break;
            case ShopEffectType.AttackSpeed:
                for (int y = 17; y <= 47; y += 15)
                {
                    Line(pixels, 9, y, 48, y, 5);
                    Line(pixels, 48, y, 38, y + 8, 5);
                    Line(pixels, 48, y, 38, y - 8, 5);
                }
                break;
            case ShopEffectType.BlastRadius:
                Ring(pixels, 32, 32, 9, 6, false);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4;
                    Line(pixels, Mathf.RoundToInt(32 + Mathf.Cos(a) * 17),
                        Mathf.RoundToInt(32 + Mathf.Sin(a) * 17),
                        Mathf.RoundToInt(32 + Mathf.Cos(a) * 28),
                        Mathf.RoundToInt(32 + Mathf.Sin(a) * 28), 5);
                }
                break;
        }
    }

    private static void Ring(Color32[] pixels, int cx, int cy, int radius,
        int thickness, bool topOnly)
    {
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                if (Mathf.Abs(d - radius) <= thickness * 0.5f && (!topOnly || y >= cy))
                    pixels[y * 64 + x] = Color.white;
            }
    }

    private static void Line(Color32[] pixels, int x0, int y0, int x1, int y1, int width)
    {
        Vector2 start = new Vector2(x0, y0);
        Vector2 delta = new Vector2(x1 - x0, y1 - y0);
        float lengthSquared = delta.sqrMagnitude;
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                Vector2 point = new Vector2(x, y);
                float t = lengthSquared > 0 ?
                    Mathf.Clamp01(Vector2.Dot(point - start, delta) / lengthSquared) : 0;
                if (Vector2.Distance(point, start + delta * t) <= width * 0.5f)
                    pixels[y * 64 + x] = Color.white;
            }
    }

    private static string FormatPrice(string template, int price) =>
        template.Replace("{price}", price.ToString());

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private Color TowerColor(TowerKind tower) =>
        tower == TowerKind.Cannon ? style.Cannon :
        tower == TowerKind.MG ? style.MachineGun : style.Missile;
}
