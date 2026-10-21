using UnityEngine;

public class HidingSpot : MonoBehaviour
{
    [Header("Refer�ncias")]
    public Transform hideSpot;
    public Transform bossSpawnPoint;

    private bool playerInRange = false;

    public static bool PlayerIsHiding = false;
    public static Transform CurrentBossSpawnPoint;

    private bool isHiding = false;
    private Renderer[] playerRenderers;

    private PlayerMovement player;
    private Animator playerAnimator;

    private Vector3 originalPosition;

    void Awake()
    {
        PlayerIsHiding = false;
        isHiding = false;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerRenderers = playerObject.GetComponentsInChildren<Renderer>(true);
        }
    }

    void Update()
    {
        if (PauseManager.IsPaused)
            return;

        if (!CraftingNpc.IsMenuOpen && playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (!isHiding)
            {
                EnterHide();
            }
            else
            {
                if (BossEncounterManager.Instance != null &&
                    BossEncounterManager.Instance.BossIsSearching)
                {
                    return;
                }

                ExitHide();
            }
        }
    }

    void EnterHide()
    {
        if (player == null)
            return;

        isHiding = true;
        PlayerIsHiding = true;
        CurrentBossSpawnPoint = bossSpawnPoint;

        originalPosition = player.transform.position;

        if (hideSpot != null)
            player.transform.position = hideSpot.position;

        player.enabled = false;

        Rigidbody rb = player.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetFloat("Speed", 0f);
        }

        if (InteractionUI.Instance != null)
            InteractionUI.Instance.HidePrompt();

        if (playerRenderers != null)
        {
            foreach (Renderer renderer in playerRenderers)
            {
                renderer.enabled = false;
            }
        }
    }

    void ExitHide()
    {
        if (player == null)
            return;

        isHiding = false;
        PlayerIsHiding = false;

        player.transform.position = originalPosition;

        player.enabled = true;

        Rigidbody rb = player.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        if (InteractionUI.Instance != null)
            InteractionUI.Instance.ShowPrompt();

        if (playerRenderers != null)
        {
            foreach (Renderer renderer in playerRenderers)
            {
                renderer.enabled = true;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!isHiding)
        {
            playerInRange = true;

            player = other.GetComponentInParent<PlayerMovement>();

            if (player != null)
            {
                playerAnimator = player.animator;
            }

            if (InteractionUI.Instance != null)
                InteractionUI.Instance.ShowPrompt();
        }
        else
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;

        if (InteractionUI.Instance != null)
            InteractionUI.Instance.HidePrompt();
    }

    public static void ResetHidingState()
    {
        PlayerIsHiding = false;
        CurrentBossSpawnPoint = null;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            Renderer[] renderers = playerObject.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = true;
            }
        }
    }
}