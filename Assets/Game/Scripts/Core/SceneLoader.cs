using UnityEngine;
using UnityEngine.SceneManagement;

namespace EternalClash.Core
{
    public class SceneLoader : MonoBehaviour
    {
        public static void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public static void LoadBattle()
        {
            LoadScene("Battle");
        }

        public static void LoadTown()
        {
            LoadScene("Town");
        }
    }
}
