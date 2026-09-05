namespace EternalClash.Core.Services
{

    /// <summary>
    /// Interface quản lý trang bị Player.
    /// Gameplay chỉ làm việc thông qua interface này.
    /// </summary>
    public interface IEquipmentService
    {

        string CurrentWeaponId { get; }


        string CurrentArmorId { get; }


        string CurrentAccessoryId { get; }



        bool EquipWeapon(string itemId);



        bool EquipArmor(string itemId);



        bool EquipAccessory(string itemId);



        bool RemoveEquipment(string slot);



        bool HasEquipment(string itemId);

    }

}