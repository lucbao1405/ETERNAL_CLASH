using UnityEngine;

namespace EternalClash.Core
{
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            EnsureSystem<EternalClash.Village.GoldSystem>();
            EnsureSystem<EternalClash.Village.PlayerStatSystem>();
            EnsureSystem<EternalClash.Village.BlacksmithCraftingSystem>();
            EnsureSystem<EternalClash.Village.AffinityManager>();
            EnsureSystem<EternalClash.Village.EquipmentSystem>();
        }

        private void Start()
        {
            var saveData = SaveManager.Instance?.Data;
            if (saveData == null) return;

            EternalClash.Village.GoldSystem.Instance?.LoadFromSave(saveData);
            EternalClash.Village.PlayerStatSystem.Instance?.LoadFromSave(saveData);
            EternalClash.Village.BlacksmithCraftingSystem.Instance?.LoadFromSave(saveData);
            EternalClash.Village.AffinityManager.Instance?.LoadFromSave(saveData);
            EternalClash.Village.EquipmentSystem.Instance?.RefreshFromSave();
        }

        private void EnsureSystem<T>() where T : Component
        {
            if (FindObjectOfType<T>() == null)
            {
                new GameObject(typeof(T).Name).AddComponent<T>();
            }
        }
    }
}
