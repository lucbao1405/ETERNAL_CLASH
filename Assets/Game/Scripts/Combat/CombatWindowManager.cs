using UnityEngine;

namespace EternalClash.Combat
{
    /// <summary>
    /// Controls the short combat lock window used by auto-run stages.
    /// Enemy approaching pauses stage progress, combat resolves, then stage resumes.
    /// </summary>
    public class CombatWindowManager : MonoBehaviour
    {
        public enum CombatPhase
        {
            None,
            Approach,
            PrepareAttack,
            AttackWindow,
            Resolve,
            End
        }

        public CombatPhase Phase { get; private set; } = CombatPhase.None;

        public bool IsCombatActive => Phase != CombatPhase.None && Phase != CombatPhase.End;

        public void StartCombat(GameObject enemy)
        {
            if (enemy == null) return;

            Phase = CombatPhase.Approach;
            Debug.Log("[COMBAT WINDOW] Enemy entered combat range");

            Invoke(nameof(PrepareEnemyAttack), 0.35f);
        }

        private void PrepareEnemyAttack()
        {
            Phase = CombatPhase.PrepareAttack;
            Debug.Log("[COMBAT WINDOW] Enemy preparing attack");

            Invoke(nameof(OpenAttackWindow), 0.5f);
        }

        private void OpenAttackWindow()
        {
            Phase = CombatPhase.AttackWindow;
            Debug.Log("[COMBAT WINDOW] Attack frame opened - Player can counter");
        }

        public void PlayerAction(string action)
        {
            if (!IsCombatActive) return;

            Debug.Log("[COMBAT WINDOW] Player action: " + action);
            Phase = CombatPhase.Resolve;
        }

        public void EndCombat()
        {
            Phase = CombatPhase.End;
            Debug.Log("[COMBAT WINDOW] Combat finished - Resume stage");

            Invoke(nameof(Clear), 0.2f);
        }

        private void Clear()
        {
            Phase = CombatPhase.None;
        }
    }
}
