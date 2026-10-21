using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Grupo de opções do tipo "escolha uma" (ex: Baixa / Média / Alta).
/// Cada FILHO deste objeto é uma opção e precisa ter:
///   - um Button
///   - um filho chamado "Brush" (Image com a pincelada)
///   - um texto TextMeshPro
/// A ordem dos filhos na Hierarchy é o índice da opção (0, 1, 2...).
/// A opção escolhida mostra a pincelada e o texto preto.
/// </summary>
public class OptionGroup : MonoBehaviour
{
    [System.Serializable]
    public class IntEvent : UnityEvent<int> { }

    [Tooltip("Opção escolhida quando não há nada salvo (0 = primeiro filho).")]
    [SerializeField] int defaultIndex = 0;

    [Tooltip("Chave do PlayerPrefs (ex: gfx_quality). Vazio = não salva.")]
    [SerializeField] string prefsKey = "";

    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color selectedColor = Color.black;

    [Tooltip("Disparado quando o jogador escolhe uma opção (recebe o índice).")]
    public IntEvent onChanged;

    struct Item
    {
        public Button button;
        public Image brush;
        public Graphic label;
    }

    Item[] items;

    void Awake()
    {
        int count = transform.childCount;
        items = new Item[count];

        for (int i = 0; i < count; i++)
        {
            Transform t = transform.GetChild(i);
            Item it = new Item();

            it.button = t.GetComponent<Button>();

            Transform b = t.Find("Brush");
            if (b != null)
            {
                it.brush = b.GetComponent<Image>();
                if (it.brush != null) it.brush.raycastTarget = false;
            }

            it.label = t.GetComponentInChildren<TMP_Text>();
            items[i] = it;

            if (it.button != null)
            {
                int index = i; // captura segura para o lambda
                it.button.onClick.AddListener(() => Select(index, true));
            }
        }
    }

    void OnEnable()
    {
        int saved = string.IsNullOrEmpty(prefsKey)
            ? defaultIndex
            : PlayerPrefs.GetInt(prefsKey, defaultIndex);

        Select(saved, false);
    }

    /// <summary>Escolhe uma opção. notify = true salva e dispara o evento.</summary>
    public void Select(int index, bool notify)
    {
        if (items == null || items.Length == 0) return;

        index = Mathf.Clamp(index, 0, items.Length - 1);

        for (int i = 0; i < items.Length; i++)
        {
            bool on = (i == index);
            if (items[i].brush != null) items[i].brush.enabled = on;
            if (items[i].label != null) items[i].label.color = on ? selectedColor : normalColor;
        }

        if (notify)
        {
            if (!string.IsNullOrEmpty(prefsKey))
                PlayerPrefs.SetInt(prefsKey, index);

            if (onChanged != null) onChanged.Invoke(index);
        }
    }
}
