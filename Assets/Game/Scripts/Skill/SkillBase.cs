using UnityEngine;

namespace EternalClash.Skill
{
    public abstract class SkillBase : MonoBehaviour
    {
        public string skillName;
        public float cooldown = 5f;

        protected float timer;

        public bool CanUse()
        {
            return timer <= 0;
        }

        public void UseSkill()
        {
            if (!CanUse())
                return;

            timer = cooldown;
            Execute();
        }

        protected virtual void Update()
        {
            if (timer > 0)
                timer -= Time.deltaTime;
        }

        protected abstract void Execute();
    }
}
