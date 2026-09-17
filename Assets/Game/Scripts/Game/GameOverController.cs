using UnityEngine;

namespace EternalClash.Game
{
    public class GameOverController : MonoBehaviour
    {
        public static bool IsGameOver { get; private set; }

        public void PlayerDied()
        {
            if (IsGameOver) return;

            IsGameOver = true;
            Debug.Log("[GAME OVER] Player defeated");
            DisableBattleInput();

            // StageManager.FailStage is the single defeat entry point. This component
            // only disables local input; DamageReceiver/PlayerDeathHandler report death.
        }

        private void OnDestroy()
        {
            IsGameOver = false;
        }

        private void DisableBattleInput()
        {
            var skills = FindObjectsOfType<EternalClash.Skill.SkillManager>();

            foreach (var skill in skills)
            {
                skill.enabled = false;
            }

            Debug.Log("[GAME OVER] Skill buttons disabled");
        }
    }
}
