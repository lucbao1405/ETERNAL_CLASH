using UnityEngine;
using System.Collections;
using EternalClash.Character;
using EternalClash.Animation;

namespace EternalClash.Combat
{
    /// <summary>
    /// Don danh chinh cua Player, theo co che Postknight:
    /// - Tu dong danh khi co dich trong tam (autoAttack, giu co che cu cua game).
    /// - Nguoi choi bam/tap cung danh duoc; bam trong luc dang danh thi "buffer"
    ///   chuoi ke tiep. Bam nhanh giup chuoi lien tuc thay vi le thoi.
    /// - Player dung yen tai lane; moi don trung day quai lui (push-pull).
    /// </summary>
    public class BasicAttackSystem : MonoBehaviour
    {
        [SerializeField] private int damage = 5;
        [Tooltip("Toc do danh toi thieu (giay giua 2 don) khi khong biet do dai clip")]
        [SerializeField] private float attackCooldown = 0.75f;
        [SerializeField] private float knockbackForce = 5f;
        [SerializeField] private float attackRange = 0.85f;
        [SerializeField, Range(0.1f, 0.9f)] private float hitMoment = 0.5f;

        [Header("Postknight feel")]
        [Tooltip("ON: tu dong danh khi co dich trong tam (co che cu cua game). Tap van hoat dong nhu thuong.")]
        [SerializeField] private bool autoAttack = true;
        [Tooltip("Phat nhanh clip attack (1.6 = danh thanh tho hon clip goc 1.6 lan)")]
        [SerializeField, Range(1f, 2.5f)] private float attackSpeedScale = 1.6f;
        [Tooltip("Nghi giua 2 don trong mot combo")]
        [SerializeField] private float attackRecovery = 0.08f;
        [Tooltip("Input bam luc dang danh duoc giu toi da nhan vay giay")]
        [SerializeField] private float inputBufferTime = 0.3f;
        [Tooltip("Quai bi day lui bao xa moi don (nho de giu nhap nhay combo)")]
        [SerializeField] private float hitPushback = 0.35f;
        [Tooltip("Noi them bao nhieu khi kiem tra tam luc dam CHAM vao quai. Dat xap xi " +
                 "hitPushback de quai vua bi day lui van an don tiep, nhung quai dung " +
                 "han ngoai tam (Goblin Mage dang di vao) thi khong dinh dam.")]
        [SerializeField] private float hitRangeTolerance = 0.35f;

        private float cooldownTimer;
        private GameObject target;
        private CharacterStateMachine stateMachine;
        private bool attacking;
        private float bufferTimeLeft;

        // Quet scene ton CPU tren dien thoai: cache ket qua toi da 0.15s moi quet lai.
        private GameObject cachedScanResult;
        private float rescanTimer;

        private void Awake() => stateMachine = GetComponent<CharacterStateMachine>();

        private void Update()
        {
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
            if (bufferTimeLeft > 0f) bufferTimeLeft -= Time.deltaTime;
            if (target == null || !IsValidTarget(target) || !IsTargetInRange(target)) target = FindNearestEnemy();

            // Tu dong danh nhu ban cu: co dich trong tam + duoc hiep gio thi danh.
            if (autoAttack && !attacking && cooldownTimer <= 0f && target != null)
                TryAttack();
        }

        /// <summary>Goi tu input/UI. Dang danh thi vao buffer, het don danh tiep.</summary>
        public void TryAttack()
        {
            if (attacking)
            {
                bufferTimeLeft = inputBufferTime;
                return;
            }
            if (cooldownTimer > 0f) return;
            if (!CanAct()) return;

            StartCoroutine(AttackSequence());
        }

        public void TryAttack(GameObject enemy) { SetTarget(enemy); TryAttack(); }

        /// <summary>Danh ngay neu du dieu kien (giu de tuong thich caller cu).</summary>
        public void StartAttack()
        {
            if (attacking || cooldownTimer > 0f || !CanAct()) return;
            StartCoroutine(AttackSequence());
        }

        private bool CanAct()
        {
            if (target == null || !IsValidTarget(target) || !IsTargetInRange(target))
                target = FindNearestEnemy();
            return target != null;
        }

        private IEnumerator AttackSequence()
        {
            attacking = true;

            IPlayerAnimationFeedback feedback = GetComponent<IPlayerAnimationFeedback>();
            float clipDuration = feedback != null ? feedback.GetAttackDuration() : 0f;
            if (clipDuration <= 0f) clipDuration = 0.6f;
            float scaledDuration = clipDuration / attackSpeedScale;
            float hitDelay = AttackTiming.HitDelay(scaledDuration, hitMoment);

            // Tran cadence: khong cho danh nhanh hon tong thoi luong don hien tai.
            cooldownTimer = Mathf.Max(attackCooldown, hitDelay + attackRecovery);

            if (stateMachine != null) stateMachine.ChangeState(CharacterState.Attack);
            feedback?.NotifyAttack(attackSpeedScale);
            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.PlayerAttack);

