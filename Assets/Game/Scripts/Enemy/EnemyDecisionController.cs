using UnityEngine;

namespace EternalClash.Enemy
{
    public enum EnemyDecisionState
    {
        Waiting,
        Approaching,
        Combat,
        Stunned,
        Dead
    }

    /// <summary>
    /// Decision layer for enemy AI.
    /// Does not move or deal damage directly.
    /// It only decides what the enemy should do.
    /// </summary>
    public class EnemyDecisionController : MonoBehaviour
    {
        public EnemyDecisionState State { get; private set; } = EnemyDecisionState.Waiting;

        public bool CanAct => State != EnemyDecisionState.Dead && State != EnemyDecisionState.Stunned;

        public void EnterCombat()
        {
            if (State == EnemyDecisionState.Dead) return;
            State = EnemyDecisionState.Combat;
        }

        public void SetApproach()
        {
            if (State == EnemyDecisionState.Dead) return;
            State = EnemyDecisionState.Approaching;
        }

        public void Stun()
        {
            if (State == EnemyDecisionState.Dead) return;
            State = EnemyDecisionState.Stunned;
        }

        public void Recover()
        {
            if (State == EnemyDecisionState.Dead) return;
            State = EnemyDecisionState.Combat;
        }

        public void Die()
        {
            State = EnemyDecisionState.Dead;
        }
    }
}
