using UnityEngine;
using EternalClash.Skill;

namespace EternalClash.Combat
{
    public enum CounterResult
    {
        Fail,
        Block,
        Perfect
    }

    public class CounterSkillResolver : MonoBehaviour
    {
        [SerializeField] private int shieldReflectDamage = 5;
        [SerializeField] private float perfectCounterWindow = 0.35f;

        public CounterResult TryShieldCounter(ShieldSkill shield, EnemyAttackTimingController enemy)
        {
            if (shield == null || enemy == null)
                return CounterResult.Fail;

            if (!enemy.IsPreparingAttack(perfectCounterWindow))
                return CounterResult.Fail;

            if (enemy.State == EnemyAttackTimingController.AttackState.Warning)
            {
                Debug.Log("[COUNTER] Perfect Shield Block");
                return CounterResult.Perfect;
            }

            Debug.Log("[COUNTER] Shield Block");
            return CounterResult.Block;
        }

        public void ApplyReflect(GameObject enemy)
        {
            if (enemy == null)
                return;

            CombatDamageResolver.Instance?.DealDamage(
                enemy,
                shieldReflectDamage,
                DamageSource.Reflect
            );
        }

        public void ApplyPerfectCounter(GameObject enemy, EnemyAttackTimingController timing)
        {
            if (enemy == null || timing == null)
                return;

            ApplyReflect(enemy);
            timing.CancelAttack();
            timing.ApplyStun(perfectCounterWindow);

            Debug.Log("[COUNTER] Perfect Counter - Reflect + Stun");
        }
    }
}