            yield return new WaitForSeconds(hitDelay);

            // Bi gian doan giua chu danh (knockback set state Hit/Run) thi khong gay damage.
            if (enabled && (stateMachine == null || stateMachine.CurrentState == CharacterState.Attack))
                DealDamage();

            yield return new WaitForSeconds(attackRecovery);

            if (stateMachine != null && stateMachine.CurrentState == CharacterState.Attack)
                stateMachine.ChangeState(CharacterState.Idle);

            attacking = false;

            // Mash: bam don ke tiep trong luc danh thi danh ngay khi het don.
            if (bufferTimeLeft > 0f)
            {
                bufferTimeLeft = 0f;
                TryAttack();
            }
        }

        public void SetTarget(GameObject enemy) { GameObject root = ResolveEnemyRoot(enemy); if (root != null) target = root; }
        public void ClearTarget() => target = null;
        public void AnimationDealDamage() => DealDamage();

        private void DealDamage()
        {
            // Chup vao bien cuc bo: DealDamage() co the khien enemy chet va
            // AttackTrigger.OnTriggerExit2D goi ClearTarget() (dat field target=null)
            // ngay trong luc dang chay ham nay.
            GameObject currentTarget = target;
            if (currentTarget == null || !IsValidTarget(currentTarget)) return;

            // Dam den CHAM ~0.3s sau khi bat dau vung (khop animation). Trong khoang
            // do muc tieu co the da di ra xa (bi day lui, hoac quai xa nhu Goblin Mage
            // van dang di vao). Khong kiem tra lai thi no an dam tu ngoai tam danh.
            if (!IsTargetInHitRange(currentTarget)) return;
            if (CombatDamageResolver.Instance == null) return;
            int finalDamage = Village.PlayerStatSystem.Instance != null ? Village.PlayerStatSystem.Instance.BasicAttackDamage : damage;
            CombatDamageResolver.Instance.DealDamage(currentTarget, finalDamage, DamageSource.BasicAttack);
            if (currentTarget == null) return;

            // Day lui nhe: quai lui ra mot buoc roi tu di lai (push-pull cua Postknight).
            KnockbackReceiver knockback = currentTarget.GetComponent<KnockbackReceiver>();
            if (knockback != null)
            {
                Vector2 direction = (currentTarget.transform.position - transform.position).normalized;
                knockback.ApplyKnockback(direction, knockbackForce, hitPushback);
            }

            ComboCounter.RegisterHit(currentTarget.transform.position);
        }

        private GameObject FindNearestEnemy()
        {
            // Dang trong thoi gian tri hoan quet: dung ket qua cu con song.
            if (rescanTimer > 0f)
            {
                rescanTimer -= Time.deltaTime;
                return cachedScanResult != null && IsValidTarget(cachedScanResult) ? cachedScanResult : null;
            }
            rescanTimer = 0.15f;

            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            GameObject nearest = null;
            float nearestDistance = attackRange;
            foreach (GameObject enemy in enemies)
            {
                if (!IsValidTarget(enemy)) continue;
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance <= nearestDistance) { nearestDistance = distance; nearest = enemy; }
            }
            cachedScanResult = nearest;
            return nearest;
        }

        private bool IsTargetInRange(GameObject enemy) => enemy != null && Vector2.Distance(transform.position, enemy.transform.position) <= attackRange;

        /// <summary>
        /// Tam tinh luc dam CHAM vao quai. Nới hon tam chon muc tieu mot chut
        /// (<see cref="hitRangeTolerance"/>): quai bi day lui ngay sau don truoc van
        /// an don nay, nhung quai dung han ngoai tam thi khong.
        /// </summary>
        private bool IsTargetInHitRange(GameObject enemy)
        {
            return enemy != null &&
                Vector2.Distance(transform.position, enemy.transform.position) <= attackRange + hitRangeTolerance;
        }

        private static bool IsValidTarget(GameObject enemy) => enemy != null && enemy.CompareTag("Enemy") && enemy.activeInHierarchy && !IsDead(enemy);

        // Xac dang chay anim chet van activeInHierarchy: phai loai truoc khi no
        // chiem cho muc tieu gan nhat va chan player danh ke dung sau no.
        private static bool IsDead(GameObject enemy) => enemy.TryGetComponent<EternalClash.Enemy.EnemyHealthSystem>(out var health) && health.IsDead;

        private static GameObject ResolveEnemyRoot(GameObject obj)
        {
            if (obj == null) return null;
            if (obj.CompareTag("Enemy")) return obj;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                if (parent.CompareTag("Enemy")) return parent.gameObject;
                parent = parent.parent;
            }
            return null;
        }
    }
}
