using System.Collections;
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
        [SerializeField, Range(0.1f, 0.9f)] private float hitMoment = 0.5f;

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

            if (Mathf.Abs(transform.position.x - player.position.x) <= attackRange && timer <= 0)
            {
                AttackPlayer();
                timer = attackInterval;
            }
        }

        public void AttackPlayer()
        {
            Debug.Log("Enemy attack player");

            // Animation-layer hook only (no gameplay impact).
            GetComponentInParent<EternalClash.Animation.IEnemyAnimationFeedback>()?.NotifyAttack();
            EternalClash.Audio.GameAudio.PlayEnemy(gameObject, EternalClash.Audio.EnemySound.Attack);

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

            // Melee: dmg den o giua state Attack (theo do dai clip Spine)
            // thay vi ngay khi bat dau state.
            StartCoroutine(MeleeHitRoutine());
        }

        private IEnumerator MeleeHitRoutine()
        {
            var feedback = GetComponentInParent<EternalClash.Animation.IEnemyAnimationFeedback>();
            float clipDuration = feedback != null ? feedback.GetAttackDuration() : 0f;
            yield return new WaitForSeconds(AttackTiming.HitDelay(clipDuration, hitMoment));
            if (!enabled) yield break; // enemy chet / player chet giua chu danh
            MeleeHit();
        }

        private void MeleeHit()
        {
            if (player == null)
                return;

            // Truyen gameObject lam attacker de Khien phan lai 5 DMG ve chinh con quai
            // vua danh (GDD 3.3). Thieu tham so nay thi phan don khong kich hoat.
            CombatDamageResolver.Instance?.DealDamage(
                player.gameObject,
                damage,
                DamageSource.EnemyAttack,
                gameObject
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
