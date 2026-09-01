using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EternalClash.World;
using EternalClash.Combat;
using EternalClash.Enemy;
using EternalClash.Village;
using EternalClash.Player;

namespace EternalClash.Skill
{
    public class ChargeSkill : SkillBase
    {
        public float chargeMultiplier = 4.0f;
        [Header("Charge Timing")]
        [SerializeField] private float chargeDuration = 0.5f;

        public int baseDamage = 20;
        public float knockbackForce = 3.0f;
        public float stunDuration = 1.0f;

        private WorldScroller worldScroller;
        private bool charging;
        private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();

        private DamageReceiver playerDamageReceiver;
        private KnockbackReceiver playerKnockbackReceiver;
        private PlayerChargeController playerChargeController;

        private Collider2D chargeHitbox;

        public bool IsCharging => charging;

        protected override void Awake()
        {
            skillName = "Charge";
            cooldown = 3.0f;
            base.Awake();
            worldScroller = FindObjectOfType<WorldScroller>();

            if (worldScroller == null)
                Debug.LogWarning("[ChargeSkill] WorldScroller not found yet, will search dynamically");

            // Locate the dedicated forward charge hitbox (a trigger BoxCollider2D
            // with a large X size, added via the prefab). Keep it off by default.
            // Search from root transform to find ChargeHitbox sibling
            var root = transform.root;
            foreach (var c in root.GetComponentsInChildren<BoxCollider2D>())
            {
                if (c.isTrigger && c.size.x > 2f)
                {
                    chargeHitbox = c;
                    break;
                }
            }
            if (chargeHitbox != null)
            {
                chargeHitbox.enabled = false;
                Debug.Log("[CHARGE] Found chargeHitbox: " + chargeHitbox.name);
            }
            else
            {
                Debug.LogWarning("[CHARGE] No chargeHitbox found! Need BoxCollider2D with isTrigger=true and size.x>2f");
            }

            playerDamageReceiver = GetComponent<DamageReceiver>();
            playerKnockbackReceiver = GetComponent<KnockbackReceiver>();
            playerChargeController = GetComponentInParent<PlayerChargeController>();
        }

        protected override void Execute()
        {
            Debug.Log("[CHARGE START] ChargeSkill.Execute START");

            if (!charging)
                StartCoroutine(ChargeRoutine());
        }

        private IEnumerator ChargeRoutine()
        {
            charging = true;
            hitEnemies.Clear();

            playerChargeController?.StartCharge();

            if (chargeHitbox != null)
            {
                chargeHitbox.enabled = true;
                Debug.Log("[CHARGE] ChargeHitbox enabled: " + chargeHitbox.name);
            }
            else
            {
                Debug.LogWarning("[CHARGE] chargeHitbox is NULL - no collider found!");
            }

            var chargeHitboxObj = transform.root.GetComponentInChildren<PlayerChargeHitbox>();
            chargeHitboxObj?.ClearHitEnemies();

            if (playerDamageReceiver != null)
                playerDamageReceiver.SetDamageMultiplier(0f);
            if (playerKnockbackReceiver != null)
                playerKnockbackReceiver.enabled = false;

            if (worldScroller == null)
                worldScroller = FindObjectOfType<WorldScroller>();

            if (worldScroller != null)
            {
                worldScroller.SetSpeedMultiplier(chargeMultiplier);
                Debug.Log("[CHARGE] WorldScroller speed multiplied x" + chargeMultiplier);
            }

            ApplyChargeWorldEffect();
            FreezeEnemiesInPlayerRange();

            yield return new WaitForSeconds(GetChargeDuration());

            if (worldScroller != null)
                worldScroller.ResetSpeed();

            foreach (var enemyMover in FindObjectsOfType<EnemyMover>())
            {
                enemyMover.DisableChargePull();
            }

            if (chargeHitbox != null)
            {
                chargeHitbox.enabled = false;
                Debug.Log("[CHARGE] ChargeHitbox disabled");
            }

            if (playerDamageReceiver != null)
                playerDamageReceiver.ResetDamageMultiplier();
            if (playerKnockbackReceiver != null)
                playerKnockbackReceiver.enabled = true;

            playerChargeController?.EndCharge();

            charging = false;
            Debug.Log("[CHARGE END] Charge routine finished");
        }

        public float GetChargeDuration()
        {
            if (playerChargeController != null)
                return playerChargeController.ChargeDuration;

            return chargeDuration;
        }

        private void ApplyChargeWorldEffect()
        {
            // Charge no longer moves the player. The player stays fixed while
            // the world/enemy flow speed increases.
            if (worldScroller != null)
            {
                worldScroller.SetSpeedMultiplier(chargeMultiplier);
            }

            foreach (var enemyMover in FindObjectsOfType<EnemyMover>())
            {
                if (enemyMover == null)
                    continue;

                float chargeSpeed = Mathf.Max(chargeMultiplier * 2f, 4f);
                enemyMover.EnableChargePull(chargeSpeed);
            }
        }

        private void FreezeEnemiesInPlayerRange()
        {
            var playerPosition = transform.root.position;
            foreach (var enemy in FindObjectsOfType<EnemyStatusController>())
            {
                if (enemy == null)
                    continue;

                var enemyMover = enemy.GetComponent<EnemyMover>();
                if (enemyMover == null)
                    continue;

                float attackRange = enemyMover.IsArcher ? enemyMover.ArcherStopDistance : 1.2f;
                if (Vector2.Distance(enemy.transform.position, playerPosition) <= attackRange)
                {
                    enemy.ApplyStun(1.0f);
                    enemyMover.PauseMovement(1.0f);
                }
            }
        }
    }
}
