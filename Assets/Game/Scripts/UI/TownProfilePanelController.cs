using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using EternalClash.Core.Save;

namespace EternalClash.UI
{
    /// <summary>
    /// Dong bo thong tin mo rong trong panel ho so (Canvas/Man_Hinh_Khac/Avt):
    ///   ThongTin/Hang_Stage/Value                         -> SaveData.stageLevel (stage cao nhat da vuot qua)
    ///   ThongTin/Hang_Thuoc/{Healing,Cooldown,Defense}/Value -> SaveData.abilities.{healing,cooldown,defense}Level
    ///   ThongTin/Hang_Ela/Value                           -> SaveData.elaAffinity
    ///   Badge_VIP/Text                                    -> isVipActive "VIP" / hasRemovedAds "NO ADS", an khi khong co
    /// Tim doi tuong theo ten giong TownStatPanelController nen KHONG can gan gi trong Inspector.
    /// </summary>
    public sealed class TownProfilePanelController : MonoBehaviour
    {
        private const string TownSceneName = "Town";

        private TMP_Text stageText;
        private TMP_Text healText;
        private TMP_Text cooldownText;
        private TMP_Text defenseText;
        private TMP_Text elaText;
        private GameObject vipBadge;
        private TMP_Text vipText;
        private SaveManager subscribedSaveManager;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            // Giong TownStatPanelController: controller bi huy cung scene, nen phai
            // bat sceneLoaded de tu tao lai moi lan roi Town.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureInTownScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureInTownScene();
        }

        private static void EnsureInTownScene()
        {
            if (FindObjectOfType<TownProfilePanelController>() != null)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                !string.Equals(scene.name, TownSceneName, StringComparison.OrdinalIgnoreCase))
                return;

            new GameObject("TownProfilePanelController (Runtime)")
                .AddComponent<TownProfilePanelController>();
        }

        private void Start()
        {
            Subscribe();
            Refresh();
        }

        private void OnDestroy()
        {
            if (subscribedSaveManager != null)
            {
                subscribedSaveManager.SaveLoaded -= OnSaveDataChanged;
                subscribedSaveManager.SaveChanged -= OnSaveDataChanged;
            }
        }

        private void Subscribe()
        {
            SaveManager saveManager = SaveManager.Instance;
            if (saveManager == subscribedSaveManager)
                return;

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

        private void OnSaveDataChanged(SaveData _) => Refresh();

        private void Refresh()
        {
            if (stageText == null || elaText == null || vipBadge == null)
                ResolveTargets();

            SaveData data = SaveManager.Instance != null ? SaveManager.Instance.Data : null;

            if (stageText != null)
                stageText.text = Mathf.Max(1, data != null ? data.stageLevel : 1).ToString();

            if (healText != null)
                healText.text = "Lv " + (data != null ? data.abilities.healingLevel : 0);
            if (cooldownText != null)
                cooldownText.text = "Lv " + (data != null ? data.abilities.cooldownLevel : 0);
            if (defenseText != null)
                defenseText.text = "Lv " + (data != null ? data.abilities.defenseLevel : 0);

            if (elaText != null)
                elaText.text = (data != null ? data.elaAffinity : 0).ToString();

            if (vipBadge != null)
            {
                bool vip = data != null && data.isVipActive;
                bool noAds = data != null && data.hasRemovedAds;
                vipBadge.SetActive(vip || noAds);
                if (vipText != null)
                    vipText.text = vip ? "VIP" : "NO ADS";
            }
        }

        private void ResolveTargets()
        {
            // Phai di tu "Man_Hinh_Khac" truoc vi Down_Panel cung co nut ten "Avt".
            Transform screens = FindSceneTransform("Man_Hinh_Khac");
            if (screens == null)
                return;

            Transform panel = FindDescendant(screens, "Avt");
            if (panel == null)
                return;

            Transform info = FindDescendant(panel, "ThongTin");
            if (info == null)
                return;

            stageText = ChildText(FindDescendant(info, "Hang_Stage"), "Value");
            elaText = ChildText(FindDescendant(info, "Hang_Ela"), "Value");

            Transform potions = FindDescendant(info, "Hang_Thuoc");
            if (potions != null)
            {
                healText = ChildText(FindDescendant(potions, "Healing"), "Value");
                cooldownText = ChildText(FindDescendant(potions, "Cooldown"), "Value");
                defenseText = ChildText(FindDescendant(potions, "Defense"), "Value");
            }

            Transform badge = FindDescendant(panel, "Badge_VIP");
            if (badge != null)
            {
                vipBadge = badge.gameObject;
                vipText = badge.GetComponentInChildren<TMP_Text>(true);
            }
        }

        private static TMP_Text ChildText(Transform row, string childName)
        {
            if (row == null)
                return null;
            Transform value = row.Find(childName);
            return value != null ? value.GetComponent<TMP_Text>() : null;
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
    }
}
