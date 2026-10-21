using UnityEngine;

public class BossZoneTrigger : MonoBehaviour
{
    [Header("Configuração")]
    public bool triggerOnlyOnce = true;

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerOnlyOnce && hasTriggered) return;

            BossEncounterManager.Instance.TriggerEncounter();
            hasTriggered = true;
        }
    }
}