using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Caixinhas de brilho (ex: 5 caixas). Clicar na caixa N define o nível N:
/// as N primeiras mostram o sprite marcado (X) e as outras o vazio.
/// Cada FILHO deste objeto é uma caixa e precisa ter Image + Button.
/// A ordem dos filhos na Hierarchy é a ordem das caixas (esquerda -> direita).
/// </summary>
public class BrightnessBoxes : MonoBehaviour
{
    [SerializeField] Sprite emptySprite;
    [SerializeField] Sprite checkedSprite;

    [Tooltip("Nível inicial quando não há nada salvo (1 a número de caixas).")]
    [SerializeField] int defaultLevel = 2;

    [Tooltip("Chave do PlayerPrefs. Vazio = não salva.")]
    [SerializeField] string prefsKey = "gfx_brightness";

    [Tooltip("Disparado quando o jogador muda o nível (recebe o nível, de 1 a N).")]
    public OptionGroup.IntEvent onChanged;

    Image[] boxes;

    void Awake()
    {
        int count = transform.childCount;
        boxes = new Image[count];

        for (int i = 0; i < count; i++)
        {
            Transform t = transform.GetChild(i);
            boxes[i] = t.GetComponent<Image>();

            Button b = t.GetComponent<Button>();
            if (b != null)
            {
                int level = i + 1; // captura segura para o lambda
                b.onClick.AddListener(() => SetLevel(level, true));
            }
        }
    }

    void OnEnable()
    {
        int saved = string.IsNullOrEmpty(prefsKey)
            ? defaultLevel
            : PlayerPrefs.GetInt(prefsKey, defaultLevel);

        SetLevel(saved, false);
    }

    /// <summary>Define o nível (1 a N). notify = true salva e dispara o evento.</summary>
    public void SetLevel(int level, bool notify)
    {
        if (boxes == null || boxes.Length == 0) return;

        level = Mathf.Clamp(level, 1, boxes.Length);

        for (int i = 0; i < boxes.Length; i++)
        {
            if (boxes[i] != null)
                boxes[i].sprite = (i < level) ? checkedSprite : emptySprite;
        }

        if (notify)
        {
            if (!string.IsNullOrEmpty(prefsKey))
                PlayerPrefs.SetInt(prefsKey, level);

            if (onChanged != null) onChanged.Invoke(level);
        }
    }
}
