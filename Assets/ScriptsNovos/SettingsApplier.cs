using UnityEngine;

/// <summary>
/// Aplica as configurações do menu de verdade.
/// - Ao iniciar o jogo, aplica o que estiver salvo (automático, não precisa de objeto na cena).
/// - Ao escolher uma opção na tela de Configurações, os métodos públicos abaixo são chamados
///   pelo evento "On Changed" dos grupos de opção.
/// </summary>
public class SettingsApplier : MonoBehaviour
{
    // Mesmas chaves usadas nos OptionGroup do menu
    public const string KeyDisplay = "gfx_display";   // 0 = Janela, 1 = Tela Cheia
    public const string KeyQuality = "gfx_quality";   // 0 = Baixa, 1 = Média, 2 = Alta

    // Nomes EXATOS dos níveis em Edit > Project Settings > Quality
    // (na ordem: Baixa, Média, Alta)
    static readonly string[] QualityNames = { "Low", "Medium", "High" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplySavedOnStartup()
    {
        ApplyDisplayMode(PlayerPrefs.GetInt(KeyDisplay, 1));
        ApplyQuality(PlayerPrefs.GetInt(KeyQuality, 1));
    }

    // Ligado no "On Changed" do Display_Options (Dynamic int)
    public void SetDisplayMode(int index)
    {
        ApplyDisplayMode(index);
    }

    // Ligado no "On Changed" do Quality_Options (Dynamic int)
    public void SetQuality(int index)
    {
        ApplyQuality(index);
    }

    // Ligado no "On Changed" do Brightness_Boxes (Dynamic int). Recebe o nível de 1 a 5.
    public void SetBrightness(int level)
    {
        BrightnessController.Apply(level);
    }

    static void ApplyDisplayMode(int index)
    {
        if (index == 0)
        {
            // Janela: 80% da largura da tela, em 16:9
            int w = Mathf.RoundToInt(Screen.currentResolution.width * 0.8f);
            int h = Mathf.RoundToInt(w * 9f / 16f);
            Screen.SetResolution(w, h, FullScreenMode.Windowed);
            Debug.Log("[Config] Modo de exibição: Janela " + w + "x" + h);
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Debug.Log("[Config] Modo de exibição: Tela Cheia");
        }
    }

    static void ApplyQuality(int index)
    {
        index = Mathf.Clamp(index, 0, QualityNames.Length - 1);

        string[] names = QualitySettings.names;
        int level = System.Array.IndexOf(names, QualityNames[index]);

        if (level < 0)
        {
            // Não achou pelo nome: usa a posição na lista, para não quebrar
            level = Mathf.Clamp(index, 0, names.Length - 1);
            Debug.LogWarning("[Config] Nível de qualidade '" + QualityNames[index] +
                             "' não existe em Project Settings > Quality. Usando '" + names[level] + "'.");
        }

        QualitySettings.SetQualityLevel(level, true);
        Debug.Log("[Config] Qualidade gráfica: " + names[level]);
    }
}
