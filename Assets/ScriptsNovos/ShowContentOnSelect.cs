using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Coloque nos itens da coluna esquerda (Áudio, Gráficos, Controles).
/// Ao selecionar o item, mostra o conteúdo dele e esconde os irmãos.
/// </summary>
public class ShowContentOnSelect : MonoBehaviour, ISelectHandler
{
    [Tooltip("Conteúdo que este item mostra (ex: AudioContent).")]
    [SerializeField] GameObject content;

    [Tooltip("Pai dos conteúdos (ex: o objeto 'Content'). Se vazio, usa o pai do conteúdo.")]
    [SerializeField] Transform contentRoot;

    public void OnSelect(BaseEventData eventData)
    {
        if (content == null) return;

        Transform root = contentRoot != null ? contentRoot : content.transform.parent;

        if (root != null)
        {
            foreach (Transform child in root)
                child.gameObject.SetActive(child.gameObject == content);
        }
        else
        {
            content.SetActive(true);
        }
    }
}
