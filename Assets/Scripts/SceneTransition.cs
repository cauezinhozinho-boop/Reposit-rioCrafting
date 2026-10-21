using UnityEngine;

public class SceneTransition : MonoBehaviour
{
    [Header("Configura��o")]
    public string sceneToLoad;
    public string spawnPointName = "SpawnPoint";

    private bool playerInRange = false;

    void Update()
    {

        if (PauseManager.IsPaused || CraftingNpc.IsMenuOpen)
            return;

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            SceneLoader.Instance.LoadScene(sceneToLoad, spawnPointName);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            InteractionUI.Instance.ShowPrompt();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            InteractionUI.Instance.HidePrompt();
        }
    }
}