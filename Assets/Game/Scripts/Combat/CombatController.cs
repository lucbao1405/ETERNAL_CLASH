using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Combat
{
    public class CombatController : MonoBehaviour
    {
        public CharacterStats attacker;
        public CharacterStats defender;
        public GameObject target;

        public void Attack()
        {
            PerformAttackHit();
        }

        // Called by AnimationCombatEvent on the attack hit frame
        public void PerformAttackHit()
        {
            if (attacker == null || defender == null || target == null)
                return;

            float damage = DamageSystem.CalculateDamage(attacker, defender);

            if (CombatDamageResolver.Instance != null)
            {
                CombatDamageResolver.Instance.DealDamage(
                    target,
                    Mathf.RoundToInt(damage),
                    DamageSource.BasicAttack
                );
            }
        }
    }
}
