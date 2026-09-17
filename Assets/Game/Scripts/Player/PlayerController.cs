using UnityEngine;
using EternalClash.Combat;

namespace EternalClash.Player
{
    /// <summary>
    /// Main player controller.
    /// Handles player state and connection with combat systems.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        private AutoRunner autoRunner;
        private CombatController combatController;

        public AutoRunner AutoRunner => autoRunner;
        public CombatController CombatController => combatController;

        private void Awake()
        {
            autoRunner = GetComponent<AutoRunner>();
            combatController = GetComponent<CombatController>();
        }

        public void StopMovement()
        {
            if (autoRunner != null)
            {
                autoRunner.StopRunning();
            }
        }

        public void ResumeMovement()
        {
            if (autoRunner != null)
            {
                autoRunner.ResumeRunning();
            }
        }
    }
}
