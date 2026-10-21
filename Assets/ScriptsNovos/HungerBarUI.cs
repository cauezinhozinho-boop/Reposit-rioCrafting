using UnityEngine;
using UnityEngine.UI;

public class HungerBarUI : MonoBehaviour
{
    public Slider slider;

    public void SetHunger(float current, float max)
    {
        slider.maxValue = max;
        slider.value = current;
    }
}