using UnityEngine;
using EternalClash.Combat;

namespace EternalClash.Character
{
    public class AnimationEventReceiver : MonoBehaviour
    {
        private BasicAttack basicAttack;

        private void Awake()
        {
            basicAttack = GetComponent<BasicAttack>();
        }

        // Goi tu Animation Event
        public void AttackDamageEvent()
        {
            if (basicAttack != null)
            {
                basicAttack.DealDamage();
            }
        }

        public void SkillStartEvent()
        {
            Debug.Log("Skill start");
        }

        public void SkillEndEvent()
        {
            Debug.Log("Skill end");
        }
    }
}
