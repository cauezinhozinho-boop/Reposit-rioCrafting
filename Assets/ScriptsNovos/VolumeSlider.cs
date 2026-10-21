using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Slider de volume do menu de Áudio (valores de 0 a 1).
/// - Atualiza o texto no formato "Título (50%)".
/// - Salva o valor (PlayerPrefs) e aplica o volume de verdade, via VolumeSettings.
/// </summary>
[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    [Header("Canal")]
    [Tooltip("Master = Volume Geral | Music = Volume da Música | Sfx = Volume dos efeitos sonoros")]
    [SerializeField] VolumeChannel channel = VolumeChannel.Master;

    [Header("Texto")]
    [SerializeField] string title = "Volume Geral";
    [SerializeField] TMP_Text label;

    [Header("Áudio")]
    [Tooltip("MainAudioMixer. Necessário para Music e Sfx (o Master não usa, mas pode deixar preenchido).")]
    [SerializeField] AudioMixer mixer;

    Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    void OnEnable()
    {
        slider.onValueChanged.AddListener(OnChanged);
        slider.SetValueWithoutNotify(VolumeSettings.Load(channel));
        RefreshLabel(slider.value);
    }

    void OnDisable()
    {
        slider.onValueChanged.RemoveListener(OnChanged);
    }

    void OnChanged(float value)
    {
        RefreshLabel(value);
        VolumeSettings.Apply(channel, value, mixer);
        VolumeSettings.Save(channel, value);
    }

    void RefreshLabel(float value)
    {
        if (label != null)
            label.text = title + " (" + Mathf.RoundToInt(value * 100f) + "%)";
    }
}
