using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EternalClash.World;
using EternalClash.Combat;
using EternalClash.Enemy;

namespace EternalClash.Skill
{
    public class ChargeSkill : SkillBase
    {
        public float chargeMultiplier = 2.5f;
        public float chargeDuration = 1.5f;

        public int damage = 30;
        public float knockbackForce = 2.5f;
        public float stunDuration = 1.5f;

        private WorldScroller worldScroller;
        private bool charging;
        private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();

        protected override void Awake()
        {
            base.Awake();
            worldScroller = FindObjectOfType<WorldScroller>();

            if (worldScroller == null)
                Debug.LogError("[ChargeSkill] Missing WorldScroller");
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

            if (worldScroller != null)
                worldScroller.SetSpeedMultiplier(chargeMultiplier);

            Debug.Log("[CHARGE] SPEED UP START");

            yield return new WaitForSeconds(chargeDuration);

            if (worldScroller != null)
                worldScroller.ResetSpeed();

            charging = false;
            Debug.Log("[CHARGE] SPEED UP END");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!charging || !other.CompareTag("Enemy"))
                return;

            if (hitEnemies.Contains(other.gameObject))
                return;

            hitEnemies.Add(other.gameObject);
            Debug.Log("[CHARGE] HIT " + other.name);

            other.GetComponent<EternalClash.Combat.DamageReceiver>()?.TakeDamage(damage);
            other.GetComponent<EternalClash.Combat.KnockbackReceiver>()?.ApplyKnockback(Vector2.left, knockbackForce);
            other.GetComponent<EnemyStatusController>()?.ApplyStun(stunDuration);
        }
    }
}
