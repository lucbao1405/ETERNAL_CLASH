using UnityEngine;
using EternalClash.Enemy;
using EternalClash.Character;

namespace EternalClash.Combat
{
    public class BasicAttack : MonoBehaviour
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

            if (target != null && cooldownTimer <= 0)
            {
                StartAttack();

                // Tam thoi gay damage truc tiep khi chua co Animation Event
                // Sau nay se chuyen lai cho AnimationDealDamage()
                DealDamage();

                cooldownTimer = attackCooldown;
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
        }

        public void AnimationDealDamage()
        {
            DealDamage();
        }

        public void DealDamage()
        {
            if (target == null)
                return;

            Debug.Log("Player attack: " + target.name);

            EnemyBase enemy = target.GetComponent<EnemyBase>();

            if (enemy != null)
            {
                enemy.ReceiveDamage(damage);

                KnockbackReceiver knockback = target.GetComponent<KnockbackReceiver>();
                if (knockback != null)
                {
                    Vector2 direction = (target.transform.position - transform.position).normalized;
                    knockback.ApplyKnockback(direction, knockbackForce);
                }
            }
        }
    }
}
