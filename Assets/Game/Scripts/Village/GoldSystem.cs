using UnityEngine;
using EternalClash.Core.Save;

namespace EternalClash.Village
{
    /// <summary>Cac loai nguyen lieu che tao trong game.</summary>
    public enum MaterialType
    {
        Ore,
        Leather,
        Wood,
        Steel
    }

    public class GoldSystem : MonoBehaviour
    {
        public static GoldSystem Instance { get; private set; }

        public int Gold { get; private set; }

        /// <summary>Kim cuong - tien te hiem, nhan tu phan thuong cuoi man.</summary>
        public int Gem { get; private set; }

        public int OreMaterial { get; private set; }
        public int LeatherMaterial { get; private set; }
        public int WoodMaterial { get; private set; }
        public int SteelOreMaterial { get; private set; }

        public event System.Action<int> OnGoldChanged;
        public event System.Action<int> OnGemChanged;
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

        /// <summary>
        /// Tong vang nhat duoc ke tu dau tran hien tai. Popup ket qua doc gia tri
        /// nay de bao dung so kiem duoc, thay vi cong them mot khoan thuong rieng.
        /// Chi dem phan CONG vao, khong tru khi tieu tien.
        /// </summary>
        public int SessionGoldEarned { get; private set; }

        /// <summary>Dat lai bo dem dau tran. StageManager goi khi bat dau man.</summary>
        public void ResetSessionCounters()
        {
            SessionGoldEarned = 0;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;

            Gold += amount;
            SessionGoldEarned += amount;
            OnGoldChanged?.Invoke(Gold);
            SyncSave();
            Debug.Log($"[GOLD] +{amount} -> Total: {Gold}");
        }

        public bool SpendGold(int amount)
        {
            if (amount <= 0) return false;
            if (Gold < amount) return false;
            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            SyncSave();
            return true;
        }

        public void AddGem(int amount)
        {
            if (amount <= 0) return;

            Gem += amount;
            OnGemChanged?.Invoke(Gem);
            SyncSave();
            Debug.Log($"[GEM] +{amount} -> Total: {Gem}");
        }

        public bool SpendGem(int amount)
        {
            if (amount <= 0) return false;
            if (Gem < amount) return false;

            Gem -= amount;
            OnGemChanged?.Invoke(Gem);
            SyncSave();
            return true;
        }

        /// <summary>
        /// Cong mot loai nguyen lieu. Dung cho vat pham nhat duoc tren ban do -
        /// moi vat pham chi cho mot loai nen khong can ham nhieu tham so.
        /// </summary>
        public void AddMaterial(MaterialType type, int amount)
        {
            if (amount <= 0) return;

            switch (type)
            {
                case MaterialType.Ore: OreMaterial += amount; break;
                case MaterialType.Leather: LeatherMaterial += amount; break;
                case MaterialType.Wood: WoodMaterial += amount; break;
                case MaterialType.Steel: SteelOreMaterial += amount; break;
            }

            OnMaterialsChanged?.Invoke(OreMaterial, LeatherMaterial);
            SyncSave();
            Debug.Log($"[MATERIALS] +{amount} {type} -> Total: {GetMaterial(type)}");
        }

        public int GetMaterial(MaterialType type)
        {
            switch (type)
            {
                case MaterialType.Ore: return OreMaterial;
                case MaterialType.Leather: return LeatherMaterial;
                case MaterialType.Wood: return WoodMaterial;
                case MaterialType.Steel: return SteelOreMaterial;
                default: return 0;
            }
        }

        public bool SpendMaterial(MaterialType type, int amount)
        {
            if (amount <= 0) return false;
            if (GetMaterial(type) < amount) return false;

            switch (type)
            {
                case MaterialType.Ore: OreMaterial -= amount; break;
                case MaterialType.Leather: LeatherMaterial -= amount; break;
                case MaterialType.Wood: WoodMaterial -= amount; break;
                case MaterialType.Steel: SteelOreMaterial -= amount; break;
            }

            OnMaterialsChanged?.Invoke(OreMaterial, LeatherMaterial);
            SyncSave();
            return true;
        }

        public void AddMaterials(int ore, int leather)
        {
            if (ore <= 0 && leather <= 0) return;
            ore = Mathf.Max(0, ore);
            leather = Mathf.Max(0, leather);
            OreMaterial += ore;
            LeatherMaterial += leather;
            OnMaterialsChanged?.Invoke(OreMaterial, LeatherMaterial);
            SyncSave();
            Debug.Log($"[MATERIALS] +{ore} Ore, +{leather} Leather");
        }

        public bool SpendMaterials(int ore, int leather)
        {
            if (ore <= 0 && leather <= 0) return false;
            if (ore < 0 || leather < 0) return false;
            if (OreMaterial < ore || LeatherMaterial < leather) return false;
            OreMaterial -= ore;
            LeatherMaterial -= leather;
            OnMaterialsChanged?.Invoke(OreMaterial, LeatherMaterial);
            SyncSave();
            return true;
        }

        public void LoadFromSave(SaveData data)
        {
            if (data == null) return;

            Gold = data.gold;
            Gem = data.gem;
            OreMaterial = data.oreMaterial;
            LeatherMaterial = data.leatherMaterial;
            WoodMaterial = data.woodMaterial;
            SteelOreMaterial = data.steelOre;

            // Bao cho UI biet gia tri vua duoc nap, neu khong thi man hinh van giu
            // chu placeholder cho toi lan thay doi dau tien.
            OnGoldChanged?.Invoke(Gold);
            OnGemChanged?.Invoke(Gem);
            OnMaterialsChanged?.Invoke(OreMaterial, LeatherMaterial);
        }

        private void SyncSave()
        {
            if (SaveManager.Instance?.Data != null)
            {
                SaveManager.Instance.Data.gold = Gold;
                SaveManager.Instance.Data.gem = Gem;
                SaveManager.Instance.Data.oreMaterial = OreMaterial;
                SaveManager.Instance.Data.leatherMaterial = LeatherMaterial;
                SaveManager.Instance.Data.woodMaterial = WoodMaterial;
                SaveManager.Instance.Data.steelOre = SteelOreMaterial;
                SaveManager.Instance.Data.copperOre = OreMaterial;
                SaveManager.Instance.Data.wolfHide = LeatherMaterial;
                SaveCoordinator.RequestSave();
            }
        }
    }
}
