using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Audio;

public class MainMenu : MonoBehaviour
{
    public string gameSceneName = "SampleScene";

    [Header("Painéis")]
    [Tooltip("Painel do menu principal. Se vazio, usa o próprio objeto onde este script está.")]
    public GameObject mainPanel;
    [Tooltip("Tela Carregar Jogo (novo).")]
    public GameObject loadPanel;
    public GameObject settingsPanel;

    [Header("Outros")]
    public FadeController fadeController;
    public AudioMixer audioMixer;

    private bool loadingGame = false;

    GameObject MainPanel
    {
        get { return mainPanel != null ? mainPanel : gameObject; }
    }

    void Start()
    {
        // Garante o estado inicial: só o menu principal visível
        ShowPanel(MainPanel);

        // Destrava a seleção (caso o menu tenha sido recarregado)
        BrushButton.SelectionLocked = false;

        // Aplica os volumes salvos (Geral, Música, Efeitos) logo que o menu abre
        VolumeSettings.ApplyAll(audioMixer);
    }

    // ---------------------------------------------------------
    // Jogo
    // ---------------------------------------------------------

    public void ContinueGame()
    {
        if (loadingGame)
            return;

        loadingGame = true;

        // Trava o destaque no botão clicado durante o fade
        BrushButton.SelectionLocked = true;

        StartCoroutine(LoadGameWithFade(false));
    }

    public void NewGame()
    {
        if (loadingGame) return;

        PlayerInventory.ResetAll();

        loadingGame = true;

        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.ClearCheckpoint();

        PlayerHunger.ResetSavedHunger();

        // Trava o destaque no botão clicado durante o fade
        BrushButton.SelectionLocked = true;

        StartCoroutine(LoadGameWithFade(true));
    }

    private IEnumerator LoadGameWithFade(bool newGame)
    {
        yield return StartCoroutine(
            fadeController.FadeOut()
        );

        SceneManager.LoadScene(gameSceneName);

        yield return null;

        yield return StartCoroutine(
            fadeController.FadeIn()
        );
    }

    public void QuitGame()
    {
        Debug.Log("Saindo do jogo...");
        Application.Quit();
    }

    // ---------------------------------------------------------
    // Navegação entre painéis
    // ---------------------------------------------------------

    public void OpenLoadGame() { ShowPanel(loadPanel); }
    public void CloseLoadGame() { ShowPanel(MainPanel); }

    public void OpenSettings() { ShowPanel(settingsPanel); }
    public void CloseSettings() { ShowPanel(MainPanel); }

    // Mantido só para não quebrar OnClick antigo que apontava para Settings()
    public void Settings() { OpenSettings(); }

    void ShowPanel(GameObject target)
    {
        GameObject main = MainPanel;

        // Ativa o alvo primeiro; depois desliga os outros.
        // (Este script pode estar no próprio painel principal, então
        // desligar o main não interrompe o método.)
        if (target != null) target.SetActive(true);

        if (main != null && main != target) main.SetActive(false);
        if (loadPanel != null && loadPanel != target) loadPanel.SetActive(false);
        if (settingsPanel != null && settingsPanel != target) settingsPanel.SetActive(false);
    }

    // ---------------------------------------------------------
    // Áudio
    // ---------------------------------------------------------

    public void SetMusicVolume(float volume)
    {
        // Clamp: Log10(0) = -infinito, o que quebra o AudioMixer
        audioMixer.SetFloat(
            "MusicVolume",
            Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f
        );
    }
}