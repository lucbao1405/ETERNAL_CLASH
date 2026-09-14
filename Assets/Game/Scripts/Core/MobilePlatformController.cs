using System.Collections.Generic;
using EternalClash.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EternalClash.Core
{
    /// <summary>
    /// Hanh vi rieng cho dien thoai. GameBootstrap tu gan vao "GameSystems".
    ///
    ///  - Man hinh: giu sang khi dang danh (Battle), o Town de may tu tat theo cai dat.
    ///  - Nut Back Android (Escape tren PC/Editor):
    ///      1. Dong bang dang mo tren cung (Setting, Trang bi, Shop, xac nhan nang cap, hoi thoai...)
    ///      2. Battle: bat / tat Pause (bo qua khi dang hien ket qua tran)
    ///      3. Town khong co bang nao mo: dua app xuong nen (chuan Android, khong thoat han)
    /// </summary>
    public sealed class MobilePlatformController : MonoBehaviour
    {
        private const string BattleSceneName = "Battle";

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplySleepTimeout(SceneManager.GetActiveScene().name);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
                ApplySleepTimeout(scene.name);
        }

        private static bool IsBattle(string sceneName)
        {
            return string.Equals(sceneName, BattleSceneName, System.StringComparison.OrdinalIgnoreCase);
        }

        // ------------------------------------------------------------------
        // Tat man hinh
        // ------------------------------------------------------------------

        private static void ApplySleepTimeout(string sceneName)
        {
            Screen.sleepTimeout = IsBattle(sceneName)
                ? SleepTimeout.NeverSleep
                : SleepTimeout.SystemSetting;
        }

        // ------------------------------------------------------------------
        // Nut Back
        // ------------------------------------------------------------------

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                HandleBack();
        }

        private void HandleBack()
        {
            if (TryCloseTopPanel())
                return;

            if (IsBattle(SceneManager.GetActiveScene().name))
            {
                TogglePause();
                return;
            }

            MoveAppToBackground();
        }

        private static void TogglePause()
        {
            // Dang hien ruong / Win / Lose: Back khong lam gi, tranh mo Pause chong len.
            StageManager stage = StageManager.Instance;
            if (stage != null && stage.CurrentState != StageManager.StageState.Running)
                return;

            BattlePopupController popup = BattlePopupController.GetForBattle();
            if (popup == null)
                return;

            if (popup.IsSettingsOpen)
                popup.CloseSettings();
            else
                popup.OpenSettings();
        }

        /// <summary>
        /// Tim tat ca bang dang mo va dong bang nam tren cung (ve sau cung trong
        /// Canvas). Dong bang con truoc bang cha, vd hop xac nhan truoc Shop Tho Ren.
        /// </summary>
        private static bool TryCloseTopPanel()
        {
            var candidates = new List<(Component panel, System.Action close)>();

            foreach (DialogueManager dialogue in FindObjectsOfType<DialogueManager>())
            {
                if (dialogue.IsOpen)
                    candidates.Add((dialogue, dialogue.CloseDialogue));
            }

            foreach (BlacksmithUpgradeConfirmUI confirm in FindObjectsOfType<BlacksmithUpgradeConfirmUI>())
                candidates.Add((confirm, confirm.Close));

            foreach (ShopPanelAnimator shop in FindObjectsOfType<ShopPanelAnimator>())
            {
                if (shop.State == ShopPanelAnimator.PanelState.Opened ||
                    shop.State == ShopPanelAnimator.PanelState.Opening)
                    candidates.Add((shop, shop.Close));
            }

            foreach (SmoothSlide slide in FindObjectsOfType<SmoothSlide>())
            {
                if (slide.IsOpen)
                    candidates.Add((slide, slide.ClosePanel));
            }

            if (candidates.Count == 0)
                return false;

            int top = 0;
            for (int i = 1; i < candidates.Count; i++)
            {
                if (CompareDrawOrder(candidates[i].panel.transform, candidates[top].panel.transform) > 0)
                    top = i;
            }

            candidates[top].close();
            return true;
        }

        /// <summary>
        /// So sanh thu tu ve UI: &gt; 0 nghia la a ve sau (nam tren) b.
        /// Con ve sau cha; anh em sibling index lon hon ve sau.
        /// </summary>
        private static int CompareDrawOrder(Transform a, Transform b)
        {
            List<int> pathA = SiblingPath(a);
            List<int> pathB = SiblingPath(b);

            int length = Mathf.Min(pathA.Count, pathB.Count);
            for (int i = 0; i < length; i++)
            {
                if (pathA[i] != pathB[i])
                    return pathA[i].CompareTo(pathB[i]);
            }

            return pathA.Count.CompareTo(pathB.Count);
        }

        private static List<int> SiblingPath(Transform t)
        {
            var path = new List<int>();
            for (Transform current = t; current != null; current = current.parent)
                path.Add(current.GetSiblingIndex());
            path.Reverse();
            return path;
        }

        private static void MoveAppToBackground()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                activity.Call<bool>("moveTaskToBack", true);
            }
#endif
        }
    }
}
