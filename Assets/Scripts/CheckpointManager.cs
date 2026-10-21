using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    private Vector3 checkpointPosition;
    private string checkpointScene;
    private bool hasCheckpoint = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCheckpoint(Vector3 position)
    {
        checkpointPosition = position;
        checkpointScene = SceneManager.GetActiveScene().name;
        hasCheckpoint = true;
    }

    public Vector3 GetCheckpoint()
    {
        return checkpointPosition;
    }

    public string GetCheckpointScene()
    {
        return checkpointScene;
    }

    public bool HasCheckpoint()
    {
        return hasCheckpoint;
    }

    public void ClearCheckpoint()
    {
        checkpointPosition = Vector3.zero;
        checkpointScene = "";
        hasCheckpoint = false;
    }
}