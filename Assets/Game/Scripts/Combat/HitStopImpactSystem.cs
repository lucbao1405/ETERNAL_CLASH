using UnityEngine;

namespace EternalClash.Combat
{
    public class HitStopImpactSystem : MonoBehaviour
    {
        public static HitStopImpactSystem Instance;

        [Header("Impact Settings")]
        public float lightHitPause = 0.03f;
        public float heavyHitPause = 0.08f;

        private void Awake()
        {
            Instance = this;
        }

        public void LightHit()
        {
            ApplyHitStop(lightHitPause);
        }

        public void HeavyHit()
        {
            ApplyHitStop(heavyHitPause);
        }

        private void ApplyHitStop(float duration)
        {
            Debug.Log("[IMPACT] Hit stop " + duration + "s");
        }
    }
}
