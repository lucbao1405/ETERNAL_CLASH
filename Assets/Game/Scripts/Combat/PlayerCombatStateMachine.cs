using UnityEngine;
using EternalClash.World;

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

        private WorldScroller worldScroller;

        public bool CanUseSkill
        {
            get
            {
                if (CurrentState == PlayerCombatState.Hit) return false;
                if (CurrentState == PlayerCombatState.Dead) return false;

                if (worldScroller == null)
                    worldScroller = FindObjectOfType<WorldScroller>();
                if (worldScroller != null && worldScroller.IsKnockbackActive) return false;

                return CurrentState == PlayerCombatState.Run ||
                       CurrentState == PlayerCombatState.CombatIdle;
            }
        }

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
            // Sau hoi sinh, CurrentState dang la Dead nen phai cho phep thoat
            // khoi Dead o day; neu khong skill se bi khoa mai sau khi revive.
            CurrentState = PlayerCombatState.CombatIdle;
            Debug.Log("[PLAYER STATE] " + CurrentState);
        }

        public void Die()
        {
            CurrentState = PlayerCombatState.Dead;
            Debug.Log("[PLAYER STATE] DEAD");
        }
    }
}
