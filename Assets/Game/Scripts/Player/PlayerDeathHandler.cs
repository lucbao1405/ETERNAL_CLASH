using UnityEngine;
using EternalClash.Character;
using EternalClash.Combat;
using EternalClash.World;
using EternalClash.Enemy;

namespace EternalClash.Player
{
    public class PlayerDeathHandler : MonoBehaviour
    {
        private HealthSystem healthSystem;
        private DamageReceiver damageReceiver;
        private bool notified;

        private void Awake()
        {
            healthSystem = GetComponent<HealthSystem>();
            damageReceiver = GetComponent<DamageReceiver>();
        }

        private void OnEnable()
        {
            notified = false;
            if (healthSystem != null)
                healthSystem.OnDeath += OnHealthSystemDeath;
        }

        private void OnDisable()
        {
            if (healthSystem != null)
                healthSystem.OnDeath -= OnHealthSystemDeath;
        }

        private void OnHealthSystemDeath()
        {
            if (notified) return;
            notified = true;

            var combatStateMachine = GetComponent<PlayerCombatStateMachine>();
            if (combatStateMachine != null)
                combatStateMachine.Die();

            var knockbackReceiver = GetComponent<KnockbackReceiver>();
            if (knockbackReceiver != null)
                knockbackReceiver.CancelRecovery();

            var worldScroller = FindObjectOfType<WorldScroller>();
            if (worldScroller != null)
            {
                worldScroller.CancelKnockback();
                worldScroller.StopScroll();
            }

            EnemyMover[] movers = FindObjectsOfType<EnemyMover>();
            foreach (EnemyMover mover in movers)
                mover.StopMovement();

            StageManager.Instance?.FailStage();
        }

        private void Update()
        {
            if (notified || healthSystem == null) return;
            if (healthSystem.IsDead)
            {
                OnHealthSystemDeath();
            }
        }
    }
}
