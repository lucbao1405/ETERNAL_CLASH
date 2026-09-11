using System;
using EternalClash.Core.Save;
using EternalClash.Village;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EternalClash.UI
{
    /// <summary>
    /// Keeps the Town and Battle identity strips bound to the persisted player
    /// name and level. The presentation is scene-local; game state remains in SaveData.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerIdentityUIController : MonoBehaviour
    {
        private const string TownSceneName = "Town";
        private const string BattleSceneName = "Battle";
        private const string DefaultPlayerName = "Ga Con";

        private TMP_Text nameText;
        private TMP_Text levelText;
        private SaveManager subscribedSaveManager;
        private PlayerStatSystem subscribedPlayerStats;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureForActiveScene();
        }

        private static void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            EnsureForActiveScene();
        }

        private static void EnsureForActiveScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (!string.Equals(sceneName, TownSceneName, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(sceneName, BattleSceneName, StringComparison.OrdinalIgnoreCase))
                return;

            if (FindObjectOfType<PlayerIdentityUIController>() != null)
                return;

            new GameObject("PlayerIdentityUIController (Runtime)")
                .AddComponent<PlayerIdentityUIController>();
        }

        private void Start()
        {
            ResolveTargets();
            Subscribe();
            Refresh();
        }

        private void OnDestroy()
        {
            if (subscribedSaveManager != null)
                subscribedSaveManager.SaveLoaded -= OnSaveDataChanged;
            if (subscribedSaveManager != null)
                subscribedSaveManager.SaveChanged -= OnSaveDataChanged;
            if (subscribedPlayerStats != null)
                subscribedPlayerStats.OnStatsChanged -= Refresh;
        }

        private void Subscribe()
        {
            SaveManager saveManager = SaveManager.Instance;
            if (saveManager != subscribedSaveManager)
            {
                if (subscribedSaveManager != null)
                {
                    subscribedSaveManager.SaveLoaded -= OnSaveDataChanged;
                    subscribedSaveManager.SaveChanged -= OnSaveDataChanged;
                }

                subscribedSaveManager = saveManager;
                if (subscribedSaveManager != null)
                {
                    subscribedSaveManager.SaveLoaded += OnSaveDataChanged;
                    subscribedSaveManager.SaveChanged += OnSaveDataChanged;
                }
            }

            PlayerStatSystem playerStats = PlayerStatSystem.Instance;
            if (playerStats == subscribedPlayerStats)
                return;

            if (subscribedPlayerStats != null)
                subscribedPlayerStats.OnStatsChanged -= Refresh;
            subscribedPlayerStats = playerStats;
            if (subscribedPlayerStats != null)
                subscribedPlayerStats.OnStatsChanged += Refresh;
        }

        private void OnSaveDataChanged(SaveData _) => Refresh();

        private void Refresh()
        {
            if (nameText == null || levelText == null)
                ResolveTargets();

            SaveData data = SaveManager.Instance?.Data;
            string playerName = data != null && !string.IsNullOrWhiteSpace(data.playerName)
                ? data.playerName.Trim()
                : DefaultPlayerName;
            int level = Mathf.Max(1, data != null ? data.level : 1);

            if (nameText != null)
                nameText.text = playerName;
            if (levelText != null)
                levelText.text = level.ToString();
        }

        private void ResolveTargets()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            Transform root = string.Equals(sceneName, TownSceneName, StringComparison.OrdinalIgnoreCase)
                ? FindSceneTransform("Down_Panel")
                : FindSceneTransform("BottomPanel");
            if (root == null)
                return;

            Transform infoRoot = string.Equals(sceneName, TownSceneName, StringComparison.OrdinalIgnoreCase)
                ? FindDescendant(root, "Stat")
                : FindDescendant(root, "PlayerInfo");
            if (infoRoot == null)
                return;

            nameText = FindText(infoRoot, "Name");
            levelText = FindText(infoRoot, "Level");
        }

        private static Transform FindSceneTransform(string name)
        {
            Scene scene = SceneManager.GetActiveScene();
            foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (transform.gameObject.scene != scene || !string.Equals(transform.name, name, StringComparison.Ordinal))
                    continue;
                return transform;
            }
            return null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
                return null;
            if (string.Equals(root.name, name, StringComparison.Ordinal))
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindDescendant(root.GetChild(i), name);
                if (match != null)
                    return match;
            }
            return null;
        }

        private static TMP_Text FindText(Transform root, string name)
        {
            Transform target = FindDescendant(root, name);
            return target != null ? target.GetComponentInChildren<TMP_Text>(true) : null;
        }
    }
}
