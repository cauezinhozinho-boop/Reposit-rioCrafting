using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Deixa TODA a UI do jogo proporcional ao tamanho da tela.
/// Roda sozinho ao iniciar o jogo e a cada cena carregada (não precisa de objeto na cena).
///
/// Para cada Canvas "de tela" (Overlay ou Screen Space - Camera) ele configura o Canvas Scaler como:
///   Scale With Screen Size, referência definida abaixo, Match 0.5.
/// Canvas de mundo (World Space, como os de cima de itens ou inimigos) não são mexidos.
///
/// Se um Canvas tiver o componente UICanvasScaleOverride, ele usa o valor
/// do componente em vez do padrão. Útil pra telas que foram desenhadas em
/// tamanho diferente (ex: o menu foi feito em 1920x1080, o HUD em 1280x720).
/// </summary>
public static class UIScaleNormalizer
{
    // =========================================================
    // AJUSTE DE TAMANHO DA UI (padrão global)
    // =========================================================
    //
    // Menor = UI MAIOR | Maior = UI MENOR
    //
    // 1280 x  720 = UI ~1.5x maior  <- padrão atual (bom pro HUD)
    // 1536 x  864 = UI ~1.25x maior
    // 1920 x 1080 = tamanho original
    //
    // Pra mudar só um Canvas específico (ex: o menu), adicione o componente
    // UICanvasScaleOverride nele e defina outro valor.
    //
    public static readonly Vector2 DefaultReferenceResolution = new Vector2(1280f, 720f);

    // 0 = acompanha só a largura | 1 = acompanha só a altura | 0.5 = meio termo
    public const float DefaultMatch = 0.5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        NormalizeAll();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        NormalizeAll();
    }

    public static void NormalizeAll()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include
        );

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];

            // Só canvas raiz (os aninhados seguem o canvas de cima)
            if (!canvas.isRootCanvas)
                continue;

            // Canvas de mundo não é UI de tela
            if (canvas.renderMode == RenderMode.WorldSpace)
                continue;

            // Valor padrão
            Vector2 referenceResolution = DefaultReferenceResolution;
            float match = DefaultMatch;

            // Override por canvas, se existir
            UICanvasScaleOverride over =
                canvas.GetComponent<UICanvasScaleOverride>();

            if (over != null)
            {
                referenceResolution = over.referenceResolution;
                match = over.match;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            bool added = false;

            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                added = true;
            }

            bool changed =
                added ||
                scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                scaler.referenceResolution != referenceResolution ||
                scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight ||
                !Mathf.Approximately(scaler.matchWidthOrHeight, match);

            if (!changed)
                continue;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = match;

            Debug.Log("[UI] Canvas '" + canvas.name + "' ajustado para Scale With Screen Size (" +
                      referenceResolution.x + " x " + referenceResolution.y + ").");
        }
    }
}