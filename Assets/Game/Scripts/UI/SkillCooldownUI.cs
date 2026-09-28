using UnityEngine;
using TMPro;
using EternalClash.Skill;
using EternalClash.Village;
using UnityEngine.UI;
using EternalClash.Data;

namespace EternalClash.UI
{
    public class SkillCooldownUI : MonoBehaviour
    {
        public TMP_Text cooldownText;
        public Image cooldownOverlay;
        public Button skillButton;

        public enum SkillType
        {
            Charge,
            Shield,
            Potion
        }

        public SkillType skillType;

        private SkillManager skillManager;
        private bool equippedIconApplied;

        private void Awake()
        {
            if (skillButton == null)
                skillButton = GetComponent<Button>();

            if (cooldownOverlay == null)
            {
                var overlayTransform = transform.Find("CooldownOverlay");
                if (overlayTransform != null)
                    cooldownOverlay = overlayTransform.GetComponent<Image>();
            }

            if (cooldownText == null)
            {
                var textTransform = transform.Find("CooldownText") ?? transform.Find("Timer");
                if (textTransform != null)
                    cooldownText = textTransform.GetComponent<TMP_Text>();
                if (cooldownText == null)
                    cooldownText = GetComponentInChildren<TMP_Text>();
            }
        }

        private void Start()
        {
            if (skillButton == null)
                skillButton = GetComponent<Button>();

            if (skillButton != null)
                skillButton.onClick.AddListener(OnClickSkill);

            FindRuntimePlayer();
            ApplyEquippedIcon();
        }

        private void ApplyEquippedIcon()
        {
            if (skillButton == null)
                return;

            string branchId = skillType == SkillType.Charge ? "charge" :
                skillType == SkillType.Shield ? "shield" : null;
            SkillData selectedData = SkillLoadout.GetSelectedData(branchId);
            if (selectedData == null)
                return;

            string iconName = skillType == SkillType.Charge ? "Kiem" :
                skillType == SkillType.Shield ? "Khien" : "Thuoc";
            Transform iconTransform = skillButton.transform.Find(iconName);
            if (iconTransform == null)
                iconTransform = skillButton.transform.Find("Icon");
            Image buttonImage = iconTransform != null
                ? iconTransform.GetComponent<Image>()
                : null;
            if (buttonImage != null && selectedData != null && selectedData.icon != null)
            {
                buttonImage.sprite = selectedData.icon;
                buttonImage.preserveAspect = true;
                equippedIconApplied = true;
            }
        }

        private void OnDestroy()
        {
            if (skillButton != null)
                skillButton.onClick.RemoveListener(OnClickSkill);
        }

        public void OnClickSkill()
        {
            if (skillManager == null)
                FindRuntimePlayer();

            if (skillManager == null) return;

            switch (skillType)
            {
                case SkillType.Charge:
                    skillManager.UseCharge();
                    break;
                case SkillType.Shield:
                    skillManager.UseShield();
                    break;
                case SkillType.Potion:
                    skillManager.UsePotion();
                    break;
            }
        }

        private void FindRuntimePlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player == null)
                return;

            skillManager = player.GetComponent<SkillManager>();
        }

        private void Update()
        {
            if (skillManager == null)
            {
                FindRuntimePlayer();
                return;
            }

            if (!equippedIconApplied)
                ApplyEquippedIcon();

            float cd = 0f;
            float maxCd = 1f;

            switch (skillType)
            {
                case SkillType.Charge:
                    cd = skillManager.GetChargeCooldown();
                    maxCd = skillManager.chargeSkill != null ? skillManager.chargeSkill.cooldown : 3.0f;
                    break;

                case SkillType.Shield:
                    cd = skillManager.GetShieldCooldown();
                    maxCd = skillManager.shieldSkill != null ? skillManager.shieldSkill.cooldown : 5.0f;
                    break;

                case SkillType.Potion:
                    cd = skillManager.GetPotionCooldown();
                    maxCd = AlchemistUpgradeSystem.Instance != null
                        ? AlchemistUpgradeSystem.Instance.GetCooldownValue()
                        : 15f;
                    break;
            }

            bool isOnCooldown = cd > 0.05f;

            // Bi day lui: khoa ca 3 nut. Phu kin nut (khong dem nguoc) de nguoi choi
            // thay ro la dang bi khoa, bam cung khong ra skill.
            bool isLocked = skillManager.AreSkillsLocked;

            if (cooldownText != null)
                cooldownText.text = isOnCooldown ? cd.ToString("0.0") : "";

            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(isOnCooldown || isLocked);
                cooldownOverlay.fillAmount = isOnCooldown
                    ? (cd / Mathf.Max(maxCd, 0.01f))
                    : (isLocked ? 1f : 0f);
            }

            if (skillButton != null)
            {
                skillButton.interactable = !isOnCooldown && !isLocked;
            }
        }
    }
}
