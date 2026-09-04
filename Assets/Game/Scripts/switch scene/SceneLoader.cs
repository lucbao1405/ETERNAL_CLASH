using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string gameplaySceneName = "Gameplay";

    public void LoadGameplay()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }
}