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

            EnemyAI ai = GetComponent<EnemyAI>();
            if (ai != null)
                ai.enabled = false;

            EnemyAttack attack = GetComponent<EnemyAttack>();
            if (attack != null)
                attack.enabled = false;

            EnemyMover mover = GetComponent<EnemyMover>();
            if (mover != null)
                mover.StopMovement();

            yield return new WaitForSeconds(duration);

            if (attack != null)
                attack.enabled = true;

            if (ai != null)
                ai.enabled = true;

            stunned = false;
        }
    }
}
