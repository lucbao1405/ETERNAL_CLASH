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

        private void Awake()
        {
            chargeController = GetComponentInParent<PlayerChargeController>();
            var root = transform.root;
            chargeSkill = root.GetComponentInChildren<ChargeSkill>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null)
                return;

            GameObject enemyObject = ResolveEnemyRoot(other.gameObject);
            if (enemyObject == null)
                return;

            bool isCharging = false;
            if (chargeController != null && chargeController.IsCharging)
                isCharging = true;
            if (chargeSkill != null && chargeSkill.IsCharging)
                isCharging = true;

            if (!isCharging)
                return;

            if (hitEnemies.Contains(enemyObject))
                return;

            hitEnemies.Add(enemyObject);

            int finalDamage = PlayerStatSystem.Instance != null
                ? PlayerStatSystem.Instance.ChargeDamage
                : 20;

            CombatDamageResolver.Instance?.DealDamage(
                enemyObject,
                finalDamage,
                DamageSource.Charge
            );

            // Pull enemy just inside the hitbox edge so it stands within attack range
            // of the player rather than on top of them.
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
