using UnityEngine;

namespace EternalClash.Combat
{
    public class CombatVFXController : MonoBehaviour
    {
        private static CombatVFXController _instance;

        public static CombatVFXController Instance
        {
            get
            {
                // Tu tao giong ImpactFeedbackSystem: scene khong can setup gi,
                // shake/hit VFX luon san sang khi combat gay ra.
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CombatVFXController>();
                    if (_instance == null)
                    {
                        var obj = new GameObject("_CombatVFXController");
                        _instance = obj.AddComponent<CombatVFXController>();
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float destroyTime = 0.5f;

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else if (_instance != this)
                Destroy(gameObject);
        }

        public void PlayHitEffect(Vector3 position)
        {
            if (hitEffectPrefab == null)
                return;

            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            Destroy(effect, destroyTime);
        }

        public void Shake(float power = 0.05f)
        {
            // Da bo rung camera: nguoi choi phan hoi rung lam mat. De ham rong
            // de giu API cho cac caller (crit/counter/knockback) khong phai sua rai.
        }
    }
}
