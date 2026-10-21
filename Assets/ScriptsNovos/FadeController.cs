using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Fade do menu.
/// FadeOut = ESCURECE (da tela normal para preto).
/// FadeIn  = CLAREIA (do preto para a tela normal).
/// O painel é ligado sozinho quando precisa, então não importa se ele está
/// desmarcado na Hierarchy (antes, desmarcado = o fade não aparecia).
/// </summary>
public class FadeController : MonoBehaviour
{
    public Image fadePanel;
    public float fadeDuration = 1f;

    private void Start()
    {
        StartCoroutine(FadeIn());
    }

    public IEnumerator FadeIn()
    {
        if (fadePanel == null)
            yield break;

        fadePanel.gameObject.SetActive(true);

        Color color = fadePanel.color;
        color.a = 1f;
        fadePanel.color = color;

        fadePanel.raycastTarget = true;

        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(1f, 0f, time / fadeDuration);
            fadePanel.color = color;

            yield return null;
        }

        color.a = 0f;
        fadePanel.color = color;

        fadePanel.raycastTarget = false;

        // Terminou: não precisa ficar ligado (nem desenhando por cima do menu)
        fadePanel.gameObject.SetActive(false);
    }

    public IEnumerator FadeOut()
    {
        if (fadePanel == null)
            yield break;

        fadePanel.gameObject.SetActive(true);

        Color color = fadePanel.color;
        color.a = 0f;
        fadePanel.color = color;

        fadePanel.raycastTarget = true;

        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(0f, 1f, time / fadeDuration);
            fadePanel.color = color;

            yield return null;
        }

        color.a = 1f;
        fadePanel.color = color;
    }
}
