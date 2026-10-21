using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossEncounterManager : MonoBehaviour
{
    public static BossEncounterManager Instance;

    [Header("Timer Aleatório")]
    public float minRandomTime = 180f;
    public float maxRandomTime = 300f;
    public bool randomEncountersEnabled = true;

    [Header("Aviso")]
    public float warningDuration = 10f;

    [Header("Sanidade durante o Minigame")]
    public float breathingSanityLoss = 3f;

    [Header("Tempo do Boss")]
    public float searchDuration = 15f;
    public float timeBeforeLeaving = 3f;

    [Header("Referências")]
    public BossWarningUI warningUI;
    public JumpscareManager jumpscareManager;
    public BossBreathingMinigame breathingMinigame;
    public Light fireLight;

    [Header("Boss")]
    public GameObject boss;
    public Transform bossSpawnPoint;

    private bool encounterInProgress = false;

    public bool BossIsSearching { get; private set; } = false;

    private Coroutine randomEncounterCoroutine;

    private bool IsPlayerInHouse()
    {
        return SceneManager.GetActiveScene().name == "InHouse";
    }

    private bool IsPlayerProtectedByFire()
    {
        if (fireLight == null)
            return false;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            return false;

        float distanceToFire = Vector3.Distance(
            player.transform.position,
            fireLight.transform.position
        );

        return distanceToFire <= fireLight.range;
    }

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
        }
    }

    void Start()
    {
        FindSceneReferences();

        if (randomEncountersEnabled)
        {
            StartRandomEncounterLoop();
        }
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
        Debug.Log("[BOSS] Nova cena carregada: " + scene.name);

        HidingSpot.ResetHidingState();
        FindSceneReferences();

        encounterInProgress = false;
        BossIsSearching = false;

        if (boss != null)
            boss.SetActive(false);

        if (randomEncountersEnabled)
        {
            StartRandomEncounterLoop();
        }
    }

    void FindSceneReferences()
    {
        Debug.Log("[BOSS] Procurando referências da cena...");

        // Utiliza FindAnyObjectByType para evitar avisos de ordenação por InstanceID
        warningUI = FindAnyObjectByType<BossWarningUI>(FindObjectsInactive.Include);
        jumpscareManager = FindAnyObjectByType<JumpscareManager>(FindObjectsInactive.Include);
        breathingMinigame = FindAnyObjectByType<BossBreathingMinigame>(FindObjectsInactive.Include);

        // Fogueira
        fireLight = null;
        GameObject fireObject = GameObject.Find("Fogueira");

        if (fireObject != null)
        {
            fireLight = fireObject.GetComponentInChildren<Light>(true);
        }

        // Busca sem o parâmetro de ordenação FindObjectsSortMode (descontinuado)
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include);

        boss = null;
        bossSpawnPoint = null;

        foreach (GameObject obj in allObjects)
        {
            string objectName = obj.name.ToLower();

            bool isUI = obj.GetComponent<Canvas>() != null ||
                        obj.GetComponent<BossWarningUI>() != null ||
                        obj.GetComponent<BossBreathingMinigame>() != null ||
                        obj.GetComponent<UnityEngine.UI.Graphic>() != null;

            bool isSpawn = objectName.Contains("spawn") || objectName.Contains("point");

            if (boss == null && objectName.Contains("boss") && !isUI && !isSpawn)
            {
                if (obj.GetComponent<BossEncounterManager>() == null)
                {
                    boss = obj;
                }
            }

            if (bossSpawnPoint == null && objectName.Contains("bossspawnpoint"))
            {
                bossSpawnPoint = obj.transform;
            }
        }

        if (boss != null)
        {
            boss.SetActive(false);
            Debug.Log("[BOSS] Modelo 3D do Boss encontrado: " + boss.name);
        }

        if (bossSpawnPoint != null)
        {
            Debug.Log("[BOSS] BossSpawnPoint encontrado: " + bossSpawnPoint.name);
        }
    }

    void StartRandomEncounterLoop()
    {
        if (randomEncounterCoroutine != null)
        {
            StopCoroutine(randomEncounterCoroutine);
        }

        randomEncounterCoroutine = StartCoroutine(RandomEncounterLoop());
    }

    IEnumerator RandomEncounterLoop()
    {
        while (true)
        {
            if (IsPlayerInHouse())
            {
                yield return null;
                continue;
            }

            if (GameOverManager.IsGameOver)
            {
                yield return null;
                continue;
            }

            float waitTime = Random.Range(minRandomTime, maxRandomTime);

            Debug.Log("[BOSS] Próximo encontro aleatório em " + waitTime + " segundos.");

            yield return new WaitForSeconds(waitTime);

            if (!GameOverManager.IsGameOver &&
                !encounterInProgress &&
                !IsPlayerInHouse())
            {
                TriggerEncounter();
            }

            while (encounterInProgress)
            {
                if (GameOverManager.IsGameOver)
                    yield break;

                yield return null;
            }
        }
    }

    public void TriggerEncounter()
    {
        if (GameOverManager.IsGameOver)
            return;

        if (encounterInProgress)
            return;

        if (IsPlayerInHouse())
        {
            Debug.Log("[BOSS] Jogador está na casa. Boss não irá aparecer.");
            return;
        }

        if (IsPlayerProtectedByFire())
        {
            Debug.Log("[BOSS] Jogador está protegido pela fogueira. Boss não irá aparecer.");
            return;
        }

        StartCoroutine(EncounterRoutine());
    }

    IEnumerator EncounterRoutine()
    {
        encounterInProgress = true;

        if (IsPlayerInHouse())
        {
            encounterInProgress = false;
            yield break;
        }

        if (warningUI == null)
        {
            Debug.LogError("[BOSS] BossWarningUI não encontrado!");
            encounterInProgress = false;
            yield break;
        }

        warningUI.ShowWarning();

        float timer = 0f;

        while (timer < warningDuration)
        {
            if (IsPlayerInHouse())
            {
                Debug.Log("[BOSS] Jogador entrou na casa durante o aviso. Encontro cancelado.");
                warningUI.HideWarning();
                encounterInProgress = false;
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        bool playerHidInTime = HidingSpot.PlayerIsHiding;

        warningUI.HideWarning();

        if (IsPlayerInHouse())
        {
            Debug.Log("[BOSS] Jogador está na casa. Encontro cancelado.");
            encounterInProgress = false;
            yield break;
        }

        if (!playerHidInTime)
        {
            if (jumpscareManager != null)
            {
                yield return StartCoroutine(jumpscareManager.PlayJumpscare());
            }
        }
        else
        {
            yield return StartCoroutine(BossSearchRoutine());
        }

        encounterInProgress = false;
    }

    IEnumerator BossSearchRoutine()
    {
        BossIsSearching = true;

        if (IsPlayerInHouse())
        {
            Debug.Log("[BOSS] Jogador entrou na casa. Boss não irá procurar.");
            BossIsSearching = false;

            if (boss != null)
                boss.SetActive(false);

            yield break;
        }

        if (boss == null || bossSpawnPoint == null)
        {
            FindSceneReferences();
        }

        if (boss != null)
            boss.SetActive(false);

        Debug.Log("[BOSS] Boss entrou na área.");

        Transform currentSpawnPoint = HidingSpot.CurrentBossSpawnPoint;

        if (currentSpawnPoint == null)
        {
            currentSpawnPoint = bossSpawnPoint;
        }

        if (currentSpawnPoint == null)
        {
            Debug.LogError("[BOSS] Nenhum Boss Spawn Point encontrado!");
            BossIsSearching = false;
            yield break;
        }

        if (boss != null)
        {
            boss.SetActive(true);
            boss.transform.position = currentSpawnPoint.position;
            boss.transform.rotation = currentSpawnPoint.rotation;

            Debug.Log("[BOSS] Boss posição APÓS spawn: " + boss.transform.position);
        }
        else
        {
            Debug.LogError("[BOSS] Boss não encontrado!");
        }

        if (!HidingSpot.PlayerIsHiding)
        {
            BossIsSearching = false;

            if (boss != null)
                boss.SetActive(false);

            if (jumpscareManager != null)
            {
                yield return StartCoroutine(jumpscareManager.PlayJumpscare());
            }

            yield break;
        }

        if (breathingMinigame == null)
        {
            Debug.LogError("[BOSS] BossBreathingMinigame não está configurado.");
            BossIsSearching = false;

            if (boss != null)
                boss.SetActive(false);

            yield break;
        }

        CanvasGroup canvasGroup = breathingMinigame.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        breathingMinigame.gameObject.SetActive(true);
        breathingMinigame.StartMinigame();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerSanity playerSanity = null;

        if (player != null)
        {
            playerSanity = player.GetComponent<PlayerSanity>();
        }

        while (!breathingMinigame.MinigameFinished)
        {
            if (IsPlayerInHouse())
            {
                breathingMinigame.StopMinigame();
                BossIsSearching = false;

                if (boss != null)
                    boss.SetActive(false);

                yield break;
            }

            if (!HidingSpot.PlayerIsHiding)
            {
                BossIsSearching = false;
                breathingMinigame.StopMinigame();

                if (boss != null)
                    boss.SetActive(false);

                if (jumpscareManager != null)
                {
                    yield return StartCoroutine(jumpscareManager.PlayJumpscare());
                }

                yield break;
            }

            if (GameOverManager.IsGameOver)
            {
                breathingMinigame.StopMinigame();
                BossIsSearching = false;

                if (boss != null)
                    boss.SetActive(false);

                yield break;
            }

            if (playerSanity != null)
            {
                playerSanity.LoseSanity(breathingSanityLoss * Time.deltaTime);
            }

            yield return null;
        }

        if (!breathingMinigame.MinigameWon)
        {
            BossIsSearching = false;

            if (boss != null)
                boss.SetActive(false);

            if (jumpscareManager != null)
            {
                yield return StartCoroutine(jumpscareManager.PlayJumpscare());
            }

            yield break;
        }

        yield return new WaitForSeconds(timeBeforeLeaving);

        if (boss != null)
            boss.SetActive(false);

        BossIsSearching = false;
    }
}