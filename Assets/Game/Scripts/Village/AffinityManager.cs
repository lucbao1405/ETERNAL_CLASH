using UnityEngine;
using EternalClash.Core.Save;

namespace EternalClash.Village
{
    public class AffinityManager : MonoBehaviour
    {
        public static AffinityManager Instance { get; private set; }

        public int AffinityPoints { get; private set; }
        public System.Collections.Generic.List<AffinityMilestoneData> Milestones { get; private set; } = new System.Collections.Generic.List<AffinityMilestoneData>();

        public event System.Action<int> OnAffinityPointsChanged;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitDefaultMilestones();
        }

        private void InitDefaultMilestones()
        {
            Milestones.Add(new AffinityMilestoneData
            {
                letterId = "letter_01",
                requiredPoints = 50,
                letterTitle = "Lá Thư Đầu Tiên",
                letterSender = "Village Elder",
                permanentBuffDescription = "+5 MaxHP vĩnh viễn"
            });
            Milestones.Add(new AffinityMilestoneData
            {
                letterId = "letter_02",
                requiredPoints = 150,
                letterTitle = "Cảm Ơn Hiệp Sĩ",
                letterSender = "Merchant",
                permanentBuffDescription = "+5% EXP bonus"
            });
            Milestones.Add(new AffinityMilestoneData
            {
                letterId = "letter_03",
                requiredPoints = 300,
                letterTitle = "Bình Hoa Ngày Xuân",
                letterSender = "Blacksmith Apprentice",
                permanentBuffDescription = "+3 Potion Heal"
            });
            Milestones.Add(new AffinityMilestoneData
            {
                letterId = "letter_04",
                requiredPoints = 500,
                letterTitle = "Đêm Trăng Tâm Sự",
                letterSender = "Mysterious Stranger",
                permanentBuffDescription = "+1% Lucky Drop"
            });
            Milestones.Add(new AffinityMilestoneData
            {
                letterId = "letter_05",
                requiredPoints = 800,
                letterTitle = "Lời Hẹn Ước",
                letterSender = "Childhood Friend",
                permanentBuffDescription = "+10 MaxHP"
            });
        }

        public void AddAffinityPoints(int amount)
        {
            AffinityPoints += amount;
            CheckMilestones();
            OnAffinityPointsChanged?.Invoke(AffinityPoints);
            SyncSave();
            Debug.Log($"[AFFINITY] +{amount} -> Total: {AffinityPoints}");
        }

        public bool IsLetterUnlocked(string letterId)
        {
            var data = SaveManager.Instance?.Data;
            return data != null && data.unlockedLetters != null && data.unlockedLetters.Contains(letterId);
        }

        private void CheckMilestones()
        {
            foreach (var milestone in Milestones)
            {
                if (!IsLetterUnlocked(milestone.letterId) && AffinityPoints >= milestone.requiredPoints)
                {
                    UnlockLetter(milestone);
                }
            }
        }

        private void UnlockLetter(AffinityMilestoneData milestone)
        {
            var data = SaveManager.Instance?.Data;
            if (data == null) return;
            if (data.unlockedLetters == null)
                data.unlockedLetters = new System.Collections.Generic.List<string>();

            data.unlockedLetters.Add(milestone.letterId);
            SaveCoordinator.RequestSave();
            ApplyPermanentBuff(milestone);
            Debug.Log($"[AFFINITY] Letter unlocked: {milestone.letterTitle}");
        }

        private void ApplyPermanentBuff(AffinityMilestoneData milestone)
        {
            var stats = PlayerStatSystem.Instance;
            if (stats == null) return;

            switch (milestone.letterId)
            {
                case "letter_01":
                    stats.ApplyArmorTierBonus(1);
                    break;
                case "letter_02":
                    stats.AddIntelligence(1);
                    break;
                case "letter_03":
                    stats.AddVitality(1);
                    break;
                case "letter_04":
                    stats.AddLuck(1);
                    break;
                case "letter_05":
                    stats.ApplyArmorTierBonus(1);
                    break;
            }

            Debug.Log($"[AFFINITY] Buff applied: {milestone.permanentBuffDescription}");
        }

        public void LoadFromSave(SaveData data)
        {
            AffinityPoints = data.affinityPoints;
        }

        private void SyncSave()
        {
            if (SaveManager.Instance?.Data != null)
            {
                SaveManager.Instance.Data.affinityPoints = AffinityPoints;
                SaveCoordinator.RequestSave();
            }
        }
    }
}
