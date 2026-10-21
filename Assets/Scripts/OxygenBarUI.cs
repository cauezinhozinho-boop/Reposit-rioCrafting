using UnityEngine;
using UnityEngine.UI;

public class OxygenBarUI : MonoBehaviour
{
    public Slider slider;

    public void SetOxygen(float current, float max)
    {
        slider.maxValue = max;
        slider.value = current;
    }
}