using UnityEngine;
using EternalClash.Combat;
using EternalClash.Skill;

namespace EternalClash.Enemy
{
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 5.0f;
        [SerializeField] private int damage = 12;
        [SerializeField] private float lifeTime = 5.0f;

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
                int finalDamage = damage;

                // GDD 3.3: Shield (Giương Khiên) chặn hỏa lực tầm xa.
                var shield = other.GetComponent<ShieldSkill>();
                if (shield != null && shield.IsActive())
                    finalDamage = shield.BlockDamage(finalDamage, gameObject);

                CombatDamageResolver.Instance?.DealDamage(
                    other.gameObject,
                    finalDamage,
                    DamageSource.EnemyAttack
                );

                Destroy(gameObject);
            }
        }
    }
}
