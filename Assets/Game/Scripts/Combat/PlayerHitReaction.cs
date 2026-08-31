using System.Collections;
using UnityEngine;

namespace EternalClash.Combat
{
    public class PlayerHitReaction : MonoBehaviour
    {
        public float hitLockTime = 0.25f;

        private bool locked;

        public bool IsLocked => locked;

        public void PlayHit()
        {
            if (!locked)
                StartCoroutine(HitRoutine());
        }

        private IEnumerator HitRoutine()
        {
            locked = true;
            Debug.Log("[PLAYER] Hit reaction");
            yield return new WaitForSeconds(hitLockTime);
            locked = false;
        }
    }
}
