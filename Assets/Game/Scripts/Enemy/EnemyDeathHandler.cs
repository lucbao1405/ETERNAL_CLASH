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

        Destroy(gameObject);
    }
}
