using UnityEngine;
using EternalClash.Skill;
using EternalClash.Enemy;

namespace EternalClash.Combat
{
    public class EnemyAttack : MonoBehaviour
    {
        public int damage = 5;
        public float attackInterval = 2f;
        public float attackRange = 1.2f;
        public float knockbackForce = 1f;

        [Header("Ranged (GDD 3.4 Goblin Cung)")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 5f;

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

            // Ranged enemy (Goblin Cung): spawn a projectile toward the player
            // instead of melee damage + knockback.
            if (projectilePrefab != null)
            {
                var proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
                var ep = proj.GetComponent<EnemyProjectile>();
                if (ep != null)
                    ep.Initialize(damage, projectileSpeed, (player.position - transform.position).normalized);
                return;
            }

            CombatDamageResolver.Instance?.DealDamage(
                player.gameObject,
                damage,
                DamageSource.EnemyAttack
            );

            ShieldSkill shield = player.GetComponent<ShieldSkill>();

            // Shield dang bat: chan day lui
            if (shield != null && shield.IsActive())
            {
                Debug.Log("Shield active - Ignore knockback");
                return;
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
