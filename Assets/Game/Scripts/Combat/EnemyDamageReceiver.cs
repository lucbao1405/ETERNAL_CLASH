using UnityEngine;
using EternalClash.Enemy;

namespace EternalClash.Combat
{
    public class EnemyDamageReceiver : MonoBehaviour
    {
        private EnemyHealthSystem health;
        private ImpactFeedbackSystem impact;

        private void Awake()
        {
            health = GetComponent<EnemyHealthSystem>();
            impact = ImpactFeedbackSystem.Instance;
        }

        // Tham so isCritical co gia tri mac dinh nen moi loi goi cu van bien dich.
        public void TakeDamage(int damage, bool isCritical = false)
        {
            if (health == null)
                return;

            Debug.Log("[ENEMY DAMAGE] " + gameObject.name + " take " + damage + (isCritical ? " (CRIT)" : ""));

            health.TakeDamage(damage);

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
    }
}
