using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// Faz o slider de "efeitos sonoros" funcionar sem você precisar abrir cada AudioSource.
/// Todo AudioSource que NÃO tem grupo de saída (Output) passa a tocar no grupo SFX do mixer.
/// Quem já tem grupo (ex: a música no grupo Music) não é mexido.
///
/// Coloque em um objeto na RAIZ da cena do menu (não dentro do Canvas).
/// Ele sobrevive à troca de cena e confere os sons a cada troca de cena e a cada poucos segundos.
/// </summary>
public class AudioRouter : MonoBehaviour
{
    static AudioRouter instance;

    [SerializeField] AudioMixer mixer;
    [Tooltip("Caminho do grupo no mixer.")]
    [SerializeField] string sfxGroupPath = "Master/SFX";
    [Tooltip("De quantos em quantos segundos procura sons novos (ex: criados durante o jogo).")]
    [SerializeField] float scanInterval = 2f;

    AudioMixerGroup sfxGroup;
    float nextScan;

    void Awake()
    {
        // Só pode existir um (o menu pode ser carregado de novo ao voltar do jogo)
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (mixer != null)
        {
            AudioMixerGroup[] groups = mixer.FindMatchingGroups(sfxGroupPath);
            if (groups != null && groups.Length > 0)
                sfxGroup = groups[0];
        }

        if (sfxGroup == null)
            Debug.LogWarning("[Config] Grupo '" + sfxGroupPath + "' não encontrado no AudioMixer. O grupo SFX foi criado dentro do Master?");

        SceneManager.sceneLoaded += OnSceneLoaded;
        Scan();
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Scan();
    }

    void Update()
    {
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + scanInterval;
            Scan();
        }
    }

    void Scan()
    {
        if (sfxGroup == null) return;

        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include);

        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i].outputAudioMixerGroup == null)
                sources[i].outputAudioMixerGroup = sfxGroup;
        }
    }
}
