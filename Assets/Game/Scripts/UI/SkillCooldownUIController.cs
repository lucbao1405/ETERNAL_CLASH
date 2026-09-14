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

            UpdateCooldown(shieldManagerTime(), shieldOverlay, shieldText);
            UpdateCooldown(skillManager.GetPotionCooldown(), potionOverlay, potionText);
            UpdateCooldown(skillManager.GetChargeCooldown(), chargeOverlay, chargeText);
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

        private float shieldManagerTime()
        {
            return skillManager.GetShieldCooldown();
        }

        private void UpdateCooldown(float time, Image overlay, TMP_Text text)
        {
            bool cooldown = time > 0;

            if (overlay != null)
                overlay.fillAmount = cooldown ? 1 : 0;

            if (text != null)
                text.text = cooldown ? time.ToString("0.0") : "";
        }
    }
}
