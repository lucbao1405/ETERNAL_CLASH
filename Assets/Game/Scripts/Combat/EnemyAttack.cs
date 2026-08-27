using UnityEngine;

namespace EternalClash.Combat
{
    public class EnemyAttack : MonoBehaviour
    {
        public int damage = 5;
        public float attackInterval = 2f;
        public float attackRange = 1.2f;
        public float knockbackForce = 1f;

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

            if (Vector2.Distance(transform.position, player.position) <= attackRange && timer <= 0)
            {
                AttackPlayer();
                timer = attackInterval;
            }
        }

        public void AttackPlayer()
        {
            Debug.Log("Enemy attack player");

            DamageReceiver receiver = player.GetComponent<DamageReceiver>();
            if (receiver != null)
                receiver.TakeDamage(damage);

            KnockbackReceiver knockback = player.GetComponent<KnockbackReceiver>();
            if (knockback != null)
            {
                Vector2 direction = (player.position - transform.position).normalized;
                knockback.ApplyKnockback(direction, knockbackForce);
            }
        }
    }
}
