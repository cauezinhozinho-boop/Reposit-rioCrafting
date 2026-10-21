using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public GameObject pausePanel;

    private bool isPaused = false;
    public static bool IsPaused { get; private set; } = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (CraftingNpc.IsMenuOpen || CraftingNpc.EscapeConsumedThisFrame)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    void PauseGame()
    {
        isPaused = true;
        IsPaused = true;

        pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    void ResumeGame()
    {
        isPaused = false;
        IsPaused = false;

        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }
}