using UnityEngine;

using EternalClash.Core.Services;
using EternalClash.Village;
using EternalClash.Core.Save;


namespace EternalClash.Core
{

    /// <summary>
    /// Composition Root của game.
    /// Chịu trách nhiệm khởi tạo các hệ thống nền.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {


        private static GameBootstrap instance;



        [Header("Core References")]


        [SerializeField]
        private PlayerStatSystem playerStatSystem;



        [SerializeField]
        private EquipmentSystem equipmentSystem;



        [SerializeField]
        private GoldSystem goldSystem;

        [SerializeField]
        private BlacksmithCraftingSystem blacksmithCraftingSystem;

        [SerializeField]
        private AlchemistUpgradeSystem alchemistUpgradeSystem;



        [SerializeField]
        private SaveManager saveManager;



        /// <summary>
        /// Đảm bảo các hệ thống lõi (Save, PlayerStat, Gold, Equipment,
        /// PlayerCondition) tồn tại ngay khi game bắt đầu, bất kể scene đầu
        /// tiên là scene nào. Host nằm trong DontDestroyOnLoad nên sống xuyên
        /// suốt Town -> Battle -> Town mà không tạo duplicate.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureBootstrapped()
        {
            if (instance != null)
                return;

            GameObject go = new GameObject("GameSystems");
            go.AddComponent<GameBootstrap>();
        }



        private void Awake()
        {

            if(instance != null)
            {
                Destroy(gameObject);
                return;
            }


            instance = this;


            DontDestroyOnLoad(gameObject);


            EnsureCoreSystems();


            InitializeCore();


            LoadSavedData();

        }




        /// <summary>
        /// Tạo các hệ thống lõi trên chính host này nếu scene không cung cấp sẵn.
        /// SaveManager được tạo trước để SaveData sẵn sàng khi các hệ thống khác load.
        /// </summary>
        private void EnsureCoreSystems()
        {

            if(saveManager == null)
                saveManager = gameObject.AddComponent<SaveManager>();


            if(playerStatSystem == null)
                playerStatSystem = gameObject.AddComponent<PlayerStatSystem>();


            if(goldSystem == null)
                goldSystem = gameObject.AddComponent<GoldSystem>();


            if(equipmentSystem == null)
                equipmentSystem = gameObject.AddComponent<EquipmentSystem>();

            if(blacksmithCraftingSystem == null)
                blacksmithCraftingSystem = gameObject.AddComponent<BlacksmithCraftingSystem>();

            if(alchemistUpgradeSystem == null)
                alchemistUpgradeSystem = gameObject.AddComponent<AlchemistUpgradeSystem>();


            if(PlayerConditionSystem.Instance == null && GetComponent<PlayerConditionSystem>() == null)
                gameObject.AddComponent<PlayerConditionSystem>();

        }




        /// <summary>
        /// Load toàn bộ dữ liệu đã lưu vào các hệ thống sau khi SaveManager đã nạp SaveData.
        /// </summary>
        private void LoadSavedData()
        {

            var data = saveManager != null ? saveManager.Data : null;
            if(data == null)
                return;

            if (saveManager != null && !saveManager.HasSaveData())
            {
                NewGameEquipmentDefaults.Apply(data);
                saveManager.Save();
            }


            if(playerStatSystem != null)
                playerStatSystem.LoadFromSave(data);


            if(goldSystem != null)
                goldSystem.LoadFromSave(data);

            if(blacksmithCraftingSystem != null)
                blacksmithCraftingSystem.LoadFromSave(data);

            if(alchemistUpgradeSystem != null)
                alchemistUpgradeSystem.LoadFromSave(data);


            PlayerConditionSystem.Instance?.LoadFromSave(data);


            if(equipmentSystem != null)
                equipmentSystem.RefreshFromSave();

            EternalClash.UI.BlacksmithShopUI[] blacksmithUis =
                FindObjectsOfType<EternalClash.UI.BlacksmithShopUI>(true);
            foreach(EternalClash.UI.BlacksmithShopUI blacksmithUi in blacksmithUis)
                blacksmithUi.RefreshCurrentView();

        }




        private void InitializeCore()
        {

            RegisterPlayerStats();


            RegisterEquipment();


            RegisterGold();


            RegisterSave();



            Debug.Log(
                "Core Services Initialized"
            );

        }




        private void RegisterPlayerStats()
        {

            if(playerStatSystem == null)
            {
                playerStatSystem =
                    FindObjectOfType<PlayerStatSystem>();
            }


            if(playerStatSystem == null)
            {
                Debug.LogError(
                    "Missing PlayerStatSystem"
                );

                return;
            }



            if(!ServiceRegistry.Has<PlayerStatSystem>())
            {
                ServiceRegistry.Register(
                    playerStatSystem
                );
            }

        }




        private void RegisterEquipment()
        {

            if(equipmentSystem == null)
            {
                equipmentSystem =
                    FindObjectOfType<EquipmentSystem>();
            }



            if(equipmentSystem == null)
            {
                Debug.LogError(
                    "Missing EquipmentSystem"
                );

                return;
            }



            if(!ServiceRegistry.Has<EquipmentSystem>())
            {
                ServiceRegistry.Register(
                    equipmentSystem
                );
            }

        }




        private void RegisterGold()
        {

            if(goldSystem == null)
            {
                goldSystem =
                    FindObjectOfType<GoldSystem>();
            }



            if(goldSystem == null)
            {
                Debug.LogError(
                    "Missing GoldSystem"
                );

                return;
            }



            if(!ServiceRegistry.Has<GoldSystem>())
            {
                ServiceRegistry.Register(
                    goldSystem
                );
            }

        }




        private void RegisterSave()
        {

            if(saveManager == null)
            {
                saveManager =
                    FindObjectOfType<SaveManager>();
            }



            if(saveManager == null)
            {
                Debug.LogError(
                    "Missing SaveManager"
                );

                return;
            }



            if(!ServiceRegistry.Has<ISaveService>())
            {
                ServiceRegistry.Register<ISaveService>(
                    saveManager
                );
            }

        }

    }

}
