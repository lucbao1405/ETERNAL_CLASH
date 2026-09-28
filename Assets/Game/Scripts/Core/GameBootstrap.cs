using UnityEngine;

using EternalClash.Core.Services;
using EternalClash.Village;
using EternalClash.Core.Save;
using EternalClash.Tutorial;


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
        /// <summary>FPS muc tieu. Android/iOS mac dinh chi chay 30 FPS neu khong dat.</summary>
        private const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureBootstrapped()
        {
            ApplyFrameRate();

            if (instance != null)
                return;

            GameObject go = new GameObject("GameSystems");
            go.AddComponent<GameBootstrap>();
        }

        /// <summary>
        /// Tat VSync de targetFrameRate co tac dung (muc chat luong Medium/High cua
        /// Android dang bat VSync, khi do Unity bo qua targetFrameRate).
        /// </summary>
        private static void ApplyFrameRate()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;

            // Ban build chinh thuc: bo Debug.Log thuong (ghi log + stack trace ton CPU
            // tren dien thoai), van giu Warning / Error de doc loi qua logcat.
            // Ban Development Build va Unity Editor van hien day du log.
            if (!Debug.isDebugBuild)
                Debug.unityLogger.filterLogType = LogType.Warning;
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

            // AffinityManager (thu/hao cam tu ruong + milestone thu) truoc day khong
            // duoc tao o day nen ChestRewardSystem.AddAffinityPoints luon la no-op.
            if(GetComponent<AffinityManager>() == null)
                gameObject.AddComponent<AffinityManager>();

            if(PlayerConditionSystem.Instance == null && GetComponent<PlayerConditionSystem>() == null)
                gameObject.AddComponent<PlayerConditionSystem>();

            if (GetComponent<MobilePlatformController>() == null)
                gameObject.AddComponent<MobilePlatformController>();

        }




        /// <summary>
        /// Lan truoc app bi tat ngang tran (vuot khoi da nhiem, het pin...): tinh nhu
        /// thua - bi thuong, ve lang voi 10% HP. Do da nhat van giu, giong luat thua.
        /// Trong Unity Editor chi xoa co, khong phat: bam Stop giua tran khi test la chuyen
        /// thuong xuyen.
        /// </summary>
        private void ApplyAbandonedBattlePenalty(SaveData data)
        {
            if (!data.battleInProgress)
                return;

            data.battleInProgress = false;

#if !UNITY_EDITOR
            int maxHp = playerStatSystem != null ? playerStatSystem.TotalMaxHealth : 100;
            PlayerConditionSystem.Instance?.MarkInjured(maxHp);
            Debug.LogWarning("[STAGE] Lan truoc thoat app giua tran -> tinh la thua, nhan vat bi thuong.");
#endif

            SaveCoordinator.RequestSave();
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

            AffinityManager.Instance?.LoadFromSave(data);


            PlayerConditionSystem.Instance?.LoadFromSave(data);

            ApplyAbandonedBattlePenalty(data);

            if(equipmentSystem != null)
                equipmentSystem.RefreshFromSave();

            // TutorialManager persists its initial checkpoint. It must be created only
            // after the normal new-game defaults have been applied to this save.
            if (GetComponent<TutorialManager>() == null)
                gameObject.AddComponent<TutorialManager>();

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
