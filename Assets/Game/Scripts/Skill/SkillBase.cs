using System;
using UnityEngine;
using EternalClash.World;

namespace EternalClash.Skill
{
    public abstract class SkillBase : MonoBehaviour
    {
        public event Action<string> SkillExecuted;
        public event Action<SkillBase> SkillExecutedSource;

        public string skillName;
        public float cooldown = 5f;

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
            if (timer > 0) return false;

            var health = GetComponentInParent<EternalClash.Character.HealthSystem>();
            if (health != null && health.IsDead) return false;

            var combatState = GetComponentInParent<EternalClash.Combat.PlayerCombatStateMachine>();
            if (combatState != null && !combatState.CanUseSkill) return false;

            return true;
        }

        public void UseSkill()
        {
            if (!CanUse())
            {
                Debug.Log("[SkillBase] Blocked: " + skillName + " (cooldown or knockback/dead)");
                return;
            }

            Debug.Log("[SkillBase] Execute skill: " + skillName);
            Execute();

            SkillExecuted?.Invoke(skillName);
            SkillExecutedSource?.Invoke(this);
            EternalClash.Audio.GameAudio.PlaySkill(skillName);

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
