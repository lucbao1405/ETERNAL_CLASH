using UnityEngine;

namespace EternalClash.Combat
{
    public enum CombatFlowState
    {
        None,
        PlayerTurn,
        EnemyTurn,
        Resolving,
        Finished
    }

    public class CombatFlowCoordinator : MonoBehaviour
    {
        public CombatFlowState State { get; private set; } = CombatFlowState.None;
        public bool IsCombatActive => State != CombatFlowState.None && State != CombatFlowState.Finished;

        private EnemyAttackTimingController currentEnemy;

        public void StartCombat(EnemyAttackTimingController enemy)
        {
            currentEnemy = enemy;
            State = CombatFlowState.EnemyTurn;
            Debug.Log("[COMBAT FLOW] Combat Started");
        }

        public void OpenPlayerResponse()
        {
            if (!IsCombatActive)
                return;

            State = CombatFlowState.PlayerTurn;
            Debug.Log("[COMBAT FLOW] Player Response Window");
        }

        public void Resolve()
        {
            State = CombatFlowState.Resolving;
            Debug.Log("[COMBAT FLOW] Resolving Result");
        }

        public void EndCombat()
        {
            currentEnemy = null;
            State = CombatFlowState.Finished;
            Debug.Log("[COMBAT FLOW] Combat End");
        }

        public void ResetFlow()
        {
            State = CombatFlowState.None;
        }
    }
}
