using UnityEngine;
using EternalClash.Enemy;

namespace EternalClash.Combat
{
    public enum EnemyCombatState
    {
        Idle,
        Approach,
        PrepareAttack,
        Attack,
        Recover,
        Stunned,
        Dead
    }

    public class EnemyCombatController : MonoBehaviour
    {
        [SerializeField] private EnemyCombatProfile profile;

        public EnemyCombatState CurrentState { get; private set; } = EnemyCombatState.Idle;
        public bool CanBeCountered => CurrentState == EnemyCombatState.PrepareAttack;

        public void EnterCombat()
        {
            ChangeState(EnemyCombatState.PrepareAttack);
        }

        public void StartAttack()
        {
            if (CurrentState != EnemyCombatState.PrepareAttack)
                return;

            ChangeState(EnemyCombatState.Attack);
        }

        public void Recover()
        {
            if (CurrentState == EnemyCombatState.Dead)
                return;

            ChangeState(EnemyCombatState.Recover);
        }

        public void ApplyStun()
        {
            if (CurrentState == EnemyCombatState.Dead)
                return;

            ChangeState(EnemyCombatState.Stunned);
        }

        public void Die()
        {
            ChangeState(EnemyCombatState.Dead);
        }

        public float GetAttackDamage()
        {
            return profile != null ? profile.attackDamage : 5f;
        }

        public float GetPrepareTime()
        {
            return profile != null ? profile.prepareTime : 0.6f;
        }

        private void ChangeState(EnemyCombatState state)
        {
            CurrentState = state;
            Debug.Log("[ENEMY STATE] " + state);
        }
    }
}
