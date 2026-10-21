using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerOxygen : MonoBehaviour
{
    [Header("Oxigênio")]
    public float maxOxygen = 60f;
    public float currentOxygen;

    [Header("Efeito na Sanidade")]
    public float oxygenSanityLoss = 5f;

    [Header("Referências")]
    public PlayerMovement playerMovement;
    public OxygenBarUI oxygenBar;
    public PlayerSanity playerSanity;

    [Header("Configuração")]
    public string safeSceneName = "InHouse";

    private bool hasDied = false;

    void Start()
    {
        currentOxygen = maxOxygen;

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        if (playerSanity == null)
            playerSanity = GetComponent<PlayerSanity>();

        UpdateOxygenBar();
    }

    void Update()
    {
        if (hasDied)
            return;

        if (HidingSpot.PlayerIsHiding)
            return;

        bool inSafeZone =
            SceneManager.GetActiveScene().name == safeSceneName;

        // Dentro da casa: oxigênio sempre fica em 100%
        if (inSafeZone)
        {
            if (currentOxygen != maxOxygen)
            {
                currentOxygen = maxOxygen;
                UpdateOxygenBar();
            }

            return;
        }

        // Fora da casa: consome oxigênio continuamente
        currentOxygen -= Time.deltaTime;

        currentOxygen = Mathf.Clamp(
            currentOxygen,
            0f,
            maxOxygen
        );

        UpdateOxygenBar();

        float oxygenPercentage = currentOxygen / maxOxygen;

        if (oxygenPercentage <= 0.30f)
        {
            if (playerSanity != null)
            {
                playerSanity.LoseSanity(oxygenSanityLoss * Time.deltaTime);
            }
        }

        if (currentOxygen <= 0f)
        {
            DieFromOxygen();
        }
    }

    void DieFromOxygen()
    {
        if (hasDied)
            return;

        hasDied = true;

        if (playerMovement != null)
        {
            playerMovement.oxygenSpeedMultiplier = 0f;
        }

        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.ShowGameOver();
        }
    }

    void UpdateOxygenBar()
    {
        if (oxygenBar != null)
        {
            oxygenBar.SetOxygen(
                currentOxygen,
                maxOxygen
            );
        }
    }

    public void ResetOxygen()
    {
        currentOxygen = maxOxygen;
        hasDied = false;
        

        UpdateOxygenBar();
    }
}