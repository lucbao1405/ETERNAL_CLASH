using UnityEngine;
using EternalClash.Skill;

namespace EternalClash.Player
{
    public class InputController : MonoBehaviour
    {
        private SkillManager skillManager;

        private void Awake()
        {
            skillManager = GetComponent<SkillManager>();
        }

        private void Update()
        {
            if (skillManager == null)
                return;

            if (Input.GetKeyDown(KeyCode.Q))
            {
                skillManager.UseDash();
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                skillManager.UseShield();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                skillManager.UsePotion();
            }
        }
    }
}
