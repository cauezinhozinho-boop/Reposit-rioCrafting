using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;
    public static bool IsGameOver = false;

    [Header("UI")]
    public GameObject gameOverPanel;
    public GameObject hudCanvas;
    public TMP_Text timerText;

    [Header("Animação de Transição")]
    [Tooltip("Tempo em segundos para os elementos de Game Over surgirem suavemente")]
    public float fadeInDuration = 0.5f;

    [Header("Texto do Cronômetro")]
    [Tooltip("Texto antes do número de segundos")]
    public string timerPrefix = "Reiniciando em ";

    [Tooltip("Texto depois do número de segundos")]
    public string timerSuffix = "s...";

    [Header("Configuração de Restart")]
    public float restartDelay = 5f;

    [Tooltip("Se ligado, reinicia a cena sozinho após 'restartDelay' segundos. " +
             "Se desligado, só reinicia quando o jogador clicar no botão.")]
    public bool autoRestart = true;

    [Header("Voltar ao Menu")]
    [Tooltip("Nome da cena do menu principal (precisa estar nas Build Settings).")]
    public string menuSceneName = "MainMenu";

    private Coroutine gameOverCoroutine;
    private Coroutine fadeInCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        ResetState();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetState();
        FindUIReferences();
    }

    public void ResetState()
    {
        IsGameOver = false;
        Time.timeScale = 1f;

        if (gameOverCoroutine != null)
        {
            StopCoroutine(gameOverCoroutine);
            gameOverCoroutine = null;
        }

        if (fadeInCoroutine != null)
        {
            StopCoroutine(fadeInCoroutine);
            fadeInCoroutine = null;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (hudCanvas != null)
        {
            hudCanvas.SetActive(true);
        }

        if (timerText != null)
        {
            timerText.text = "";
        }
    }

    void FindUIReferences()
    {
        if (gameOverPanel == null)
        {
            GameObject panel = GameObject.Find("GameOverPanel");
            if (panel == null)
            {
                panel = GameObject.Find("GameOverScreen");
            }

            if (panel == null)
            {
                Canvas canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform t = canvas.transform.Find("GameOverPanel");
                    if (t == null) t = canvas.transform.Find("GameOverScreen");
                    if (t != null) panel = t.gameObject;
                }
            }

            if (panel != null)
            {
                gameOverPanel = panel;
            }
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);

            if (timerText == null)
            {
                timerText = gameOverPanel.GetComponentInChildren<TMP_Text>(true);
            }
        }

        if (hudCanvas == null)
        {
            hudCanvas = GameObject.Find("HUDCanvas");
        }
    }

    public void ShowGameOver(bool enableAutoRestart = true)
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        this.autoRestart = enableAutoRestart;

        // =====================================================
        // FIX 1: destrava o hover dos botões
        // =====================================================
        // Sem isso, se o jogador clicou em "New Game" no menu,
        // o lock do BrushButton continua ativo e nenhum botão
        // do Game Over destaca no hover.
        BrushButton.SelectionLocked = false;

        if (gameOverPanel == null || hudCanvas == null)
        {
            FindUIReferences();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();

            // =================================================
            // FIX 2: garante que o painel desenha POR CIMA do
            // JumpscarePlaceholder (que está no HUDCanvas).
            // =================================================
            // FadeCanvas e HUDCanvas são canvases raiz separados.
            // Como HUDCanvas vem depois na Hierarchy, o jumpscare
            // desenhava na frente e escondia o fade-in do painel.
            // Dando um Canvas próprio com overrideSorting ao painel,
            // ele passa a ficar na frente (mas ainda atrás do
            // FadeImage, que sobe pra 999 quando vamos pro menu).
            Canvas panelCanvas = gameOverPanel.GetComponent<Canvas>();
            if (panelCanvas == null)
                panelCanvas = gameOverPanel.AddComponent<Canvas>();

            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 500;

            if (gameOverPanel.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                gameOverPanel.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            CanvasGroup cg = gameOverPanel.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = gameOverPanel.AddComponent<CanvasGroup>();
            }

            if (fadeInCoroutine != null)
                StopCoroutine(fadeInCoroutine);

            fadeInCoroutine = StartCoroutine(FadeInGameOver(cg));
        }

        if (gameOverCoroutine != null)
        {
            StopCoroutine(gameOverCoroutine);
            gameOverCoroutine = null;
        }

        if (autoRestart)
        {
            gameOverCoroutine = StartCoroutine(GameOverSequence());
        }
        else if (timerText != null)
        {
            timerText.text = "";
        }
    }

    IEnumerator FadeInGameOver(CanvasGroup cg)
    {
        cg.alpha = 0f;
        float timer = 0f;

        while (timer < fadeInDuration)
        {
            timer += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(timer / fadeInDuration);
            yield return null;
        }

        cg.alpha = 1f;

        if (hudCanvas != null)
        {
            hudCanvas.SetActive(false);
        }
    }

    IEnumerator GameOverSequence()
    {
        float timer = restartDelay;

        while (timer > 0f)
        {
            if (timerText != null)
            {
                timerText.text = timerPrefix + Mathf.CeilToInt(timer) + timerSuffix;
            }

            yield return new WaitForSecondsRealtime(1f);
            timer -= 1f;
        }

        RestartGame();
    }

    public void RestartGame()
    {
        if (gameOverCoroutine != null)
        {
            StopCoroutine(gameOverCoroutine);
            gameOverCoroutine = null;
        }

        HidingSpot.ResetHidingState();
        PlayerHunger.ResetSavedHunger();
        PlayerInventory.ResetAll();

        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    public void ReturnToMenu()
    {
        if (gameOverCoroutine != null)
        {
            StopCoroutine(gameOverCoroutine);
            gameOverCoroutine = null;
        }

        StartCoroutine(ReturnToMenuRoutine());
    }

    IEnumerator ReturnToMenuRoutine()
    {
        HidingSpot.ResetHidingState();
        Time.timeScale = 1f;

        if (FadeManager.Instance != null &&
            FadeManager.Instance.fadeImage != null)
        {
            GameObject fadeGO = FadeManager.Instance.fadeImage.gameObject;

            Canvas fadeCanvas = fadeGO.GetComponent<Canvas>();
            if (fadeCanvas == null)
                fadeCanvas = fadeGO.AddComponent<Canvas>();

            fadeCanvas.overrideSorting = true;
            fadeCanvas.sortingOrder = 999;

            if (fadeGO.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                fadeGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            yield return StartCoroutine(FadeManager.Instance.FadeOut());
        }

        SceneManager.LoadScene(menuSceneName);
    }
}