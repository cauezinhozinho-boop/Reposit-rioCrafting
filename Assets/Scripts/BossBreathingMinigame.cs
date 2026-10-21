using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BossBreathingMinigame : MonoBehaviour
{
    [Header("UI")]
    public GameObject minigamePanel;

    public RectTransform leftNote;
    public RectTransform rightNote;

    public RectTransform leftTargetZone;
    public RectTransform rightTargetZone;

    public Slider breathingBar;
    public TMP_Text resultText;

    public CanvasGroup minigameCanvasGroup;

    [Header("Entrada")]
    public float minigameAppearDelay = 1.5f;
    public float fadeInDuration = 0.75f;

    [Header("Saída")]
    public float exitDelay = 2f;
    public float fadeOutDuration = 0.75f;

    [Header("Ritmo")]
    public float noteSpeed = 500f;
    public float noteDisappearDistance = 150f;

    [Header("Duração")]
    public float gameDuration = 15f;

    [Header("Respiração")]
    public float maxBreathing = 100f;
    public float breathingDrain = 5f;
    public float hitRecovery = 8f;
    public float missDamage = 15f;

    [Header("Oxigênio")]
    public PlayerOxygen playerOxygen;
    public float oxygenBonusPercentage = 0.20f;
    public float performanceThreshold = 0.70f;

    [Header("Precisão")]
    public float perfectRange = 25f;
    public float goodRange = 60f;

    [Header("Dificuldade por Sanidade")]
    public PlayerSanity playerSanity;

    [Tooltip("Multiplicador máximo da velocidade das notas com 0% de sanidade.")]
    public float maxDifficultyNoteMultiplier = 1.5f;

    [Tooltip("Multiplicador mínimo do Perfect com 0% de sanidade.")]
    public float minPerfectRangeMultiplier = 0.75f;

    [Tooltip("Multiplicador mínimo do Good com 0% de sanidade.")]
    public float minGoodRangeMultiplier = 0.85f;

    private float breathing;
    private float gameTimer;

    private bool playing = false;

    private bool leftNoteActive = false;
    private bool rightNoteActive = false;

    public bool MinigameFinished { get; private set; }
    public bool MinigameWon { get; private set; }

    void Awake()
    {
        if (minigameCanvasGroup == null)
        {
            minigameCanvasGroup = GetComponent<CanvasGroup>();
        }
    }

    void Start()
    {
        if (playerSanity == null)
        {
            playerSanity = GetComponentInParent<PlayerSanity>();

            if (playerSanity == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");

                if (player != null)
                    playerSanity = player.GetComponent<PlayerSanity>();
            }

            if (playerOxygen == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");

                if (player != null)
                    playerOxygen = player.GetComponent<PlayerOxygen>();
            }
        }
    }

    void Update()
    {
        if (!playing)
            return;

        if (GameOverManager.IsGameOver)
        {
            StopMinigame();
            return;
        }

        if (!HidingSpot.PlayerIsHiding)
        {
            StopMinigame();
            return;
        }

        gameTimer += Time.deltaTime;

        MoveNotes();

        breathing -= breathingDrain * Time.deltaTime;

        if (breathing < 0)
            breathing = 0;

        breathingBar.value = breathing;

        // =========================================================
        // INPUT (agora configurável pelo sistema de KeyBindings)
        // =========================================================
        // Padrão: LMB para esquerda, RMB para direita.
        // O jogador pode rebindar na tela de Controles.
        if (KeyBindings.GetDown(GameAction.MinigameLeft))
        {
            CheckLeftNote();
        }

        if (KeyBindings.GetDown(GameAction.MinigameRight))
        {
            CheckRightNote();
        }

        if (gameTimer >= gameDuration)
        {
            WinMinigame();
        }

        if (breathing <= 0)
        {
            LoseMinigame();
        }
    }

    public void StartMinigame()
    {
        if (!HidingSpot.PlayerIsHiding)
        {
            Debug.Log("Minigame cancelado: jogador não está escondido.");
            return;
        }

        StartCoroutine(StartMinigameRoutine());
    }

    IEnumerator StartMinigameRoutine()
    {
        playing = false;

        // Reativa explicitamente o painel do minijogo na UI
        if (minigamePanel != null)
            minigamePanel.SetActive(true);

        breathing = maxBreathing;
        gameTimer = 0f;

        MinigameFinished = false;
        MinigameWon = false;

        breathingBar.maxValue = maxBreathing;
        breathingBar.value = breathing;

        if (resultText != null)
            resultText.text = "";

        leftNoteActive = false;
        rightNoteActive = false;

        leftNote.gameObject.SetActive(false);
        rightNote.gameObject.SetActive(false);

        // Delay antes de aparecer
        yield return new WaitForSeconds(minigameAppearDelay);

        if (GameOverManager.IsGameOver || !HidingSpot.PlayerIsHiding)
            yield break;

        // Fade In
        if (minigameCanvasGroup != null)
        {
            minigameCanvasGroup.alpha = 0f;

            float timer = 0f;

            while (timer < fadeInDuration)
            {
                if (GameOverManager.IsGameOver || !HidingSpot.PlayerIsHiding)
                    yield break;

                timer += Time.deltaTime;

                minigameCanvasGroup.alpha = Mathf.Clamp01(timer / fadeInDuration);

                yield return null;
            }

            minigameCanvasGroup.alpha = 1f;
        }

        playing = true;

        StartCoroutine(NoteLoop());
    }

    float GetSanityPercentage()
    {
        if (playerSanity == null)
            return 1f;

        if (playerSanity.maxSanity <= 0f)
            return 1f;

        return Mathf.Clamp01(playerSanity.currentSanity / playerSanity.maxSanity);
    }

    float GetCurrentNoteSpeed()
    {
        float sanityPercentage = GetSanityPercentage();

        float multiplier = Mathf.Lerp(
            maxDifficultyNoteMultiplier,
            1f,
            sanityPercentage
        );

        return noteSpeed * multiplier;
    }

    float GetCurrentPerfectRange()
    {
        float sanityPercentage = GetSanityPercentage();

        return Mathf.Lerp(
            perfectRange * minPerfectRangeMultiplier,
            perfectRange,
            sanityPercentage
        );
    }

    float GetCurrentGoodRange()
    {
        float sanityPercentage = GetSanityPercentage();

        return Mathf.Lerp(
            goodRange * minGoodRangeMultiplier,
            goodRange,
            sanityPercentage
        );
    }

    void MoveNotes()
    {
        float currentNoteSpeed = GetCurrentNoteSpeed();

        if (leftNoteActive)
        {
            leftNote.anchoredPosition += Vector2.right * currentNoteSpeed * Time.deltaTime;

            float disappearX = leftTargetZone.anchoredPosition.x + noteDisappearDistance;

            if (leftNote.anchoredPosition.x >= disappearX)
            {
                MissLeftNote();
            }
        }

        if (rightNoteActive)
        {
            rightNote.anchoredPosition += Vector2.left * currentNoteSpeed * Time.deltaTime;

            float disappearX = rightTargetZone.anchoredPosition.x - noteDisappearDistance;

            if (rightNote.anchoredPosition.x <= disappearX)
            {
                MissRightNote();
            }
        }
    }

    void CheckLeftNote()
    {
        if (!leftNoteActive)
            return;

        float distance = Mathf.Abs(leftNote.anchoredPosition.x - leftTargetZone.anchoredPosition.x);

        float currentPerfectRange = GetCurrentPerfectRange();
        float currentGoodRange = GetCurrentGoodRange();

        if (distance <= currentPerfectRange)
        {
            leftNoteActive = false;
            leftNote.gameObject.SetActive(false);
            PerfectHit();
        }
        else if (distance <= currentGoodRange)
        {
            leftNoteActive = false;
            leftNote.gameObject.SetActive(false);
            GoodHit();
        }
        else
        {
            MissLeftNote();
        }
    }

    void CheckRightNote()
    {
        if (!rightNoteActive)
            return;

        float distance = Mathf.Abs(rightNote.anchoredPosition.x - rightTargetZone.anchoredPosition.x);

        float currentPerfectRange = GetCurrentPerfectRange();
        float currentGoodRange = GetCurrentGoodRange();

        if (distance <= currentPerfectRange)
        {
            rightNoteActive = false;
            rightNote.gameObject.SetActive(false);
            PerfectHit();
        }
        else if (distance <= currentGoodRange)
        {
            rightNoteActive = false;
            rightNote.gameObject.SetActive(false);
            GoodHit();
        }
        else
        {
            MissRightNote();
        }
    }

    void SpawnLeftNote()
    {
        if (!playing)
            return;

        leftNote.gameObject.SetActive(true);
        leftNote.anchoredPosition = new Vector2(-600f, leftTargetZone.anchoredPosition.y);
        leftNoteActive = true;
    }

    void SpawnRightNote()
    {
        if (!playing)
            return;

        rightNote.gameObject.SetActive(true);
        rightNote.anchoredPosition = new Vector2(600f, rightTargetZone.anchoredPosition.y);
        rightNoteActive = true;
    }

    IEnumerator NoteLoop()
    {
        yield return new WaitForSeconds(Random.Range(0.3f, 1.2f));

        while (playing)
        {
            int randomSide = Random.Range(0, 2);

            if (randomSide == 0)
            {
                SpawnLeftNote();
                yield return new WaitUntil(() => !leftNoteActive || !playing);
            }
            else
            {
                SpawnRightNote();
                yield return new WaitUntil(() => !rightNoteActive || !playing);
            }

            if (!playing)
                yield break;

            yield return new WaitForSeconds(Random.Range(0.01f, 0.2f));
        }
    }

    void PerfectHit()
    {
        breathing += hitRecovery;

        if (breathing > maxBreathing)
            breathing = maxBreathing;

        breathingBar.value = breathing;

        if (resultText != null)
            resultText.text = "PERFECT!";
    }

    void GoodHit()
    {
        breathing += hitRecovery * 0.5f;

        if (breathing > maxBreathing)
            breathing = maxBreathing;

        breathingBar.value = breathing;

        if (resultText != null)
            resultText.text = "GOOD";
    }

    void MissLeftNote()
    {
        leftNoteActive = false;
        leftNote.gameObject.SetActive(false);

        breathing -= missDamage;

        if (breathing < 0)
            breathing = 0;

        breathingBar.value = breathing;

        if (resultText != null)
            resultText.text = "MISS";
    }

    void MissRightNote()
    {
        rightNoteActive = false;
        rightNote.gameObject.SetActive(false);

        breathing -= missDamage;

        if (breathing < 0)
            breathing = 0;

        breathingBar.value = breathing;

        if (resultText != null)
            resultText.text = "MISS";
    }

    void GiveOxygenBonus()
    {
        if (playerOxygen == null)
            return;

        float oxygenBonus = playerOxygen.maxOxygen * oxygenBonusPercentage;
        playerOxygen.currentOxygen += oxygenBonus;

        playerOxygen.currentOxygen = Mathf.Clamp(
            playerOxygen.currentOxygen,
            0f,
            playerOxygen.maxOxygen
        );

        Debug.Log("Oxigênio recuperado: +" + oxygenBonus);
    }

    void WinMinigame()
    {
        if (!playing)
            return;

        playing = false;

        float performance = breathing / maxBreathing;
        float sanityPercentage = GetSanityPercentage();
        bool lowSanity = sanityPercentage <= 0.50f;

        float detectionThreshold = lowSanity ? 0.50f : 0.30f;
        float oxygenBonusThreshold = lowSanity ? 0.80f : 0.70f;

        if (performance < detectionThreshold)
        {
            MinigameWon = false;
            Debug.Log("Boss percebeu o jogador.");
        }
        else
        {
            MinigameWon = true;

            if (performance > oxygenBonusThreshold)
            {
                GiveOxygenBonus();
                Debug.Log("Excelente respiração! Bônus de oxigênio concedido.");
            }
            else
            {
                Debug.Log("Minigame concluído, mas desempenho insuficiente para bônus.");
            }
        }

        StartCoroutine(EndMinigameRoutine());
    }

    void LoseMinigame()
    {
        if (!playing)
            return;

        playing = false;

        MinigameFinished = true;
        MinigameWon = false;

        Debug.Log("Boss percebeu o jogador.");

        StartCoroutine(FadeOutAfterLoss());
    }

    IEnumerator EndMinigameRoutine()
    {
        playing = false;

        yield return new WaitForSeconds(exitDelay);

        if (minigameCanvasGroup != null)
        {
            float timer = 0f;
            float startingAlpha = minigameCanvasGroup.alpha;

            while (timer < fadeOutDuration)
            {
                timer += Time.deltaTime;
                minigameCanvasGroup.alpha = Mathf.Lerp(startingAlpha, 0f, timer / fadeOutDuration);
                yield return null;
            }

            minigameCanvasGroup.alpha = 0f;
        }

        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        leftNoteActive = false;
        rightNoteActive = false;

        leftNote.gameObject.SetActive(false);
        rightNote.gameObject.SetActive(false);

        MinigameFinished = true;
    }

    IEnumerator FadeOutAfterLoss()
    {
        if (minigameCanvasGroup != null)
        {
            float timer = 0f;
            float startingAlpha = minigameCanvasGroup.alpha;

            while (timer < fadeOutDuration)
            {
                timer += Time.deltaTime;
                minigameCanvasGroup.alpha = Mathf.Lerp(startingAlpha, 0f, timer / fadeOutDuration);
                yield return null;
            }

            minigameCanvasGroup.alpha = 0f;
        }

        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        leftNoteActive = false;
        rightNoteActive = false;

        leftNote.gameObject.SetActive(false);
        rightNote.gameObject.SetActive(false);
    }

    public void StopMinigame()
    {
        playing = false;

        MinigameFinished = false;
        MinigameWon = false;

        leftNoteActive = false;
        rightNoteActive = false;

        if (leftNote != null)
            leftNote.gameObject.SetActive(false);

        if (rightNote != null)
            rightNote.gameObject.SetActive(false);

        if (minigameCanvasGroup != null)
            minigameCanvasGroup.alpha = 0f;

        if (minigamePanel != null)
            minigamePanel.SetActive(false);
    }
}