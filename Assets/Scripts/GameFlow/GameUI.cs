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

    [Header("Results")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private Button[] playAgainButtons;
    [SerializeField] private Button[] mainMenuButtons;

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

        AddButtonListeners(playAgainButtons, RestartLevel);
        AddButtonListeners(mainMenuButtons, ReturnToMainMenu);
    }

    private void Start()
    {
        if (!ValidateConfiguration())
        {
            enabled = false;
            return;
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

        RemoveButtonListeners(playAgainButtons, RestartLevel);
        RemoveButtonListeners(mainMenuButtons, ReturnToMainMenu);
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

    private void Refresh()
    {
        GameFlowState state = gameManager.State;
        bool isPreparing = state == GameFlowState.Preparing;

        SetActive(victoryPanel, state == GameFlowState.Victory);
        SetActive(defeatPanel, state == GameFlowState.Defeat);
        SetActive(placementHint, isPreparing && !gameManager.HasStarted);
        SetActive(waveText, gameManager.HasStarted);
        SetActive(enemiesText, gameManager.HasStarted);
        SetActive(startButton, gameManager.CanBeginWave);
        SetActive(towerOneButton, isPreparing && gameManager.RequiredCannonTowers > 0);
        SetActive(towerTwoButton, isPreparing && gameManager.RequiredMachineGunTowers > 0);

        waveText.text = $"Oleada {gameManager.CurrentWave}";
        enemiesText.text = $"Enemigos: {gameManager.RemainingEnemies}";
        livesText.text = $"Vida: {gameManager.RemainingLives}";

        int remainingCannon = Mathf.Max(0, gameManager.RequiredCannonTowers - gameManager.GetPlacedTowerCount(0));
        int remainingMachineGun = Mathf.Max(0, gameManager.RequiredMachineGunTowers - gameManager.GetPlacedTowerCount(1));
        towerOneCountText.text = $"{remainingCannon} / {gameManager.RequiredCannonTowers}";
        towerTwoCountText.text = $"{remainingMachineGun} / {gameManager.RequiredMachineGunTowers}";

        startButton.interactable = gameManager.CanBeginWave;
        towerOneButton.interactable = gameManager.CanPlaceTowerType(0);
        towerTwoButton.interactable = gameManager.CanPlaceTowerType(1);
    }

    private void RestartLevel()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.buildIndex >= 0)
            SceneManager.LoadScene(currentScene.buildIndex);
        else
            SceneManager.LoadScene(currentScene.name);
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
            towerOneCountText != null && towerTwoCountText != null)
        {
            return true;
        }

        Debug.LogError("GameUI necesita referencias al GameManager y al HUD.", this);
        return false;
    }

    private static void AddButtonListeners(
        UnityEngine.UI.Button[] buttons,
        UnityEngine.Events.UnityAction action)
    {
        if (buttons == null)
            return;

        foreach (UnityEngine.UI.Button button in buttons)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }
    }

    private static void RemoveButtonListeners(
        UnityEngine.UI.Button[] buttons,
        UnityEngine.Events.UnityAction action)
    {
        if (buttons == null)
            return;

        foreach (UnityEngine.UI.Button button in buttons)
        {
            if (button != null)
                button.onClick.RemoveListener(action);
        }
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
}
