using UnityEngine;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance;

    public GameObject promptObject;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        HidePrompt();
    }

    public void ShowPrompt()
    {
        if (promptObject != null)
            promptObject.SetActive(true);
    }

    public void HidePrompt()
    {
        if (promptObject != null)
            promptObject.SetActive(false);
    }
}