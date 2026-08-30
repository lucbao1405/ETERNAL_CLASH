using UnityEngine;

namespace EternalClash.Combat
{
    public class CombatVFXController : MonoBehaviour
    {
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float destroyTime = 0.5f;

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
