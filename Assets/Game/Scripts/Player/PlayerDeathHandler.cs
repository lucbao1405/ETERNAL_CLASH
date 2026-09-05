using UnityEngine;
using EternalClash.Character;
using EternalClash.Combat;

namespace EternalClash.Player
{
    /// <summary>
    /// Bridges HealthSystem death to the defeat flow.
    /// DamageReceiver already calls StageManager.FailStage() which starts the defeat UI.
    /// This handler is a safety net so defeated players always reach the defeat flow.
    /// </summary>
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
