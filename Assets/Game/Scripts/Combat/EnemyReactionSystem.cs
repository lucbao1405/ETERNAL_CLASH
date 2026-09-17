using UnityEngine;

namespace EternalClash.Combat
{
    public class EnemyReactionSystem : MonoBehaviour
    {
        [Header("Reaction Settings")]
        public float knockbackDistance = 0.35f;
        public float stunDuration = 0.8f;

        private bool stunned;

        public void ReceiveHit(float damage, bool canStun = false)
        {
            Debug.Log($"[ENEMY HIT] Damage: {damage}");

            PlayHitEffect();

            if (canStun)
            {
                ApplyStun();
            }
        }

        public void ApplyCounterReaction()
        {
            Debug.Log("[ENEMY COUNTERED]");

            // Combat state (Stunned) is handled by EnemyAttackTimingController / EnemyStatusController.
            // This system only handles visual reaction and movement feedback.
            ApplyKnockback();
        }

        private void ApplyStun()
        {
            stunned = true;
            Debug.Log("[ENEMY STUN]");
            Invoke(nameof(Recover), stunDuration);
        }

        private void ApplyKnockback()
        {
            transform.position += Vector3.right * knockbackDistance;
        }

        private void Recover()
        {
            stunned = false;
            Debug.Log("[ENEMY RECOVER]");
        }

        private void PlayHitEffect()
        {
            Debug.Log("[ENEMY FLASH + HIT STOP]");
        }

        public bool IsStunned()
        {
            return stunned;
        }
    }
}
