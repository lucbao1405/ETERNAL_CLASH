using System.Collections;
using UnityEngine;
using EternalClash.Combat;

namespace EternalClash.Enemy
{
    public class EnemyStatusController : MonoBehaviour
    {
        private bool stunned;

        public bool IsStunned()
        {
            return stunned;
        }

        public void ApplyStun(float duration)
        {
            if (!stunned)
                StartCoroutine(StunRoutine(duration));
        }

        private IEnumerator StunRoutine(float duration)
        {
            stunned = true;

            EnemyDecisionController decision = GetComponent<EnemyDecisionController>();
            if (decision != null)
                decision.enabled = false;

            EnemyAttackTimingController attackTiming = GetComponent<EnemyAttackTimingController>();
            if (attackTiming != null)
                attackTiming.enabled = false;

            EnemyMover mover = GetComponent<EnemyMover>();
            if (mover != null)
                mover.StopMovement();

            yield return new WaitForSeconds(duration);

            if (attackTiming != null)
                attackTiming.enabled = true;

            if (decision != null)
                decision.enabled = true;

            stunned = false;
        }
    }
}
