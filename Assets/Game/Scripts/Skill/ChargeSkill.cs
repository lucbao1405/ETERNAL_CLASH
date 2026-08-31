using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EternalClash.World;
using EternalClash.Combat;
using EternalClash.Enemy;
using EternalClash.Village;

namespace EternalClash.Skill
{
    public class ChargeSkill : SkillBase
    {
        public float chargeMultiplier = 3.0f;
        public float chargeDuration = 0.67f;

        public int baseDamage = 20;
        public float knockbackForce = 3.0f;
        public float stunDuration = 1.0f;

        private WorldScroller worldScroller;
        private bool charging;
        private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();

        // Forward charge hitbox collider (added on the Player prefab). Disabled
        // outside of a charge so it only detects enemies during the lunge.
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
            foreach (var c in GetComponentsInChildren<BoxCollider2D>())
            {
                if (c.isTrigger && c.size.x > 2f)
                {
                    chargeHitbox = c;
                    break;
                }
            }
            if (chargeHitbox != null)
                chargeHitbox.enabled = false;
        }

        protected override void Execute()
        {
            Debug.Log("[SKILL] Charge Execute START");

            if (!charging)
                StartCoroutine(ChargeRoutine());
        }

        private IEnumerator ChargeRoutine()
        {
            charging = true;
            hitEnemies.Clear();

            if (chargeHitbox != null)
                chargeHitbox.enabled = true;

            if (worldScroller == null)
                worldScroller = FindObjectOfType<WorldScroller>();

            if (worldScroller != null)
                worldScroller.SetSpeedMultiplier(chargeMultiplier);

            yield return new WaitForSeconds(chargeDuration);

            if (worldScroller != null)
                worldScroller.ResetSpeed();

            if (chargeHitbox != null)
                chargeHitbox.enabled = false;

            charging = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!charging || !other.CompareTag("Enemy"))
                return;

            if (hitEnemies.Contains(other.gameObject))
                return;

            hitEnemies.Add(other.gameObject);

            int finalDamage = PlayerStatSystem.Instance != null 
                ? PlayerStatSystem.Instance.ChargeDamage 
                : baseDamage;

            CombatDamageResolver.Instance?.DealDamage(
                other.gameObject,
                finalDamage,
                DamageSource.Charge
            );

            other.GetComponent<KnockbackReceiver>()?.ApplyKnockback(Vector2.left, knockbackForce);
            other.GetComponent<EnemyStatusController>()?.ApplyStun(stunDuration);
        }
    }
}
