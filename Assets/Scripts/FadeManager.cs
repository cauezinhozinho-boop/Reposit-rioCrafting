using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;

[Header("Configuração")]
    public Image fadeImage;
    public float fadeDuration = 1f;
    public float blackScreenDelay = 2f;

    private Coroutine currentFade;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        StartFadeIn();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[FADE] Nova cena carregada: " + scene.name);

if (SceneLoader.Instance != null &&
    SceneLoader.Instance.IsTransitioning)
        {
            Debug.Log(
                "[FADE] Transição do SceneLoader detectada. " +
                "O próprio SceneLoader cuidará do FadeIn."
            );

            return;
        }

        Debug.Log(
            "[FADE] Cena carregada diretamente. Iniciando FadeIn."
        );

        StartFadeIn();

}


    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    public void StartFadeIn()
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(FadeIn());
    }

    public void StartFadeOut()
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(FadeOut());
    }

    public IEnumerator FadeOut()
    {
        if (fadeImage == null)
            yield break;

        fadeImage.gameObject.SetActive(true);

        Color c = fadeImage.color;

        float startAlpha = c.a;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;

            c.a = Mathf.Lerp(
                startAlpha,
                1f,
                Mathf.Clamp01(t / fadeDuration)
            );

            fadeImage.color = c;

            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;

        currentFade = null;
    }

    public IEnumerator FadeIn()
    {
        if (fadeImage == null)
            yield break;

        Color c = fadeImage.color;

        c.a = 1f;
        fadeImage.color = c;

        if (blackScreenDelay > 0f)
        {
            float timer = 0f;

            while (timer < blackScreenDelay)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;

            c.a = 1f - Mathf.Clamp01(t / fadeDuration);

            fadeImage.color = c;

            yield return null;
        }

        c.a = 0f;
        fadeImage.color = c;

        currentFade = null;
    }

}
