using UnityEngine;

namespace EternalClash.Combat
{
    public enum PlayerCombatState
    {
        Run,
        CombatIdle,
        Attack,
        Charge,
        Shield,
        Hit,
        Dead
    }

    public class PlayerCombatStateMachine : MonoBehaviour
    {
        public PlayerCombatState CurrentState { get; private set; } = PlayerCombatState.Run;

        public bool CanUseSkill =>
            CurrentState == PlayerCombatState.Run ||
            CurrentState == PlayerCombatState.CombatIdle;

        public void ChangeState(PlayerCombatState state)
        {
            if (CurrentState == PlayerCombatState.Dead)
                return;

            CurrentState = state;
            Debug.Log("[PLAYER STATE] " + state);
        }

        public void EnterCombat()
        {
            ChangeState(PlayerCombatState.CombatIdle);
        }

        public void Attack()
        {
            if (!CanUseSkill) return;
            ChangeState(PlayerCombatState.Attack);
        }

        public void Charge()
        {
            if (!CanUseSkill) return;
            ChangeState(PlayerCombatState.Charge);
        }

        public void Shield()
        {
            if (!CanUseSkill) return;
            ChangeState(PlayerCombatState.Shield);
        }

        public void Hit()
        {
            if (CurrentState == PlayerCombatState.Dead)
                return;

            ChangeState(PlayerCombatState.Hit);
        }

        public void ReturnToCombatIdle()
        {
            if (CurrentState != PlayerCombatState.Dead)
                ChangeState(PlayerCombatState.CombatIdle);
        }

        public void Die()
        {
            CurrentState = PlayerCombatState.Dead;
            Debug.Log("[PLAYER STATE] DEAD");
        }
    }
}
