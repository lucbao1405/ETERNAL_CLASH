using UnityEngine;
using EternalClash.Combat;
using EternalClash.Skill;
using EternalClash.World;

namespace EternalClash.Enemy
{
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 5.0f;
        [SerializeField] private int damage = 12;
        [SerializeField] private float lifeTime = 5.0f;
        [SerializeField] private float knockbackForce = 0.5f;

        private Vector2 direction = Vector2.left;

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        public void Initialize(int dmg, float spd, Vector2 dir = default)
        {
            damage = dmg;
            speed = spd;
            if (dir != Vector2.zero) direction = dir.normalized;
        }

        private void Update()
        {
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                CombatDamageResolver.Instance?.DealDamage(
                    other.gameObject,
                    damage,
                    DamageSource.EnemyAttack,
                    gameObject
                );

                ShieldSkill shield = other.GetComponent<ShieldSkill>();
                if (shield == null || !shield.IsActive())
                {
                    KnockbackReceiver knockback = other.GetComponent<KnockbackReceiver>();
                    if (knockback != null)
                    {
                        Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
                        knockback.ApplyKnockback(knockbackDir, knockbackForce);
                    }
                }

                Destroy(gameObject);
            }
        }
    }
}
