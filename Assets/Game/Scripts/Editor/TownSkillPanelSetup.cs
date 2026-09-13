using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EternalClash.UI;
using EternalClash.Data;

namespace EternalClash.EditorTools
{
    public static class TownSkillPanelSetup
    {
        [MenuItem("Tools/Setup Town Skill Panel", false, 100)]
        public static void SetupSkillPanel()
        {
            // Tìm GameObject Skill trong scene
            GameObject skillPanelObj = GameObject.Find("Skill");
            if (skillPanelObj == null)
            {
                var allTransforms = Object.FindObjectsOfType<Transform>(true);
                foreach (var t in allTransforms)
                {
                    if (t.name == "Skill" && t.parent != null && t.parent.name == "Man_Hinh_Khac")
                    {
                        skillPanelObj = t.gameObject;
                        break;
                    }
                }
            }

            if (skillPanelObj == null)
            {
                Debug.LogError("[TownSkillPanelSetup] Không tìm thấy GameObject 'Skill' trong Scene!");
                return;
            }

            // Gắn hoặc lấy TownSkillPanelController
            var controller = skillPanelObj.GetComponent<TownSkillPanelController>();
            if (controller == null)
            {
                controller = skillPanelObj.AddComponent<TownSkillPanelController>();
                Debug.Log("[TownSkillPanelSetup] Đã thêm component TownSkillPanelController vào GameObject 'Skill'.");
            }

            // Tự động tìm kiếm các GameObject con & khởi tạo dữ liệu
            controller.AutoFindReferences();

            // Tìm và gán các file SkillData của người dùng đã tạo trong Assets
            var guids = AssetDatabase.FindAssets("t:SkillData");
            List<SkillData> chargeSkills = new List<SkillData>();
            List<SkillData> shieldSkills = new List<SkillData>();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SkillData data = AssetDatabase.LoadAssetAtPath<SkillData>(path);
                if (data == null) continue;

                if (data.branchType == SkillBranchType.Charge || path.Contains("/Charge/"))
                {
                    chargeSkills.Add(data);
                }
                else if (data.branchType == SkillBranchType.Shield || path.Contains("/Shield/"))
                {
                    shieldSkills.Add(data);
                }
            }

            // Sắp xếp tăng dần theo Tier (Tier 1 thấp nhất)
            chargeSkills.Sort((a, b) => a.tier.CompareTo(b.tier));
            shieldSkills.Sort((a, b) => a.tier.CompareTo(b.tier));

            if (chargeSkills.Count > 0)
                controller.AssignSkillDataAssets("charge", chargeSkills);
            if (shieldSkills.Count > 0)
                controller.AssignSkillDataAssets("shield", shieldSkills);

            // Tắt BlacksmithShopUI cũ nếu có
            var blacksmith = skillPanelObj.GetComponent<BlacksmithShopUI>();
            if (blacksmith != null)
            {
                blacksmith.enabled = false;
                Debug.Log("[TownSkillPanelSetup] Đã tắt BlacksmithShopUI thừa trên GameObject 'Skill'.");
            }

            EditorUtility.SetDirty(skillPanelObj);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("<color=green>[TownSkillPanelSetup] Cài đặt TownSkillPanelController thành công 100%!</color>");
            Selection.activeGameObject = skillPanelObj;
        }

