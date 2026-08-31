using UnityEngine;
using EternalClash.Character;
using EternalClash.Skill;
using EternalClash.Player;
using EternalClash.UI;

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
            int initialDamage = Mathf.RoundToInt(finalDamage);
            int dealtDamage = initialDamage;

            if (shieldSkill != null)
            {
                dealtDamage = shieldSkill.BlockDamage(initialDamage);

                int blocked = initialDamage - dealtDamage;
                if (blocked > 0 && DamagePopupManager.Instance != null)
                {
                    DamagePopupManager.Instance.ShowBlock(
                        transform.position,
                        blocked);
                }
            }

            healthSystem.TakeDamage(dealtDamage);

            if (DamagePopupManager.Instance != null)
            {
                DamagePopupManager.Instance.ShowDamage(
                    transform.position,
                    dealtDamage,
                    GetComponent<PlayerController>() != null);
            }

            if (healthSystem.IsDead)
                HandleDeath();
        }

        public bool IsDead()
        {
            return isDead;
        }

        private void HandleDeath()
        {
            isDead = true;

            if (playerController != null)
                playerController.StopMovement();

            InputController inputController = GetComponent<InputController>();
            if (inputController != null)
                inputController.enabled = false;

            BasicAttackSystem basicAttackSystem = GetComponent<BasicAttackSystem>();
            if (basicAttackSystem != null)
                basicAttackSystem.enabled = false;

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            foreach (Collider2D col in colliders)
                col.enabled = false;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.simulated = false;
            }

            SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sprite in sprites)
                sprite.enabled = false;

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
