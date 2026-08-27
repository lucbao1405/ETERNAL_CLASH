using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Combat
{
    public class EnemyAttack : MonoBehaviour
    {
        public int damage = 5;
        public float attackInterval = 2f;
        public float attackRange = 1.2f;
        public float knockbackForce = 5f;

        private float timer;
        private Transform player;

        private void Awake()
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null)
                player = obj.transform;
        }

        private void Update()
        {
            if (player == null)
                return;

            timer -= Time.deltaTime;

            float distance = Vector2.Distance(transform.position, player.position);

            if (distance <= attackRange && timer <= 0)
            {
                AttackPlayer();
                timer = attackInterval;
            }
        }

        private void AttackPlayer()
        {
            Debug.Log("Enemy attack player");

            HealthSystem health = player.GetComponent<HealthSystem>();

            if (health != null)
            {
                health.TakeDamage(damage);
            }

            KnockbackReceiver knockback = player.GetComponent<KnockbackReceiver>();
            if (knockback != null)
            {
                Vector2 direction = (player.position - transform.position).normalized;
                knockback.ApplyKnockback(direction, knockbackForce);
            }
        }
    }
}
