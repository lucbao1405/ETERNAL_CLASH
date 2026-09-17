using UnityEngine;
using EternalClash.Combat;

namespace EternalClash.Character
{
    public class AnimationEventReceiver : MonoBehaviour
    {
        private BasicAttackSystem basicAttackSystem;

        private void Awake()
        {
            basicAttackSystem = GetComponent<BasicAttackSystem>();
        }

        // Goi tu Animation Event
        public void AttackDamageEvent()
        {
            if (basicAttackSystem != null)
            {
                basicAttackSystem.AnimationDealDamage();
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
