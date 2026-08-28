using UnityEngine;

namespace EternalClash.Skill
{
    public abstract class SkillBase : MonoBehaviour
    {
        public string skillName;
        public float cooldown = 5f;

        // Skill must be ready immediately when the object is created.
        // Keep the initial value on the field because derived skills override Awake().
        protected float timer = 0f;

        protected virtual void Awake()
        {
            timer = 0f;
        }

        public float CooldownRemaining
        {
            get
            {
                return Mathf.Max(timer, 0);
            }
        }

        public bool CanUse()
        {
            return timer <= 0;
        }

        public void UseSkill()
        {
            Debug.Log("[SkillBase] Try use skill: " + skillName);

            if (!CanUse())
            {
                Debug.Log("[SkillBase] Cooldown active: " + timer);
                return;
            }

            Debug.Log("[SkillBase] Execute skill: " + skillName);
            Execute();

            // Start cooldown only after the skill actually executes
            timer = cooldown;
        }

        protected virtual void Update()
        {
            if (timer > 0)
                timer -= Time.deltaTime;
        }

        protected abstract void Execute();
    }
}
