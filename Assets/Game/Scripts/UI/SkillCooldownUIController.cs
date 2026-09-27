using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalClash.Skill;

namespace EternalClash.UI
{
    public class SkillCooldownUIController : MonoBehaviour
    {
        [SerializeField] private SkillManager skillManager;

        [Header("Skill UI")]
        [SerializeField] private Image shieldOverlay;
        [SerializeField] private Image potionOverlay;
        [SerializeField] private Image chargeOverlay;

        [SerializeField] private TMP_Text shieldText;
        [SerializeField] private TMP_Text potionText;
        [SerializeField] private TMP_Text chargeText;

        private void Start()
        {
            BindRuntimePlayer();
        }

        private void Update()
        {
            if (skillManager == null)
            {
                BindRuntimePlayer();
                if (skillManager == null)
                    return;
            }

            UpdateCooldown(skillManager.GetShieldCooldown(), skillManager.GetShieldCooldownMax(), shieldOverlay, shieldText);
            UpdateCooldown(skillManager.GetPotionCooldown(), skillManager.GetPotionCooldownMax(), potionOverlay, potionText);
            UpdateCooldown(skillManager.GetChargeCooldown(), skillManager.GetChargeCooldownMax(), chargeOverlay, chargeText);
        }

        private void BindRuntimePlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            skillManager = player.GetComponent<SkillManager>();
            if (skillManager != null)
                skillManager.EnsureSkills();
        }

        private void UpdateCooldown(float time, float maxTime, Image overlay, TMP_Text text)
        {
            bool cooldown = time > 0f;

            if (overlay != null)
            {
                // Overlay gan dan theo thoi gian con lai: 1 -> 0 khi het cooldown.
                overlay.fillAmount = cooldown && maxTime > 0f
                    ? Mathf.Clamp01(time / maxTime)
                    : 0f;
            }

            if (text != null)
                text.text = cooldown ? time.ToString("0.0") : "";
        }
    }
}
