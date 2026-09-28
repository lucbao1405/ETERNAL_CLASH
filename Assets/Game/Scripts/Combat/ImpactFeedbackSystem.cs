using UnityEngine;

namespace EternalClash.Combat
{
    public class ImpactFeedbackSystem : MonoBehaviour
    {
        private static ImpactFeedbackSystem _instance;

        public static ImpactFeedbackSystem Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<ImpactFeedbackSystem>();

                if (_instance == null)
                {
                    var obj = new GameObject("_ImpactFeedbackSystem");
                    _instance = obj.AddComponent<ImpactFeedbackSystem>();
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else if (_instance != this)
                Destroy(gameObject);
        }

        [Header("Impact Settings")]
        public float lightHitShake = 0.05f;
        public float heavyHitShake = 0.12f;

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
