using UnityEngine;

public class FireDangerZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        EnemyAISimple enemy = other.GetComponent<EnemyAISimple>();

        if (enemy != null)
        {
            enemy.EnterFireZone(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        EnemyAISimple enemy = other.GetComponent<EnemyAISimple>();

        if (enemy != null)
        {
            enemy.ExitFireZone(this);
        }
    }
}