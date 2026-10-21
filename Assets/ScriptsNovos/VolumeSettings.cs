using UnityEngine;
using UnityEngine.Audio;

public enum VolumeChannel
{
    Master,   // Volume Geral  -> AudioListener.volume (pega TODOS os sons, até os que não passam pelo mixer)
    Music,    // Volume da Música  -> parâmetro exposto "MusicVolume" no AudioMixer
    Sfx       // Volume dos efeitos sonoros -> parâmetro exposto "SFXVolume" no AudioMixer
}

/// <summary>
/// Salva e aplica os volumes do menu de Áudio.
/// Valores de 0 a 1 (0% a 100%).
/// </summary>
public static class VolumeSettings
{
    // Nomes EXATOS dos parâmetros expostos no MainAudioMixer
    public const string MusicParam = "MusicVolume";
    public const string SfxParam = "SFXVolume";

    // Valor inicial quando o jogador nunca mexeu (o layout mostra 50%)
    public const float DefaultValue = 0.5f;

    public static string Key(VolumeChannel channel)
    {
        return "vol_" + channel;
    }

    public static float Load(VolumeChannel channel)
    {
        return PlayerPrefs.GetFloat(Key(channel), DefaultValue);
    }

    public static void Save(VolumeChannel channel, float value)
    {
        PlayerPrefs.SetFloat(Key(channel), value);
    }

    public static void Apply(VolumeChannel channel, float value, AudioMixer mixer)
    {
        value = Mathf.Clamp01(value);

        switch (channel)
        {
            case VolumeChannel.Master:
                AudioListener.volume = value;
                break;

            case VolumeChannel.Music:
                SetMixer(mixer, MusicParam, value);
                break;

            case VolumeChannel.Sfx:
                SetMixer(mixer, SfxParam, value);
                break;
        }
    }

    /// <summary>Aplica os 3 volumes salvos (chamado quando o menu abre).</summary>
    public static void ApplyAll(AudioMixer mixer)
    {
        foreach (VolumeChannel channel in System.Enum.GetValues(typeof(VolumeChannel)))
            Apply(channel, Load(channel), mixer);
    }

    static void SetMixer(AudioMixer mixer, string parameter, float value)
    {
        if (mixer == null) return;

        // Log10(0) = -infinito, então limita o mínimo (-80 dB)
        float db = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;

        if (!mixer.SetFloat(parameter, db))
            Debug.LogWarning("[Config] Parâmetro '" + parameter + "' não encontrado no AudioMixer. Ele foi exposto e renomeado?");
    }
}
