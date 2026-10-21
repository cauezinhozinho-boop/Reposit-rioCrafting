using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PersistentEventSystem : MonoBehaviour
{
    private static PersistentEventSystem instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RemoveDuplicateEventSystems();
    }

    private void RemoveDuplicateEventSystems()
    {
        EventSystem persistentEventSystem = GetComponent<EventSystem>();
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>();

        foreach (EventSystem eventSystem in eventSystems)
        {
            if (eventSystem != persistentEventSystem)
                Destroy(eventSystem.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }
}
