using UnityEngine;
using EternalClash.Enemy;

namespace EternalClash.Combat
{
    public class EnemyDamageReceiver : MonoBehaviour
    {
        private EnemyHealthSystem health;
        private ImpactFeedbackSystem impact;
        private EnemyAttack attack;
        private EnemyKnockbackReceiver knockback;
        private Transform player;

        private void Awake()
        {
            health = GetComponent<EnemyHealthSystem>();
            impact = ImpactFeedbackSystem.Instance;
            attack = GetComponent<EnemyAttack>();
            knockback = GetComponent<EnemyKnockbackReceiver>();
            // Prefab khong gan: tu them de moi quai trung don bi day lui (Postknight).
            if (knockback == null)
                knockback = gameObject.AddComponent<EnemyKnockbackReceiver>();
        }

        // Tham so isCritical co gia tri mac dinh nen moi loi goi cu van bien dich.
        public void TakeDamage(int damage, bool isCritical = false)
        {
            if (health == null)
                return;

            Debug.Log("[ENEMY DAMAGE] " + gameObject.name + " take " + damage + (isCritical ? " (CRIT)" : ""));

            health.TakeDamage(damage);

            // Postknight: trung don thi bi day lui khoi player; neu dang ngam
            // (telegraph) thi don bi pha. Chet thi khong con day lui nua.
            if (!health.IsDead)
            {
                attack?.InterruptAttack();
                knockback?.ApplyEnemyKnockback(GetKnockbackDirection());
            }

            // Hit stop + rung man hinh dung tai day vi moi sat thuong vao quai
            // (thuong, chi mang, charge, phan don) deu di qua cho nay.
            if (isCritical)
            {
                HitStopImpactSystem.Instance?.HeavyHit();
                CombatVFXController.Instance?.Shake(0.12f);
            }
            else
            {
                HitStopImpactSystem.Instance?.LightHit();
            }

            if (impact != null)
                impact.EnemyHit(gameObject, damage);

            var popupManager = EternalClash.UI.DamagePopupManager.Instance;
            if (popupManager != null)
            {
                if (isCritical)
                    popupManager.ShowCriticalDamage(transform.position + Vector3.up, damage);
                else
                    popupManager.ShowDamage(transform.position + Vector3.up, damage, false);
            }
            else if (DamagePopup.Instance != null)
            {
                DamagePopup.Instance.Show(transform.position + Vector3.up, damage);
            }
        }

        private Vector2 GetKnockbackDirection()
        {
            if (player == null)
            {
                GameObject obj = GameObject.FindGameObjectWithTag("Player");
                player = obj != null ? obj.transform : null;
            }

            if (player == null)
                return Vector2.right;

            return transform.position.x >= player.position.x ? Vector2.right : Vector2.left;
        }
    }
}
