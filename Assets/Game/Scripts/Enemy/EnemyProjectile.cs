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
                // Khong tu goi ShieldSkill.BlockDamage() o day nua: DamageReceiver
                // ben trong DealDamage() da lam viec do. Goi ca hai noi khien mui ten
                // bi giam 80% HAI LAN (12 -> 2 -> 1) va phan don hai lan.
                // Chi can truyen gameObject lam attacker de Khien phan lai 5 DMG.
                CombatDamageResolver.Instance?.DealDamage(
                    other.gameObject,
                    damage,
                    DamageSource.EnemyAttack,
                    gameObject
                );

                Destroy(gameObject);
            }
        }
    }
}
