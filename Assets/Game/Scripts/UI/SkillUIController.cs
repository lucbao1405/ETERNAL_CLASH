using UnityEngine;
using EternalClash.Skill;

namespace EternalClash.UI
{
    public class SkillUIController : MonoBehaviour
    {
        private SkillManager skillManager;

        private void Start()
        {
            FindPlayerSkillManager();
        }

        private void FindPlayerSkillManager()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player == null)
            {
                Debug.LogWarning("[UI] Cannot find Player");
                return;
            }

            skillManager = player.GetComponent<SkillManager>();

            if (skillManager == null)
            {
                Debug.LogWarning("[UI] Player has no SkillManager");
                return;
            }

            Debug.Log("[UI] Runtime Player SkillManager found: " + player.name);
        }

        public void UseCharge()
        {
            Debug.Log("[UI] Use Charge Button Pressed");

            if (skillManager == null)
                FindPlayerSkillManager();

            if (skillManager != null)
                skillManager.UseCharge();
        }

        public void UseShield()
        {
            Debug.Log("[UI] Use Shield Button Pressed");

            if (skillManager == null)
                FindPlayerSkillManager();

            if (skillManager != null)
                skillManager.UseShield();
        }

        public void UsePotion()
        {
            Debug.Log("[UI] Use Potion Button Pressed");

            if (skillManager == null)
                FindPlayerSkillManager();

            if (skillManager != null)
                skillManager.UsePotion();
        }
    }
}
