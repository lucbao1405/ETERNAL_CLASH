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

        [Header("Telegraph (Postknight)")]
        [Tooltip("Thoi gian dung lai 'ngam' truoc khi ra don: nguoi choi doc y dinh, ne/phan don kip. Trung don trong luc nay thi don bi pha.")]
        [SerializeField, Min(0f)] private float windUpTime = 0.6f;
        [Tooltip("Khoang cach bat dau pha ngam: quai vua lai gan la ngam ngay trong luc van di toi, hit ha khi da den cho. Phai lon hon playerStopDistance cua EnemyMover.")]
        [SerializeField, Min(0f)] private float windUpStartRange = 2.4f;

        [Header("Ranged (GDD 3.4 Goblin Cung)")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 5f;
        [SerializeField] private Vector2 projectileSpawnOffset = new Vector2(0f, 0.8f);

        private float timer;
        private Transform player;
        private Camera mainCamera;
        private Coroutine meleeCoroutine;
        private Coroutine attackSequence;
        private EnemyStatusController status;
        private EnemyAttackTimingController attackTiming;

        /// <summary>Dang trong pha ngam (telegraph) - don chua bay, co the bi pha.</summary>
        public bool IsWindingUp { get; private set; }

        private void Awake()
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null)
                player = obj.transform;

            status = GetComponent<EnemyStatusController>();
            attackTiming = GetComponent<EnemyAttackTimingController>();
            // Prefab khong gan: tu them de cua so phan don (Warning) ton tai tren
            // moi quai - giong cach EnemyMover tu them CombatLaneAligner.
            if (attackTiming == null)
                attackTiming = gameObject.AddComponent<EnemyAttackTimingController>();

            // Grace period ngan cho quai can chien: khong ngam ngay luc vua spawn
            // (kip cau hinh, cho nguoi choi doc tu the - giong Postknight deu co
            // luong bu dau tran). Ranged (Goblin Mage) vao man hinh la ban nen
            // khong an grace. Telegraph bat dau tu xa (windUpStartRange) nen phan
            // doc y dinh da nam trong chinh pha ngam.
            timer = projectilePrefab != null ? 0f : attackInterval * 0.25f;
        }

        private void Update()
        {
            if (player == null)
                return;

            // Dang trong chu ky ngam -> danh, cooldown khong giam trong luc nay.
            if (attackSequence != null)
                return;

            // Bi choang: huy don dang ngam va khong mo chu ky moi.
            if (status != null && status.IsStunned())
            {
                InterruptAttack();
                return;
            }

            timer -= Time.deltaTime;

            // Ranged (Goblin Mage): vua lo mat vao man hinh la mo chu ky ban,
            // khong cho di dung tam attackRange. Melee: bat dau ngam tu xa
            // (windUpStartRange) trong luc van dang lai gan player.
            bool inTrigger = projectilePrefab != null
                ? IsOnScreen()
                : Mathf.Abs(transform.position.x - player.position.x) <= windUpStartRange;
            if (inTrigger && timer <= 0)
            {
                timer = attackInterval;
                attackSequence = StartCoroutine(AttackSequence());
            }
        }

        private bool IsOnScreen()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
            if (mainCamera == null)
                return Mathf.Abs(transform.position.x - player.position.x) <= attackRange;

            Vector3 viewport = mainCamera.WorldToViewportPoint(transform.position);
            return viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
        }

        private IEnumerator AttackSequence()
        {
            // Telegraph kieu Postknight: dung lai ro rang truoc khi ra don.
            IsWindingUp = true;
            // Dong bo cua so phan don (Prepare/Warning) chay song song voi pha ngam.
            attackTiming?.StartAttack();
            yield return new WaitForSeconds(windUpTime);
            IsWindingUp = false;

            if (!enabled)
                yield break;

            // Player phan don/choang dung luc ngam -> don that bai.
            if (attackTiming != null &&
                (attackTiming.Interrupted || attackTiming.State == EnemyAttackTimingController.AttackState.Stunned))
            {
                timer = attackInterval * 0.5f;
                attackSequence = null;
                yield break;
            }

            AttackPlayer();
            attackSequence = null;
        }

        /// <summary>
        /// Pha don don bi pha khi quai trung don trong luc ngam (stagger kieu Postknight).
        /// Don da ra (clip attack da chay qua pha ngam) thi khong bi huy.
        /// </summary>
        public void InterruptAttack()
        {
            if (!IsWindingUp)
                return;

            if (attackSequence != null)
            {
                StopCoroutine(attackSequence);
                attackSequence = null;
            }
            IsWindingUp = false;
            timer = attackInterval * 0.5f; // nghi ngan truoc khi ngam lai
        }

        /// <summary>Nhan sat thuong theo do kho man (goi ngay sau khi spawn).</summary>
        public void ScaleDamage(float multiplier)
        {
            if (multiplier <= 1f)
                return;

            damage = Mathf.Max(1, Mathf.RoundToInt(damage * multiplier));
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
                Vector3 spawnPos = transform.position + (Vector3)projectileSpawnOffset;
                Vector3 targetPos = GetPlayerColliderCenter();
                
                var proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
                var ep = proj.GetComponent<EnemyProjectile>();
                if (ep != null)
                    ep.Initialize(damage, projectileSpeed, (targetPos - spawnPos).normalized);
                return;
            }

            // Melee: dmg den o giua state Attack (theo do dai clip Spine)
            // thay vi ngay khi bat dau state.
            meleeCoroutine = StartCoroutine(MeleeHitRoutine());
        }

        public void CancelPendingAttack()
        {
            if (meleeCoroutine != null)
            {
                StopCoroutine(meleeCoroutine);
                meleeCoroutine = null;
            }
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

            // Melee phai thuc su dung can player (trong attackRange) moi gay dmg:
            // don mo ra khi con dang chay toi / player da chay khoi thi vo (miss).
            if (Mathf.Abs(transform.position.x - player.position.x) > attackRange)
                return;

            // Truyen gameObject lam attacker de Khien phan lai 5 DMG ve chinh con quai
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

        private Vector3 GetPlayerColliderCenter()
        {
            if (player == null) return Vector3.zero;
            
            var collider = player.GetComponent<Collider2D>();
            if (collider != null)
                return collider.bounds.center;
            
            return player.position;
        }
    }
}
