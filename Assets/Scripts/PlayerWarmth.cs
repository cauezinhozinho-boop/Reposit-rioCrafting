using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerWarmth : MonoBehaviour
{
    [Header("Calor")]
    public float maxWarmth = 100f;
    public float currentWarmth;

    [Header("Taxas")]
    public float coldRate = 2f;
    public float warmRate = 15f;

    [Header("Velocidade")]
    [Range(0f, 1f)]
    public float minimumSpeedMultiplier = 0.5f;

    [Header("Efeito do Frio na Sanidade")]
    public float coldSanityLoss = 5f;

    [Header("Configuração")]
    public string safeSceneName = "InHouse";

    [Header("Referências")]
    public WarmthBarUI warmthBar;
    public PlayerMovement playerMovement;
    public PlayerSanity playerSanity;

    private bool isNearFire = false;
    private bool hasDied = false;

    void Start()
    {
        currentWarmth = maxWarmth;

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        if (playerSanity == null)
            playerSanity = GetComponent<PlayerSanity>();

        UpdateWarmthBar();
        UpdateMovementSpeed();
    }

    void Update()
    {
        if (hasDied)
            return;

        if (HidingSpot.PlayerIsHiding)
            return;

        bool inSafeZone =
            SceneManager.GetActiveScene().name == safeSceneName;

        if (inSafeZone)
        {
            currentWarmth = maxWarmth;
        }
        else if (isNearFire)
        {
            currentWarmth += warmRate * Time.deltaTime;
        }
        else
        {
            currentWarmth -= coldRate * Time.deltaTime;
        }

        currentWarmth = Mathf.Clamp(
            currentWarmth,
            0f,
            maxWarmth
        );

        UpdateWarmthBar();
        UpdateMovementSpeed();

        if (!inSafeZone && currentWarmth / maxWarmth <= 0.30f)
        {
            if (playerSanity != null)
            {
                playerSanity.LoseSanity(
                    coldSanityLoss * Time.deltaTime
                );
            }
        }

        if (currentWarmth <= 0f && !inSafeZone)
        {
            DieFromCold();
        }
    }

    void UpdateMovementSpeed()
    {
        if (playerMovement == null)
            return;

        float warmthPercentage = currentWarmth / maxWarmth;

        playerMovement.warmthSpeedMultiplier = Mathf.Lerp(
            minimumSpeedMultiplier,
            1f,
            warmthPercentage
        );
    }

    void DieFromCold()
    {
        if (hasDied)
            return;

        hasDied = true;

        if (playerMovement != null)
            playerMovement.warmthSpeedMultiplier = 0f;

        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.ShowGameOver();
        }
    }

    void UpdateWarmthBar()
    {
        if (warmthBar != null)
        {
            warmthBar.SetWarmth(
                currentWarmth,
                maxWarmth
            );
        }
    }

    public void SetNearFire(bool value)
    {
        isNearFire = value;
    }

    public void ResetWarmth()
    {
        currentWarmth = maxWarmth;
        hasDied = false;

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (playerMovement != null)
            playerMovement.warmthSpeedMultiplier = 1f;

        UpdateWarmthBar();
    }
}