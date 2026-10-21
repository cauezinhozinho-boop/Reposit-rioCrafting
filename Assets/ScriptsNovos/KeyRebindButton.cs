using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class KeyRebindButton : MonoBehaviour
{
    [Tooltip("Qual ação este botão rebinda.")]
    [SerializeField] GameAction action = GameAction.Interact;

    [Tooltip("Texto que mostra a tecla atual.")]
    [SerializeField] TMP_Text label;

    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    void OnEnable()
    {
        KeyBindings.OnBindingChanged  += HandleChanged;
        KeyBindings.OnRebindCancelled += HandleCancelled;
        Refresh();
    }

    void OnDisable()
    {
        KeyBindings.OnBindingChanged  -= HandleChanged;
        KeyBindings.OnRebindCancelled -= HandleCancelled;
    }

    void HandleChanged(GameAction changed)
    {
        if (changed == action) Refresh();
    }

    void HandleCancelled(GameAction cancelled)
    {
        if (cancelled == action) Refresh();
    }

    void OnClick()
    {
        KeyBindings.StartRebind(action);
        if (label != null) label.text = "...";
    }

    void Refresh()
    {
        if (label != null)
            label.text = KeyBindings.Get(action).DisplayName();
    }
}