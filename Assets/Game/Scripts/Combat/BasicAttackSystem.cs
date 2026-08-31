using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Combat
{
    public class BasicAttackSystem : MonoBehaviour
    {
        public int damage = 5;
        public float attackCooldown = 1f;
        public float knockbackForce = 5f;

        private float cooldownTimer;
        private GameObject target;
        private CharacterStateMachine stateMachine;

        private void Awake()
        {
            stateMachine = GetComponent<CharacterStateMachine>();
        }

        private void Update()
        {
            if (cooldownTimer > 0)
                cooldownTimer -= Time.deltaTime;

            // Giữ flow auto attack cũ của BasicAttack
            if (target != null && cooldownTimer <= 0)
            {
                StartAttack();
            }
        }

        public void SetTarget(GameObject enemy)
        {
            target = enemy;
        }

        public void ClearTarget()
        {
            target = null;
        }

        public void StartAttack()
        {
            if (stateMachine != null)
                stateMachine.ChangeState(CharacterState.Attack);
            else
                DealDamage();
        }

        // Gọi từ Animation Event tại hit frame
        public void AnimationDealDamage()
        {
            DealDamage();
        }

        public void TryAttack(GameObject enemy)
        {
            if (enemy == null)
                return;

            SetTarget(enemy);
            StartAttack();
        }

        private void DealDamage()
        {
            if (target == null || cooldownTimer > 0)
                return;

            cooldownTimer = attackCooldown;

            if (CombatDamageResolver.Instance == null)
                return;

            int finalDamage = Village.PlayerStatSystem.Instance != null 
                ? Village.PlayerStatSystem.Instance.BasicAttackDamage 
                : damage;

            CombatDamageResolver.Instance.DealDamage(
                target,
                finalDamage,
                DamageSource.BasicAttack
            );

            KnockbackReceiver knockback = target.GetComponent<KnockbackReceiver>();
            if (knockback != null)
            {
                Vector2 direction = (target.transform.position - transform.position).normalized;
                knockback.ApplyKnockback(direction, knockbackForce);
            }
        }
    }
}
