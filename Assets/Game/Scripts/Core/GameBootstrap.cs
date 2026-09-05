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
        private SaveManager saveManager;




        private void Awake()
        {

            if(instance != null)
            {
                Destroy(gameObject);
                return;
            }


            instance = this;


            DontDestroyOnLoad(gameObject);



            InitializeCore();

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