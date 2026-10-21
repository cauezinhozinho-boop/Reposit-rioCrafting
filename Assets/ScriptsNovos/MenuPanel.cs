using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Coloque em cada painel do menu (Principal, Carregar Jogo, Configurações).
/// Ao abrir o painel, seleciona o primeiro botão. Se o foco se perder
/// (clique no vazio, por exemplo), devolve para o último item selecionado.
/// </summary>
public class MenuPanel : MonoBehaviour
{
    [SerializeField] Selectable firstSelected;

    GameObject lastSelected;

    void OnEnable()
    {
        lastSelected = firstSelected != null ? firstSelected.gameObject : null;
        StartCoroutine(SelectFirst());
    }

    IEnumerator SelectFirst()
    {
        // Espera 1 frame para o EventSystem estar pronto
        yield return null;

        EventSystem es = EventSystem.current;
        if (es == null || lastSelected == null) yield break;

        // Limpa antes, para o OnSelect sempre disparar (mesmo se já era o selecionado)
        es.SetSelectedGameObject(null);
        es.SetSelectedGameObject(lastSelected);
    }

    void Update()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return;

        GameObject current = es.currentSelectedGameObject;

        if (current != null)
        {
            if (current.transform.IsChildOf(transform))
                lastSelected = current;
        }
        else if (lastSelected != null && lastSelected.activeInHierarchy)
        {
            es.SetSelectedGameObject(lastSelected);
        }
    }
}
