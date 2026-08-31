using UnityEngine;

namespace EternalClash.Enemy
{
    public class EnemyDeathController : MonoBehaviour
    {
        private bool dead;
        private Collider2D[] colliders;
        private Animator animator;

        private void Awake()
        {
            colliders = GetComponentsInChildren<Collider2D>();
            animator = GetComponent<Animator>();
        }

        public void Die()
        {
            if (dead) return;
            dead = true;

            Debug.Log("[ENEMY] Death animation start: " + gameObject.name);

            var decision = GetComponent<EnemyDecisionController>();
            if (decision != null)
                decision.Die();

            foreach (var col in colliders)
                col.enabled = false;

            if (animator != null)
                animator.SetTrigger("Die");

            Invoke(nameof(RemoveEnemy), 0.8f);
        }

        private void RemoveEnemy()
        {
            Destroy(gameObject);
        }
    }
}
