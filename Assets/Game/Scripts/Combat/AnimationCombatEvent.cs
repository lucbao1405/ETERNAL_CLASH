using UnityEngine;

namespace EternalClash.Combat
{
    /// <summary>
    /// Bridge between animation hit frames and combat damage.
    /// Damage happens only at the animation hit frame.
    /// </summary>
    public class AnimationCombatEvent : MonoBehaviour
    {
        private CombatController combatController;
        private PlayerCombatStateMachine state;

        private void Awake()
        {
            combatController = GetComponent<CombatController>();
            state = GetComponent<PlayerCombatStateMachine>();
        }

        public void OnAttackHitFrame()
        {
            if (combatController != null)
                combatController.PerformAttackHit();
        }

        public void OnAttackAnimationEnd()
        {
            if (state != null)
                state.ReturnToCombatIdle();
        }
    }
}
