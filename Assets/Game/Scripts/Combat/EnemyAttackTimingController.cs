using UnityEngine;

namespace EternalClash.Combat
{
    public class EnemyAttackTimingController : MonoBehaviour
    {
        public enum AttackState
        {
            Idle,
            Prepare,
            Warning,
            AttackFrame,
            Recover,
            Stunned
        }

        [SerializeField] private float prepareTime = 0.6f;
        [SerializeField] private float warningTime = 0.35f;
        [SerializeField] private float recoverTime = 1f;

        public AttackState State { get; private set; } = AttackState.Idle;
        public bool CanHit { get; private set; }

        private float stateTimer;

        public bool IsPreparingAttack(float window)
        {
            if (State != AttackState.Prepare && State != AttackState.Warning)
                return false;

            // Counter window only becomes valid near the attack moment.
            // Prepare is still a valid interrupt window, Warning is the perfect window.
            return stateTimer <= window || State == AttackState.Warning;
        }

        public void CancelAttack()
        {
            StopAllCoroutines();
            CanHit = false;
            State = AttackState.Recover;
        }

        public void ApplyStun(float duration)
        {
            StopAllCoroutines();
            StartCoroutine(StunRoutine(duration));
        }

        private System.Collections.IEnumerator StunRoutine(float duration)
        {
            State = AttackState.Stunned;
            CanHit = false;
            yield return new WaitForSeconds(duration);
            State = AttackState.Idle;
        }

        public void StartAttack()
        {
            if (State != AttackState.Idle) return;
            StartCoroutine(AttackRoutine());
        }

        private System.Collections.IEnumerator AttackRoutine()
        {
            State = AttackState.Prepare;
            stateTimer = prepareTime;
            CanHit = false;
            yield return new WaitForSeconds(prepareTime);

            State = AttackState.Warning;
            stateTimer = warningTime;
            yield return new WaitForSeconds(warningTime);

            State = AttackState.AttackFrame;
            stateTimer = 0f;
            CanHit = true;
            yield return new WaitForSeconds(0.1f);

            CanHit = false;
            State = AttackState.Recover;
            yield return new WaitForSeconds(recoverTime);

            State = AttackState.Idle;
        }
    }
}
