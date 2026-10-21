using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHunger : MonoBehaviour
{
    [Header("Fome")]
    public float maxHunger = 100f;
    public float currentHunger;

    [Header("Taxa de Fome")]
    public float drainRate = 0.5f;

    [Header("Referências")]
    public HungerBarUI hungerBar;
    public PlayerSanity playerSanity;

    [Header("Efeito na Velocidade")]
    public PlayerMovement playerMovement;

    [Header("Efeitos da Fome")]
    public float lowHungerSanityLoss = 2f;
    public float criticalHungerSanityLoss = 15f;

    private bool hungerDeathTriggered = false;

    // Guarda a fome entre as cenas.
    private static float savedHunger = 100f;
    private static bool hasSavedHunger = false;

    void Start()
    {
        if (hasSavedHunger)
        {
            currentHunger = savedHunger;
        }
        else
        {
            currentHunger = maxHunger;
            savedHunger = currentHunger;
            hasSavedHunger = true;
        }

        if (playerSanity == null)
            playerSanity = GetComponent<PlayerSanity>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        UpdateHungerBar();
    }

    void Update()
    {
        if (GameOverManager.IsGameOver)
            return;

        currentHunger -= drainRate * Time.deltaTime;

        currentHunger = Mathf.Clamp(
            currentHunger,
            0f,
            maxHunger
        );

        // Salva o valor atual para a próxima cena.
        savedHunger = currentHunger;

        HandleHungerEffects();
        HandleHungerSpeed();
        UpdateHungerBar();
    }

    void HandleHungerEffects()
    {
        if (playerSanity == null)
            return;

        if (currentHunger > 50f)
            return;

        if (currentHunger >= 25f)
        {
            playerSanity.LoseSanity(
                lowHungerSanityLoss * Time.deltaTime
            );

            return;
        }

        if (currentHunger > 0f)
        {
            playerSanity.LoseSanity(
                criticalHungerSanityLoss * Time.deltaTime
            );

            return;
        }

        if (currentHunger <= 0f)
        {
            currentHunger = 0f;

            savedHunger = 0f;

            if (!hungerDeathTriggered)
            {
                hungerDeathTriggered = true;

                if (GameOverManager.Instance != null)
                {
                    GameOverManager.Instance.ShowGameOver();
                }
            }
        }
    }

    public void RestoreHunger(float amount)
    {
        currentHunger += amount;

        currentHunger = Mathf.Clamp(
            currentHunger,
            0f,
            maxHunger
        );

        savedHunger = currentHunger;

        hungerDeathTriggered = false;

        UpdateHungerBar();
    }

    public void ResetHunger()
    {
        currentHunger = maxHunger;
        savedHunger = maxHunger;

        hasSavedHunger = true;
        hungerDeathTriggered = false;

        UpdateHungerBar();
    }

    void UpdateHungerBar()
    {
        if (hungerBar != null)
        {
            hungerBar.SetHunger(
                currentHunger,
                maxHunger
            );
        }
    }

    void HandleHungerSpeed()
    {
        if (playerMovement == null)
            return;

        if (currentHunger <= 24f)
            playerMovement.hungerSpeedMultiplier = 0.5f;
        else
            playerMovement.hungerSpeedMultiplier = 1f;
    }

    public static void ResetSavedHunger()
    {
        savedHunger = 100f;
        hasSavedHunger = false;
    }
}