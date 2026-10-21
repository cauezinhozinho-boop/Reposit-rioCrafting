using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Botão do menu com "pincelada" branca atrás do texto quando selecionado.
/// Mouse, teclado e controle passam todos pela seleção do EventSystem,
/// então nunca aparecem dois botões destacados ao mesmo tempo.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class BrushButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    [Header("Referências")]
    [Tooltip("Filho 'Brush' com a pincelada. Precisa estar ACIMA do texto na Hierarchy (fica atrás).")]
    [SerializeField] Image brush;
    [Tooltip("Texto do botão (TextMeshPro ou Text legado).")]
    [SerializeField] Graphic label;

    [Header("Cores do texto")]
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color selectedColor = Color.black;

    [Header("Opcional (abas de Configurações)")]
    [Tooltip("Se preenchido, a pincelada continua ligada enquanto o foco estiver DENTRO deste objeto " +
             "(ex: ao mexer num slider do AudioContent). Quando o foco vai para outro botão, apaga. " +
             "Use nas abas: Áudio -> AudioContent, Gráficos -> GraphicsContent, etc.")]
    [FormerlySerializedAs("keepHighlightWhileActive")]
    [SerializeField] GameObject keepHighlightWhileInside;

    /// <summary>
    /// Quando true, o hover do mouse NÃO muda a seleção.
    /// Usado durante o fade de "New Game" / "Continue" pra travar o destaque
    /// no botão que foi clicado.
    /// </summary>
    public static bool SelectionLocked { get; set; } = false;

    Selectable selectable;
    bool selected;
    bool shown;

    void Reset()
    {
        // Preenche sozinho ao adicionar o componente no Inspector
        Transform b = transform.Find("Brush");
        if (b != null) brush = b.GetComponent<Image>();

        label = GetComponentInChildren<TMP_Text>();
        if (label == null) label = GetComponentInChildren<Text>();
    }

    void Awake()
    {
        selectable = GetComponent<Selectable>();
        if (brush != null) brush.raycastTarget = false;
    }

    void OnEnable()
    {
        selected = false;
        Apply(Compute());
    }

    void Update()
    {
        // Barato (poucos botões): reavalia todo frame para a aba acompanhar o foco.
        bool on = Compute();
        if (on != shown) Apply(on);
    }

    bool Compute()
    {
        if (selected) return true;

        // Aba: continua acesa só enquanto o foco estiver dentro do conteúdo dela
        if (keepHighlightWhileInside != null)
        {
            EventSystem es = EventSystem.current;
            if (es != null)
            {
                GameObject current = es.currentSelectedGameObject;
                if (current != null && current.transform.IsChildOf(keepHighlightWhileInside.transform))
                    return true;
            }
        }

        return false;
    }

    void Apply(bool on)
    {
        shown = on;
        if (brush != null) brush.enabled = on;
        if (label != null) label.color = on ? selectedColor : normalColor;
    }

    public void OnSelect(BaseEventData eventData) { selected = true; }
    public void OnDeselect(BaseEventData eventData) { selected = false; }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Se a seleção está trancada, o hover não troca o destaque
        if (SelectionLocked) return;

        // Mouse por cima = seleciona (mesmo caminho do teclado/controle)
        if (selectable != null && !selectable.IsInteractable()) return;
        EventSystem.current.SetSelectedGameObject(gameObject);
    }
}