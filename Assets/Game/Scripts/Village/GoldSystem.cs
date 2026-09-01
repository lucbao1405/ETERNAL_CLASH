using UnityEngine;

namespace EternalClash.Village
{
    public class GoldSystem : MonoBehaviour
    {
        public static GoldSystem Instance { get; private set; }

        public int Gold { get; private set; }
        public int OreMaterial { get; private set; }
        public int LeatherMaterial { get; private set; }

        public event System.Action<int> OnGoldChanged;
        public event System.Action<int, int> OnMaterialsChanged;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
            SyncSave();
            Debug.Log($"[GOLD] +{amount} -> Total: {Gold}");
        }

        public bool SpendGold(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            SyncSave();
            return true;
        }

        public void AddMaterials(int ore, int leather)
        {
            OreMaterial += ore;
            LeatherMaterial += leather;
            OnMaterialsChanged?.Invoke(OreMaterial, LeatherMaterial);
            SyncSave();
            Debug.Log($"[MATERIALS] +{ore} Ore, +{leather} Leather");
        }

        public bool SpendMaterials(int ore, int leather)
        {
            if (OreMaterial < ore || LeatherMaterial < leather) return false;
            OreMaterial -= ore;
            LeatherMaterial -= leather;
            OnMaterialsChanged?.Invoke(OreMaterial, LeatherMaterial);
            SyncSave();
            return true;
        }

        public void LoadFromSave(SaveData data)
        {
            Gold = data.gold;
            OreMaterial = data.oreMaterial;
            LeatherMaterial = data.leatherMaterial;
        }

        private void SyncSave()
        {
            if (SaveManager.Instance?.Data != null)
            {
                SaveManager.Instance.Data.gold = Gold;
                SaveManager.Instance.Data.oreMaterial = OreMaterial;
                SaveManager.Instance.Data.leatherMaterial = LeatherMaterial;
                SaveManager.Instance.Save();
            }
        }
    }
}