        [MenuItem("Tools/Create Sample Skill Data Assets", false, 101)]
        public static void CreateSampleSkillDataAssets()
        {
            string chargeDir = "Assets/Game/Data/Skills/Charge";
            string shieldDir = "Assets/Game/Data/Skills/Shield";

            if (!Directory.Exists(chargeDir))
                Directory.CreateDirectory(chargeDir);
            if (!Directory.Exists(shieldDir))
                Directory.CreateDirectory(shieldDir);

            // --- CHARGE SKILLS ---
            // Tier 1 (Mặc định mở)
            CreateOrUpdateSkillAsset(chargeDir, "Charge_PowerRush.asset",
                "charge_01", "Power Rush", SkillBranchType.Charge, 1, true, "Mặc định mở khóa.",
                "Charges forward with explosive speed, increasing damage dealt by +25% on impact.",
                "Deals +25% bonus impact damage and knocks smaller enemies airborne.",
                SpecialEffectType.DamageBonus, 0.25f, 0f, 15);

            // Tier 2 (Mặc định mở)
            CreateOrUpdateSkillAsset(chargeDir, "Charge_IronVanguard.asset",
                "charge_02", "Iron Vanguard", SkillBranchType.Charge, 2, true, "Mặc định mở khóa.",
                "Gain hyper-armor during charge. Stuns the first obstacle or enemy struck for 1.2s.",
                "Stuns target for 1.2s and grants full immunity to crowd control during dash.",
                SpecialEffectType.Stun, 1.2f, 1.2f, 0);

            // Tier 3 (Mặc định khóa)
            CreateOrUpdateSkillAsset(chargeDir, "Charge_CrushingMomentum.asset",
                "charge_03", "Crushing Momentum", SkillBranchType.Charge, 3, false, "Mở khóa khi hoàn thành Stage 2.",
                "Inflicts heavy knockback on all enemies along the path and clears minor projectiles.",
                "Knocks back enemies with high force (3.5x force) and deflects weak projectiles.",
                SpecialEffectType.Knockback, 3.5f, 0.5f, 10);

            // Tier 4 (Mặc định khóa)
            CreateOrUpdateSkillAsset(chargeDir, "Charge_OverdriveSurge.asset",
                "charge_04", "Overdrive Surge", SkillBranchType.Charge, 4, false, "Mở khóa khi đạt cấp độ cao hơn.",
                "Reduces Charge cooldown by 1.0s and increases player movement speed by 30% for 2s after charging.",
                "Cooldown -1.0s, grants +30% movement speed burst for 2 seconds upon arrival.",
                SpecialEffectType.CooldownReduction, 1.0f, 2.0f, 0);

            // --- SHIELD SKILLS ---
            // Tier 1 (Mặc định mở)
            CreateOrUpdateSkillAsset(shieldDir, "Shield_IronAegis.asset",
                "shield_01", "Iron Aegis", SkillBranchType.Shield, 1, true, "Mặc định mở khóa.",
                "Strengthens the barrier, raising damage reduction to 90% and prolonging block window by 0.3s.",
                "Damage reduction increased to 90% (base 80%) with extended 1.3s duration.",
                SpecialEffectType.DamageReduction, 0.90f, 1.3f, 0);

            // Tier 2 (Mặc định mở)
            CreateOrUpdateSkillAsset(shieldDir, "Shield_SpikeRetaliation.asset",
                "shield_02", "Spike Retaliation", SkillBranchType.Shield, 2, true, "Mặc định mở khóa.",
                "Reflects an additional 10 DMG (total 15 DMG) back to the attacking enemy.",
                "Reflects 15 DMG back to attacker upon receiving direct melee or ranged strike.",
                SpecialEffectType.ReflectDamage, 15f, 0f, 0);

            // Tier 3 (Mặc định khóa)
            CreateOrUpdateSkillAsset(shieldDir, "Shield_BulwarkStance.asset",
                "shield_03", "Bulwark Stance", SkillBranchType.Shield, 3, false, "Mở khóa khi hoàn thành Stage 2.",
                "Heals 6% max HP when successfully withstanding a direct heavy blow while shielding.",
                "Restores 6% of maximum health on successful block (cooldown 3s per trigger).",
                SpecialEffectType.HealOnBlock, 0.06f, 0f, 0);

            // Tier 4 (Mặc định khóa)
            CreateOrUpdateSkillAsset(shieldDir, "Shield_CounterShockwave.asset",
                "shield_04", "Counter Shockwave", SkillBranchType.Shield, 4, false, "Mở khóa khi đạt cấp độ cao hơn.",
                "Emits a shockwave upon blocking that knocks back and slows adjacent enemies by 40%.",
                "Unleashes a radial blast that pushes enemies back and slows them by 40% for 2s.",
                SpecialEffectType.Shockwave, 40f, 2.0f, 5);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[TownSkillPanelSetup] Đã tạo thành công 8 file SkillData mẫu (sắp xếp Tier 1..4, 2 skill đầu mặc định mở)!</color>");
        }

        private static void CreateOrUpdateSkillAsset(string folder, string fileName,
            string id, string skillName, SkillBranchType branch, int tier, bool isDefaultUnlocked, string unlockReq,
            string desc, string specialDesc, SpecialEffectType effectType, float effectVal, float effectDur, int bonusDmg)
        {
            string path = Path.Combine(folder, fileName).Replace("\\", "/");
            SkillData asset = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SkillData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.skillId = id;
            asset.skillName = skillName;
            asset.branchType = branch;
            asset.tier = tier;
            asset.isDefaultUnlocked = isDefaultUnlocked;
            asset.unlockRequirement = unlockReq;
            asset.description = desc;
            asset.specialEffectDescription = specialDesc;
            asset.specialEffectType = effectType;
            asset.effectValue = effectVal;
            asset.effectDuration = effectDur;
            asset.bonusDamage = bonusDmg;

            EditorUtility.SetDirty(asset);
        }
    }
}
