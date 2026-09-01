using UnityEngine;
using EternalClash.Combat;

public class ChargeCounterResolver : MonoBehaviour
{
        [SerializeField] private float interruptWindow = 0.6f;
        [SerializeField] private int damage = 20;
        [SerializeField] private float stunTime = 1.2f;
        [SerializeField] private AudioClip perfectChargeSfx;

    public bool TryCounterEnemyAttack(EnemyAttackTimingController enemy)
    {
        if (enemy == null) return false;

            if (enemy.IsPreparingAttack(interruptWindow))
            {
                enemy.CancelAttack();
                enemy.ApplyStun(stunTime);

                if (CombatDamageResolver.Instance != null)
                {
                    CombatDamageResolver.Instance.DealDamage(
                        enemy.gameObject,
                        damage,
                        DamageSource.Charge
                    );
                }

                HitStopImpactSystem.Instance?.HeavyHit();
                CombatVFXController.Instance?.PlayHitEffect(enemy.transform.position);
                CombatVFXController.Instance?.Shake(0.12f);

                if (perfectChargeSfx != null)
                    AudioSource.PlayClipAtPoint(perfectChargeSfx, enemy.transform.position);

                return true;
            }

        return false;
    }
}
