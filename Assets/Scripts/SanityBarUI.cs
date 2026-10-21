using UnityEngine;
using UnityEngine.UI;

public class SanityBarUI : MonoBehaviour
{
    public Slider slider;

    public void SetSanity(float current, float max)
    {
        if (slider != null)
        {
            slider.maxValue = max;
            slider.value = current;
        }
    }
}