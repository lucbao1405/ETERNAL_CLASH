using UnityEngine;
using UnityEngine.UI;
using EternalClash.Skill;
using EternalClash.Character;

namespace EternalClash.UI
{
    public class SkillUIController : MonoBehaviour
    {
        private SkillManager skillManager;
        public Button[] skillButtons;

        private void Start()
        {
            FindPlayerSkillManager();
        }

        private void Update()
        {
            if (HealthSystem.PlayerDead)
            {
                DisableButtons();
            }
        }

        private void DisableButtons()
        {
            SetSkillButtonsInteractable(false);
        }

        public void SetSkillButtonsInteractable(bool interactable)
        {
            if (skillButtons == null)
                return;

            foreach (Button button in skillButtons)
            {
                if (button != null)
                    button.interactable = interactable;
            }
        }

        private void FindPlayerSkillManager()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player == null) return;

            skillManager = player.GetComponent<SkillManager>();
        }

        public void UseCharge()
        {
            if (HealthSystem.PlayerDead) return;
            if (skillManager == null) FindPlayerSkillManager();
            skillManager?.UseCharge();
        }

        public void UseShield()
        {
            if (HealthSystem.PlayerDead) return;
            if (skillManager == null) FindPlayerSkillManager();
            skillManager?.UseShield();
        }

        public void UsePotion()
        {
            if (HealthSystem.PlayerDead) return;
            if (skillManager == null) FindPlayerSkillManager();
            skillManager?.UsePotion();
        }
    }
}
