using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Fundo animado do menu.
/// Vai no mesmo objeto do VideoPlayer + RawImage.
/// A RawImage começa escondida: enquanto o vídeo não entrega o primeiro quadro (ou se der erro),
/// o PNG de fundo que está atrás continua aparecendo, sem flash preto.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
[RequireComponent(typeof(RawImage))]
public class BackgroundVideo : MonoBehaviour
{
    VideoPlayer player;
    RawImage image;

    void Awake()
    {
        player = GetComponent<VideoPlayer>();
        image = GetComponent<RawImage>();

        image.enabled = false;

        player.sendFrameReadyEvents = true;
        player.frameReady += OnFirstFrame;
        player.errorReceived += OnError;
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.frameReady -= OnFirstFrame;
            player.errorReceived -= OnError;
        }
    }

    void OnFirstFrame(VideoPlayer source, long frameIdx)
    {
        image.enabled = true;

        // Só precisamos do primeiro quadro
        source.sendFrameReadyEvents = false;
        source.frameReady -= OnFirstFrame;
    }

    void OnError(VideoPlayer source, string message)
    {
        Debug.LogWarning("[Menu] Erro no vídeo de fundo: " + message + " (o PNG de fundo continua aparecendo)");
    }
}
