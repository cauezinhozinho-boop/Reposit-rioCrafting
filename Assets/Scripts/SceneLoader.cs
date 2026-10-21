using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    private string pendingSpawnPointName;
    private bool isTransitioning = false;
    public bool IsTransitioning
    {
        get { return isTransitioning; }
    }


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string sceneName, string spawnPointName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName, spawnPointName));
    }

    IEnumerator TransitionRoutine(string sceneName, string spawnPointName)
    {
        isTransitioning = true;
        pendingSpawnPointName = spawnPointName;
        Debug.Log("1. TransitionRoutine iniciou");

        if (InteractionUI.Instance != null)
        {
            InteractionUI.Instance.HidePrompt();
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerMovement playerMovement = null;
        Rigidbody rb = null;

        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            rb = player.GetComponent<Rigidbody>();

            if (playerMovement != null)
                playerMovement.enabled = false;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        Debug.Log("2. Player travado, indo pro FadeOut");

        yield return StartCoroutine(
            FadeManager.Instance.FadeOut()
        );

        Debug.Log("3. FadeOut terminou");

        SceneManager.sceneLoaded += OnSceneLoaded;

        SceneManager.LoadScene(sceneName);

        Debug.Log("4. LoadScene chamado");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("5. OnSceneLoaded disparou");

        SceneManager.sceneLoaded -= OnSceneLoaded;

        GameObject spawnPoint =
            GameObject.Find(pendingSpawnPointName);

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        Debug.Log(
            "6. SpawnPoint achado? " +
            (spawnPoint != null) +
            " | Player achado? " +
            (player != null)
        );

        if (spawnPoint != null && player != null)
        {
            player.transform.position =
                spawnPoint.transform.position;

            Rigidbody rb =
                player.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        StartCoroutine(FinishTransition(player));

        Debug.Log("7. FinishTransition iniciado");
    }

    IEnumerator FinishTransition(GameObject player)
    {
        Debug.Log("8. Esperando FadeIn");

        yield return StartCoroutine(
            FadeManager.Instance.FadeIn()
        );

        Debug.Log("9. FadeIn terminou");

        if (player != null)
        {
            PlayerMovement playerMovement =
                player.GetComponent<PlayerMovement>();

            if (playerMovement != null)
            {
                playerMovement.enabled = true;
                Debug.Log("10. Movimento reativado!");
            }
            else
            {
                Debug.Log(
                    "10. ERRO: playerMovement é null!"
                );
            }
        }
        else
        {
            Debug.Log(
                "10. ERRO: player é null!"
            );
        }

        isTransitioning = false;
    }
}