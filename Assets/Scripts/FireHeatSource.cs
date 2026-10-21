using UnityEngine;

public class FireHeatSource : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerWarmth warmth = other.GetComponentInParent<PlayerWarmth>();
            if (warmth != null) warmth.SetNearFire(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerWarmth warmth = other.GetComponentInParent<PlayerWarmth>();
            if (warmth != null) warmth.SetNearFire(false);
        }
    }
}