using UnityEngine;
using UnityEngine.UI;

public class WarmthBarUI : MonoBehaviour
{
    public Slider slider;

    public void SetWarmth(float current, float max)
    {
        slider.maxValue = max;
        slider.value = current;
    }
}