using System.Collections;
using UnityEngine;

public class Bed : MonoBehaviour
{
    [Header("Configura��o")]
    public float interactionDistance = 2f;

    [Header("Fade")]
    public CanvasGroup fadeCanvas;
    public float fadeDuration = 0.5f;
    public float blackScreenDuration = 0.3f;

    private Transform player;
    private PlayerSanity playerSanity;
    private PlayerWarmth playerWarmth;
    private PlayerMovement playerMovement;

    private bool playerNearby = false;
    private bool sleeping = false;
    private bool promptShowing = false;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;

            playerSanity = playerObject.GetComponent<PlayerSanity>();
            playerWarmth = playerObject.GetComponent<PlayerWarmth>();
            playerMovement = playerObject.GetComponent<PlayerMovement>();
        }

        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f;
            fadeCanvas.blocksRaycasts = false;
        }
    }

    void Update()
    {
        if (sleeping || player == null || CraftingNpc.IsMenuOpen)
            return;

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        playerNearby = distance <= interactionDistance;

        if (playerNearby)
        {
            if (!promptShowing && InteractionUI.Instance != null)
            {
                InteractionUI.Instance.ShowPrompt();
                promptShowing = true;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                StartCoroutine(Sleep());
            }
        }
        else
        {
            if (promptShowing && InteractionUI.Instance != null)
            {
                InteractionUI.Instance.HidePrompt();
                promptShowing = false;
            }
        }

    }

    IEnumerator Sleep()
    {
        sleeping = true;

        if (InteractionUI.Instance != null)
            InteractionUI.Instance.HidePrompt();

        // Impede o jogador de andar durante o fade
        if (playerMovement != null)
            playerMovement.enabled = false;

        // Fade para preto
        yield return StartCoroutine(Fade(1f));

        // Tela preta por um pequeno per�odo
        yield return new WaitForSeconds(blackScreenDuration);

        // Recupera os atributos
        if (playerSanity != null)
            playerSanity.ResetSanity();

        if (playerWarmth != null)
            playerWarmth.ResetWarmth();

        // Fade de volta
        yield return StartCoroutine(Fade(0f));

        // Libera o movimento
        if (playerMovement != null)
            playerMovement.enabled = true;

        sleeping = false;
    }

    IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvas == null)
            yield break;

        float startAlpha = fadeCanvas.alpha;
        float elapsed = 0f;

        fadeCanvas.blocksRaycasts = true;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float percentage = elapsed / fadeDuration;

            fadeCanvas.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                percentage
            );

            yield return null;
        }

        fadeCanvas.alpha = targetAlpha;

        if (targetAlpha == 0f)
            fadeCanvas.blocksRaycasts = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            interactionDistance
        );
    }
}