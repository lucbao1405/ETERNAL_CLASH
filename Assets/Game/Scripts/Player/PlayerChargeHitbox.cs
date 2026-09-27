using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EternalClash.Skill;
using EternalClash.Combat;
using EternalClash.Enemy;
using EternalClash.Village;

namespace EternalClash.Player
{
    public class PlayerChargeHitbox : MonoBehaviour
    {
        private PlayerChargeController chargeController;
        private ChargeSkill chargeSkill;
        private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();

        [Header("Charge Sweep")]
        [Tooltip("World-space reach in front of the player that the charge hits. " +
                 "Compensates for the scaled-down charge hitbox collider.")]
        [SerializeField] private float chargeReach = 1.1f;
        [Tooltip("Max vertical offset an enemy may have from the player and still be hit.")]
        [SerializeField] private float verticalTolerance = 1.0f;
        private const float FrontMinOffsetX = -0.05f;

        private void Awake()
        {
            chargeController = GetComponentInParent<PlayerChargeController>();
            var root = transform.root;
            chargeSkill = root.GetComponent<ChargeSkill>()
                          ?? root.GetComponentInChildren<ChargeSkill>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null)
                return;

            GameObject enemyObject = ResolveEnemyRoot(other.gameObject);
            if (enemyObject == null)
                return;

            if (!IsCharging())
                return;

            // Phai kiem tra khoang cach that y HET nhu SweepFrontEnemies: quai co
            // collider phu rat rong (vung AttackRange cua Goblin Mage ban kinh 4.55
            // lech ve phia player) cham vao hitbox luot tu ~8 don vi, trong khi than
            // quai van dung rat xa. Khong chan thi no dinh dam tu ngoai man hinh.
            if (!IsWithinChargeReach(enemyObject))
                return;

            HandleChargeHit(enemyObject);
        }

        private void Update()
        {
            if (!IsCharging())
                return;

            SweepFrontEnemies();
        }

        private bool IsCharging()
        {
            if (chargeController != null && chargeController.IsCharging)
                return true;
            if (chargeSkill != null && chargeSkill.IsCharging)
                return true;
            return false;
        }

        private void SweepFrontEnemies()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            for (int i = 0; i < enemies.Length; i++)
            {
                GameObject enemy = enemies[i];
                if (enemy == null || !enemy.activeInHierarchy)
                    continue;

                GameObject enemyObject = ResolveEnemyRoot(enemy);
                if (enemyObject == null || hitEnemies.Contains(enemyObject))
                    continue;

                if (enemyObject.TryGetComponent<EnemyHealthSystem>(out var health) && health.IsDead)
                    continue;

                if (!IsWithinChargeReach(enemyObject))
                    continue;

                HandleChargeHit(enemyObject);
            }
        }

        /// <summary>
        /// Quai co nam trong tam quet cua don luot khong: tinh theo VI TRI THAT cua
        /// quai, khong tin vao va cham collider (quai co the mang collider phu rong
        /// hon than minh rat nhieu).
        /// </summary>
        private bool IsWithinChargeReach(GameObject enemyObject)
        {
            if (enemyObject == null)
                return false;

            Vector3 toEnemy = enemyObject.transform.position - transform.root.position;
            if (toEnemy.x < FrontMinOffsetX || toEnemy.x > chargeReach)
                return false;

            return Mathf.Abs(toEnemy.y) <= verticalTolerance;
        }

        private void HandleChargeHit(GameObject enemyObject)
        {
            if (enemyObject == null || hitEnemies.Contains(enemyObject))
                return;

            if (enemyObject.TryGetComponent<EnemyHealthSystem>(out var health) && health.IsDead)
                return;

            hitEnemies.Add(enemyObject);

            int finalDamage = PlayerStatSystem.Instance != null
                ? PlayerStatSystem.Instance.ChargeDamage
                : 20;

            SkillManager skillManager = transform.root.GetComponent<SkillManager>();
            if (skillManager != null)
                finalDamage += SkillLoadout.GetBonusDamage("charge");

            if (skillManager != null && SkillLoadout.GetEffectType("charge") ==
                (int)EternalClash.Data.SpecialEffectType.DamageBonus)
            {
                finalDamage = Mathf.RoundToInt(finalDamage *
                    (1f + SkillLoadout.GetEffectValue("charge") / 100f));
            }

            Debug.Log($"[CHARGE HIT] {enemyObject.name} finalDamage={finalDamage}");

            CombatDamageResolver.Instance?.DealDamage(
                enemyObject,
                finalDamage,
                DamageSource.Charge
            );

            // Boss (ranged, tu dieu khien vi tri) khong an knockback/pull nhu quai
            // thuong: bao no lui ra ria NGAY voi toc do day manh.
            enemyObject.GetComponent<BossController>()?.OnHitByCharge();

            // Push enemy toward the player: shove it just inside attack range
            // instead of leaving it parked where the charge caught it.
            StartCoroutine(PullIntoRange(enemyObject));
        }

        private static GameObject ResolveEnemyRoot(GameObject obj)
        {
            if (obj == null)
                return null;

            if (obj.CompareTag("Enemy"))
                return obj;

            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                if (parent.CompareTag("Enemy"))
                    return parent.gameObject;
                parent = parent.parent;
            }

            return null;
        }

        private IEnumerator PullIntoRange(GameObject enemy)
        {
            if (enemy == null) yield break;

            var enemyStatus = enemy.GetComponent<EnemyStatusController>();
            var enemyMover = enemy.GetComponent<EnemyMover>();
            enemyStatus?.ApplyStun(1.5f);
            if (enemyMover != null)
                enemyMover.PauseMovement(1.5f);

            // Quai danh xa (Goblin Mage, Boss...) chi an dam + choang, KHONG bi keo
            // vao sat nguoi. Keo vao trong luc Charge nhin nhu no "tu bay" vao
            // player, va cung pha luon vai tro dung xa ban cua no.
            // Nhan dien ranged qua attack that (co projectilePrefab), khong phai co
            // isArcher cua mover: Boss bang dan nhung isArcher = 0.
            if ((enemyMover != null && enemyMover.IsArcher) ||
                (enemy.TryGetComponent<EnemyAttack>(out var enemyAttack) && enemyAttack.IsRanged))
                yield break;

            Vector3 playerPos = transform.root.position;
            Vector3 enemyStart = enemy.transform.position;

            // Target: snap enemy to the hitbox edge nearest its current position.
            // Use a small radius (0.6f) so the enemy stops just inside the attack range,
            // not overlapping the player.
            const float combatRadius = 0.6f;
            Vector3 toEnemy = enemyStart - playerPos;
            Vector3 targetPos = enemyStart;
            if (toEnemy.sqrMagnitude > 0.0001f)
                targetPos = playerPos + toEnemy.normalized * combatRadius;

            float duration = 0.18f;
            float t = 0f;
            while (t < duration && enemy != null)
            {
                t += Time.deltaTime;
                enemy.transform.position = Vector3.Lerp(enemyStart, targetPos, t / duration);
                yield return null;
            }
            if (enemy != null)
                enemy.transform.position = targetPos;
        }

        public void ClearHitEnemies()
        {
            hitEnemies.Clear();
        }
    }
}
