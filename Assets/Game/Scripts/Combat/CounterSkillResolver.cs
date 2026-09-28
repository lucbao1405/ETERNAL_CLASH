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
        [SerializeField] private AudioClip perfectCounterSfx;

        public CounterResult TryShieldCounter(ShieldSkill shield, EnemyAttackTimingController enemy)
        {
            if (shield == null || enemy == null)
                return CounterResult.Fail;

            if (!enemy.IsPreparingAttack(perfectCounterWindow))
                return CounterResult.Fail;

            if (enemy.State == EnemyAttackTimingController.AttackState.Warning)
            {
                Debug.Log("[COUNTER] Perfect Shield Block");
                PlayCounterFeedback(CounterResult.Perfect, enemy.transform.position);
                return CounterResult.Perfect;
            }

            Debug.Log("[COUNTER] Shield Block");
            PlayCounterFeedback(CounterResult.Block, enemy.transform.position);
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

            HitStopImpactSystem.Instance?.HeavyHit();
            CombatVFXController.Instance?.PlayHitEffect(enemy.transform.position);
            CombatVFXController.Instance?.Shake(0.15f);

            if (perfectCounterSfx != null)
                AudioSource.PlayClipAtPoint(perfectCounterSfx, enemy.transform.position);

            Debug.Log("[COUNTER] Perfect Counter - Reflect + Stun");
        }

        private void PlayCounterFeedback(CounterResult result, Vector3 position)
        {
            if (result == CounterResult.Perfect)
            {
                HitStopImpactSystem.Instance?.HeavyHit();
                CombatVFXController.Instance?.PlayHitEffect(position);
                CombatVFXController.Instance?.Shake(0.12f);
            }
            else if (result == CounterResult.Block)
            {
                HitStopImpactSystem.Instance?.LightHit();
                CombatVFXController.Instance?.PlayHitEffect(position);
            }
        }
    }
}
