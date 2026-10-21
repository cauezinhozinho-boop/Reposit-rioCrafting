using UnityEngine;

/// <summary>
/// Coloque num Canvas pra ele usar um tamanho de referência DIFERENTE do padrão.
/// Útil quando a UI daquela tela foi desenhada em outra resolução.
///
/// Ex: o HUD fica bom em 1280x720, mas o menu foi feito em 1920x1080.
/// Adicione esse componente no Canvas do menu e deixe em 1920x1080.
/// </summary>
[DisallowMultipleComponent]
public class UICanvasScaleOverride : MonoBehaviour
{
    [Tooltip("Resolução de referência deste Canvas. Menor = UI maior, maior = UI menor.")]
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);

    [Range(0f, 1f)]
    [Tooltip("0 = acompanha largura | 1 = acompanha altura | 0.5 = meio termo")]
    public float match = 0.5f;
}