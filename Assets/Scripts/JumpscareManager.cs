using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class JumpscareManager : MonoBehaviour
{
    [Header("Configuração")]
    public GameObject placeholderJumpscareImage;

    [Header("Tempos do Susto e Transição")]
    [Tooltip("Tempo em segundos que a imagem do susto permanece visível")]
    public float jumpscareDuration = 2f;

    [Tooltip("Duração da transição da cor da imagem (Vermelho -> Preto)")]
    public float fadeToBlackDuration = 0.75f;

    [Tooltip("Tempo em segundos na tela preta antes do Game Over surgir")]
    public float blackScreenDuration = 2f;

    private bool jumpscareCancelled = false;

    public IEnumerator PlayJumpscare()
    {
        Debug.Log("[JUMPSCARE] Tocando Jumpscare... Imagem = " + (placeholderJumpscareImage != null));

        jumpscareCancelled = false;

        Image jumpscareImg = null;
        Color originalColor = Color.red;

        if (placeholderJumpscareImage != null)
        {
            placeholderJumpscareImage.SetActive(true);
            placeholderJumpscareImage.transform.SetAsLastSibling();

            jumpscareImg = placeholderJumpscareImage.GetComponent<Image>();
            if (jumpscareImg == null)
            {
                jumpscareImg = placeholderJumpscareImage.GetComponentInChildren<Image>();
            }

            if (jumpscareImg != null)
            {
                originalColor = jumpscareImg.color;
            }
        }

        // Trava o player imediatamente
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerMovement pm = player.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                pm.enabled = false;
            }
        }

        // 1. Exibe a imagem de susto na cor original
        yield return new WaitForSeconds(jumpscareDuration);

        if (jumpscareCancelled || GameOverManager.IsGameOver)
            yield break;

        // 2. Transição gradual da cor da PRÓPRIA imagem para Preto
        if (jumpscareImg != null && fadeToBlackDuration > 0f)
        {
            float timer = 0f;
            while (timer < fadeToBlackDuration)
            {
                if (jumpscareCancelled)
                    yield break;

                timer += Time.deltaTime;
                jumpscareImg.color = Color.Lerp(originalColor, Color.black, timer / fadeToBlackDuration);
                yield return null;
            }
            jumpscareImg.color = Color.black;
        }

        // 3. Permanece totalmente em preto
        yield return new WaitForSeconds(blackScreenDuration);

        if (jumpscareCancelled || GameOverManager.IsGameOver)
            yield break;

        // 4. Aciona a tela de Game Over
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.ShowGameOver(true);
        }
        else
        {
            Debug.LogError("[JUMPSCARE] GameOverManager.Instance não foi encontrado na cena!");
        }

        // Aguarda a transição de Game Over ser concluída antes de desligar o jumpscare
        yield return new WaitForSeconds(1f);

        if (jumpscareImg != null)
        {
            jumpscareImg.color = originalColor;
        }

        if (placeholderJumpscareImage != null)
        {
            placeholderJumpscareImage.SetActive(false);
        }
    }

    public void CancelJumpscare()
    {
        jumpscareCancelled = true;

        if (placeholderJumpscareImage != null)
        {
            Image jumpscareImg = placeholderJumpscareImage.GetComponent<Image>();
            if (jumpscareImg == null)
            {
                jumpscareImg = placeholderJumpscareImage.GetComponentInChildren<Image>();
            }

            if (jumpscareImg != null)
            {
                jumpscareImg.color = Color.red;
            }

            placeholderJumpscareImage.SetActive(false);
        }
    }
}