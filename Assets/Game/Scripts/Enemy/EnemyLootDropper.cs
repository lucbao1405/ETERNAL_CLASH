using UnityEngine;

namespace EternalClash.Enemy
{
    [System.Serializable]
    public class DropItem
    {
        [Tooltip("Ten item - chi de ghi chu, hien thi cho de nhin trong Inspector")]
        public string itemName;

        [Tooltip("Prefab vat pham se Instantiate khi trung ti le roi")]
        public GameObject prefab;

        [Range(0f, 100f)]
        [Tooltip("Ti le roi (%). Vi du 25 nghia la 25% co hoi roi item nay moi lan Enemy chet")]
        public float dropChance = 10f;
    }

    /// <summary>
    /// Gan vao Enemy. Goi DropLoot() dung 1 lan khi Enemy chet - duyet qua danh sach,
    /// moi item tu quay ti le rieng, khong loai tru lan nhau (co the roi 0, 1 hoac nhieu item).
    /// </summary>
    public class EnemyLootDropper : MonoBehaviour
    {
        [SerializeField] private DropItem[] lootTable;

        [Tooltip("Sorting layer se duoc gan cho SpriteRenderer cua item vua roi ra")]
        [SerializeField] private string itemSortingLayer = "Item";

        public void DropLoot()
        {
            if (lootTable == null || lootTable.Length == 0)
                return;

            foreach (DropItem item in lootTable)
            {
                if (item == null || item.prefab == null)
                    continue;

                float roll = Random.Range(0f, 100f);
                if (roll > item.dropChance)
                    continue;

                GameObject drop = Instantiate(item.prefab, transform.position, Quaternion.identity);

                // Cung cha voi Enemy de Item troi theo Map neu Enemy dang la con cua
                // 1 container Map/World nao do (SetParent(worldPositionStays: true) de
                // khong bi nhay vi tri khi doi cha).
                drop.transform.SetParent(transform.parent, true);

                SpriteRenderer sr = drop.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.sortingLayerName = itemSortingLayer;
            }
        }
    }
}
