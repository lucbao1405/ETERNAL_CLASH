using UnityEngine;

public class EnemyDeathHandler : MonoBehaviour
{
    [SerializeField] private EnemyLootDropController lootDrop;
    private bool dead;

    public void Die()
    {
        if (dead) return;
        dead = true;

        if (lootDrop != null)
            lootDrop.DropLoot();

        EnemyManager.Instance?.UnregisterEnemy(this.gameObject);

        // Animation-layer support: if this enemy exposes a death animation,
        // stop its gameplay and let the death clip play before removal.
        var visual = GetComponentInChildren<EternalClash.Animation.IEnemyDeathVisual>(true);
        if (visual != null)
        {
            DisableGameplay();
            float duration = visual.PlayDeath();
            if (duration <= 0f)
                duration = 1f;
            Destroy(gameObject, duration);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void DisableGameplay()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            if (col != null)
                col.enabled = false;
        }

        EternalClash.Enemy.EnemyMover mover = GetComponent<EternalClash.Enemy.EnemyMover>();
        if (mover != null)
            mover.StopMovement();

        EternalClash.Enemy.EnemyController controller = GetComponent<EternalClash.Enemy.EnemyController>();
        if (controller != null)
            controller.StopMove();

        EternalClash.Combat.EnemyAttack attack = GetComponent<EternalClash.Combat.EnemyAttack>();
        if (attack != null)
            attack.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.simulated = false;
    }
}
