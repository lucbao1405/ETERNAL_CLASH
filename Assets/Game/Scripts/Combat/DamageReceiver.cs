using UnityEngine;
using EternalClash.Character;
using EternalClash.Skill;
using EternalClash.Player;

namespace EternalClash.Combat
{
    public class DamageReceiver : MonoBehaviour
    {
        private HealthSystem healthSystem;
        private ShieldSkill shieldSkill;
        private PlayerController playerController;

        private float damageMultiplier = 1f;
        private bool isDead;

        private void Awake()
        {
            healthSystem = GetComponent<HealthSystem>();
            shieldSkill = GetComponent<ShieldSkill>();
            playerController = GetComponent<PlayerController>();
        }

        public void TakeDamage(int damage)
        {
            if (healthSystem == null || isDead)
                return;

            float finalDamage = damage * damageMultiplier;

            if (shieldSkill != null)
                finalDamage = shieldSkill.ReduceDamage(finalDamage);

            healthSystem.TakeDamage(Mathf.RoundToInt(finalDamage));

            if (healthSystem.IsDead)
                HandleDeath();
        }

        private void HandleDeath()
        {
            isDead = true;

            if (playerController != null)
                playerController.StopMovement();

            InputController inputController = GetComponent<InputController>();
            if (inputController != null)
                inputController.enabled = false;

            BasicAttack basicAttack = GetComponent<BasicAttack>();
            if (basicAttack != null)
                basicAttack.enabled = false;

            StageManager.Instance?.FailStage();
        }

        public void SetDamageMultiplier(float multiplier)
        {
            damageMultiplier = multiplier;
        }

        public void ResetDamageMultiplier()
        {
            damageMultiplier = 1f;
        }
    }
}
