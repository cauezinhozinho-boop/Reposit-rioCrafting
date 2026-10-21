using UnityEngine;

public class PlayerSanity : MonoBehaviour
{
    [Header("Sanidade")]
    public float maxSanity = 100f;
    public float currentSanity;

    [Header("Referência")]
    public SanityBarUI sanityBar;

    [Header("Recuperação na Fogueira")]
    public Light fireLight;
    public float fireSanityRecovery = 5f;

    void Start()
    {
        currentSanity = maxSanity;
        UpdateSanityBar();
    }

    void Update()
    {
        if (GameOverManager.IsGameOver)
            return;

        if (Input.GetKeyDown(KeyCode.K))
        {
            LoseSanity(10f);
        }

        if (fireLight != null)
        {
            float distanceToFire =
                Vector3.Distance(
                    transform.position,
                    fireLight.transform.position
                );

            if (distanceToFire <= fireLight.range)
            {
                RecoverSanity(
                    fireSanityRecovery * Time.deltaTime
                );
            }
        }
    }

    public void LoseSanity(float amount)
    {
        if (GameOverManager.IsGameOver)
            return;

        currentSanity -= amount;
        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);

        UpdateSanityBar();

        if (currentSanity <= 0f)
        {
            OnSanityZero();
        }
    }

    public void RecoverSanity(float amount)
    {
        if (GameOverManager.IsGameOver)
            return;

        currentSanity += amount;
        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);

        UpdateSanityBar();
    }

    void OnSanityZero()
    {
        Debug.Log("Sanidade chegou a 0.");
    }

    void UpdateSanityBar()
    {
        if (sanityBar != null)
        {
            sanityBar.SetSanity(currentSanity, maxSanity);
        }
    }

    public void ResetSanity()
    {
        currentSanity = maxSanity;
        UpdateSanityBar();
    }
}