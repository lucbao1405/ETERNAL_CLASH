using UnityEngine;

namespace EternalClash.Combat
{
    public class ImpactFeedbackSystem : MonoBehaviour
    {
        public static ImpactFeedbackSystem Instance;

        [Header("Impact Settings")]
        public float lightHitShake = 0.05f;
        public float heavyHitShake = 0.12f;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void EnemyHit(GameObject enemy, int damage, bool heavy = false)
        {
            if (enemy == null) return;

            Debug.Log($"[IMPACT] Enemy Hit {enemy.name} Damage={damage} Heavy={heavy}");

            Flash(enemy);
        }

        private void Flash(GameObject target)
        {
            var flash = target.GetComponent<EnemyFlashEffect>();
            if (flash != null)
                flash.Play();
        }

        public void PlayerHit()
        {
            Debug.Log("[IMPACT] Player Hit");
        }
    }
}
