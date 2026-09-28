using UnityEngine;
using UnityEngine.UI;
using EternalClash.Character;

namespace EternalClash.UI
{
    public class SkillButtonController : MonoBehaviour
    {
        [SerializeField] private Button[] skillButtons;

        private void Update()
        {
            if (HealthSystem.PlayerDead)
            {
                DisableSkills();
            }
        }

        public void DisableSkills()
        {
            foreach (Button button in skillButtons)
            {
                if (button != null)
                    button.interactable = false;
            }
        }

        public void EnableSkills()
        {
            foreach (Button button in skillButtons)
            {
                if (button != null)
                    button.interactable = true;
            }
        }
    }
}
