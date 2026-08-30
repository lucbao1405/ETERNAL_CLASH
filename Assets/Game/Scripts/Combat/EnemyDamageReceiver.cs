using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Combat
{
    public class EnemyDamageReceiver : MonoBehaviour
    {
        private HealthSystem health;
        private ImpactFeedbackSystem impact;

        private void Awake()
        {
            health = GetComponent<HealthSystem>();
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

            if (DamagePopup.Instance != null)
                DamagePopup.Instance.Show(transform.position + Vector3.up, damage);
        }
    }
}
