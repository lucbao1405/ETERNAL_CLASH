using UnityEngine;

namespace EternalClash.Combat
{
    public class CombatVFXController : MonoBehaviour
    {
        public static CombatVFXController Instance { get; private set; }

        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float destroyTime = 0.5f;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
                Instance = this;
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
            Debug.Log("[VFX] Screen shake " + power);
        }
    }
}
