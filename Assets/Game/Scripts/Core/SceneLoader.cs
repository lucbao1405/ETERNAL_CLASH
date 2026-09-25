using UnityEngine;
using UnityEngine.SceneManagement;

namespace EternalClash.Core
{
    public class SceneLoader : MonoBehaviour
    {
        public static void LoadScene(string sceneName)
        {
            // HealthSystem.Die() dat Time.timeScale = 0 khi Player chet va khong noi
            // nao dat lai. Neu khong khoi phuc o day thi scene moi (Town) se dung
            // hinh: hoi mau theo thoi gian khong chay, hoat anh dung im.
            Time.timeScale = 1f;
            AudioListener.pause = false;

            // Doi scene co the ton vai giay tren may yeu: ghi het save dang cho truoc.
            EternalClash.Core.Save.SaveCoordinator.Flush();

            // Mo dan den -> tai scene -> mo dan lo man hinh moi.
            SceneFader.Transition(sceneName);
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
