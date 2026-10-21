using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Brilho do jogo (URP).
/// Cria sozinho, ao iniciar o jogo, um Volume global com Color Adjustments (Post Exposure)
/// que sobrevive à troca de cena. Não precisa colocar nada na cena.
///
/// Níveis de 1 a 5 (as 5 caixinhas do menu). O nível 2 é o padrão e NÃO altera nada
/// (a imagem fica como você montou a cena). Níveis 3 a 5 clareiam e o nível 1 escurece.
///
/// Requisito: a câmera do jogo precisa estar com "Post Processing" ligado
/// (Inspector da câmera > Rendering > Post Processing).
/// </summary>
public class BrightnessController : MonoBehaviour
{
    // Mesma chave usada pelo BrightnessBoxes
    public const string KeyBrightness = "gfx_brightness";
    public const int DefaultLevel = 2;

    // Post Exposure (em "stops": +1 = o dobro de luz) para os níveis 1, 2, 3, 4 e 5.
    // Se achar muito forte ou fraco, mexa aqui.
    static readonly float[] PostExposure = { -0.7f, 0f, 0.6f, 1.2f, 1.8f };

    static BrightnessController instance;
    ColorAdjustments adjustments;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;

        GameObject go = new GameObject("BrightnessVolume");
        DontDestroyOnLoad(go);
        go.AddComponent<BrightnessController>(); // o Awake abaixo registra a instância e aplica o brilho salvo
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        Volume volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f; // acima dos Volumes da cena

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        // false = só o Post Exposure será sobrescrito (contraste, saturação etc. continuam os da cena)
        adjustments = profile.Add<ColorAdjustments>(false);
        volume.sharedProfile = profile;

        Apply(PlayerPrefs.GetInt(KeyBrightness, DefaultLevel));
    }

    /// <summary>Aplica o nível de brilho (1 a 5).</summary>
    public static void Apply(int level)
    {
        if (instance == null || instance.adjustments == null) return;

        level = Mathf.Clamp(level, 1, PostExposure.Length);
        float value = PostExposure[level - 1];

        // No nível padrão não sobrescreve nada: a cena fica exatamente como está
        instance.adjustments.postExposure.overrideState = (level != DefaultLevel);
        instance.adjustments.postExposure.value = value;

        Debug.Log("[Config] Brilho: nível " + level + " (Post Exposure " + value + ")");
    }
}
