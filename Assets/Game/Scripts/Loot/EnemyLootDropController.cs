using UnityEngine;

public class EnemyLootDropController : MonoBehaviour
{
    [SerializeField] private GameObject[] dropPrefabs;
    [SerializeField] private int goldAmount = 5;

    public void DropLoot()
    {
        if (dropPrefabs.Length > 0)
        {
            int index = Random.Range(0, dropPrefabs.Length);
            Instantiate(dropPrefabs[index], transform.position, Quaternion.identity);
        }

        // gold system will connect here
    }
}
