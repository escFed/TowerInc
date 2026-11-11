using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameUI : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    [Header("HUD")]
    [SerializeField] private TMP_Text placementHint;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text enemiesText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button towerOneButton;
    [SerializeField] private Button towerTwoButton;
    [SerializeField] private TMP_Text towerOneCountText;
    [SerializeField] private TMP_Text towerTwoCountText;
    [SerializeField] private Button towerThreeButton;
    [SerializeField] private TMP_Text towerThreeCountText;

    [Header("Results")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button victoryUpgradesButton;
    [SerializeField] private Button defeatUpgradesButton;
    private ShopPanelUI shopPanel;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        if (gameManager != null)
            gameManager.Changed += Refresh;

        if (startButton != null)
            startButton.onClick.AddListener(BeginWave);
        if (towerOneButton != null)
            towerOneButton.onClick.AddListener(SelectTowerOne);
        if (towerTwoButton != null)
            towerTwoButton.onClick.AddListener(SelectTowerTwo);
        if (towerThreeButton != null)
            towerThreeButton.onClick.AddListener(SelectTowerThree);

        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(ContinueResult);
        if (retryButton != null)
            retryButton.onClick.AddListener(ContinueResult);
        if (victoryUpgradesButton != null)
            victoryUpgradesButton.onClick.AddListener(gameManager.OpenShopFromResult);
        if (defeatUpgradesButton != null)
            defeatUpgradesButton.onClick.AddListener(gameManager.OpenShopFromResult);
    }

    private void Start()
    {
        if (!ValidateConfiguration())
        {
            enabled = false;
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            shopPanel = canvas.GetComponent<ShopPanelUI>();
            if (shopPanel == null)
                Debug.LogError("El Canvas necesita ShopPanelUI y ShopOverlay configurados en la escena.", canvas);
            else
                shopPanel.Initialize(gameManager);
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        if (gameManager != null)
            gameManager.Changed -= Refresh;

        if (startButton != null)
            startButton.onClick.RemoveListener(BeginWave);
        if (towerOneButton != null)
            towerOneButton.onClick.RemoveListener(SelectTowerOne);
        if (towerTwoButton != null)
            towerTwoButton.onClick.RemoveListener(SelectTowerTwo);
        if (towerThreeButton != null)
            towerThreeButton.onClick.RemoveListener(SelectTowerThree);

        if (nextLevelButton != null)
            nextLevelButton.onClick.RemoveListener(ContinueResult);
        if (retryButton != null)
            retryButton.onClick.RemoveListener(ContinueResult);
        if (victoryUpgradesButton != null)
            victoryUpgradesButton.onClick.RemoveListener(gameManager.OpenShopFromResult);
        if (defeatUpgradesButton != null)
            defeatUpgradesButton.onClick.RemoveListener(gameManager.OpenShopFromResult);
    }

    private void BeginWave()
    {
        gameManager.BeginWave();
    }

    private void SelectTowerOne()
    {
        gameManager.SelectTower(0);
    }

    private void SelectTowerTwo()
    {
        gameManager.SelectTower(1);
    }

    private void SelectTowerThree()
    {
        gameManager.SelectTower(2);
    }

    private void Refresh()
    {
        GameFlowState state = gameManager.State;
        bool isPreparing = state == GameFlowState.Preparing;

        SetActive(victoryPanel, state == GameFlowState.Victory);
        SetActive(defeatPanel, state == GameFlowState.Defeat);
        if (nextLevelButton != null)
        {
            TMP_Text label = nextLevelButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = gameManager.HasNextWave ? "NextLevel" : "Main Menu";
        }
        SetActive(placementHint, isPreparing);
        SetActive(waveText, true);
        SetActive(enemiesText, gameManager.HasStarted);
        SetActive(startButton, isPreparing);
        SetActive(towerOneButton, isPreparing);
        SetActive(towerTwoButton, isPreparing && gameManager.IsMachineGunUnlocked);
        SetActive(towerThreeButton, isPreparing && gameManager.IsMissileUnlocked);

        waveText.text = $"Level {gameManager.CurrentWave}";
        enemiesText.text = $"Enemies: {gameManager.RemainingEnemies}";
        livesText.text = $"Lives: {gameManager.RemainingLives}";

        int cannonSlots = gameManager.GetTowerSlots(TowerKind.Cannon);
        int mgSlots = gameManager.GetTowerSlots(TowerKind.MG);
        int missileSlots = gameManager.GetTowerSlots(TowerKind.Missile);
        int cannonRemaining = Mathf.Max(0, cannonSlots - gameManager.GetPlacedTowerCount(0));
        int mgRemaining = Mathf.Max(0, mgSlots - gameManager.GetPlacedTowerCount(1));
        int missileRemaining = Mathf.Max(0, missileSlots - gameManager.GetPlacedTowerCount(2));

        towerOneCountText.text = $"{cannonRemaining} / {cannonSlots}";
        towerTwoCountText.text = $"{mgRemaining} / {mgSlots}";
        towerThreeCountText.text = $"{missileRemaining} / {missileSlots}";

        SetTowerSprite(towerOneButton, gameManager.GetTowerPreviewSprite(0));
        SetTowerSprite(towerTwoButton, gameManager.GetTowerPreviewSprite(1));
        SetTowerSprite(towerThreeButton, gameManager.GetTowerPreviewSprite(2));

        startButton.interactable = gameManager.CanBeginWave;
        towerOneButton.interactable = gameManager.CanPlaceTowerType(0);
        towerTwoButton.interactable = gameManager.CanPlaceTowerType(1);
        if (towerThreeButton != null)
            towerThreeButton.interactable = gameManager.CanPlaceTowerType(2);
        if (shopPanel != null)
            shopPanel.Refresh();
    }

    private void ContinueResult()
    {
        if (gameManager.State == GameFlowState.Victory && !gameManager.HasNextWave)
            ReturnToMainMenu();
        else
            gameManager.ContinueFromResult();
    }

    private void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private bool ValidateConfiguration()
    {
        if (gameManager != null && placementHint != null && waveText != null &&
            enemiesText != null && livesText != null && startButton != null &&
            towerOneButton != null && towerTwoButton != null &&
            towerOneCountText != null && towerTwoCountText != null &&
            towerThreeButton != null && towerThreeCountText != null)
        {
            return true;
        }

        Debug.LogError("GameUI necesita referencias al GameManager y al HUD.", this);
        return false;
    }

    private static void SetActive(Component component, bool active)
    {
        if (component != null)
            component.gameObject.SetActive(active);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    private static void SetTowerSprite(Button button, Sprite sprite)
    {
        if (button != null && sprite != null && button.TryGetComponent(out UnityEngine.UI.Image image))
            image.sprite = sprite;
    }
}
