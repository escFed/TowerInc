using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private string levelSceneName = "Level1";

    public Button StartButton => startButton;
    public string LevelSceneName => levelSceneName;

    private void Awake()
    {
        if (startButton == null)
            startButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (Application.isPlaying && startButton != null)
            startButton.onClick.AddListener(StartGame);
    }

    private void OnDisable()
    {
        if (Application.isPlaying && startButton != null)
            startButton.onClick.RemoveListener(StartGame);
    }

    public void StartGame()
    {
        if (!Application.CanStreamedLevelBeLoaded(levelSceneName))
        {
            Debug.LogError($"La escena '{levelSceneName}' no esta habilitada en Build Settings.", this);
            return;
        }

        SceneManager.LoadScene(levelSceneName);
    }
}
