using UnityEngine;
using TMPro;

public class BossWarningUI : MonoBehaviour
{
    [Header("Referências")]
    public GameObject warningPanel;
    public TextMeshProUGUI warningText; // trocou de Text pra TextMeshProUGUI

    public void ShowWarning()
    {
        warningPanel.SetActive(true);
        if (warningText != null)
        {
            warningText.text = "??? está vindo... Se esconda.";
        }
    }

    public void HideWarning()
    {
        warningPanel.SetActive(false);
    }
}