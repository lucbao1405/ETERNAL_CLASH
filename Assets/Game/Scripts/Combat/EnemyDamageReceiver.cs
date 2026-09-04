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

        public void TakeDamage(int damage)
        {
            if (health == null)
                return;

            Debug.Log("[ENEMY DAMAGE] " + gameObject.name + " take " + damage);

            health.TakeDamage(damage);

            if (impact != null)
                impact.EnemyHit(gameObject, damage);

            if (EternalClash.UI.DamagePopupManager.Instance != null)
                EternalClash.UI.DamagePopupManager.Instance.ShowDamage(transform.position + Vector3.up, damage, false);
            else if (DamagePopup.Instance != null)
                DamagePopup.Instance.Show(transform.position + Vector3.up, damage);
        }
    }
}
