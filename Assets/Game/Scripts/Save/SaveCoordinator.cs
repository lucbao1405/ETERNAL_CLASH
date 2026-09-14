using UnityEngine;

namespace EternalClash.Core.Save
{
    /// <summary>
    /// Compatibility-safe save entry point for gameplay systems. SaveManager remains
    /// available to legacy callers while new code requests persistence here.
    ///
    /// RequestSave() khong ghi ngay: SaveManager gom cac yeu cau va ghi o cuoi frame,
    /// toi da 1 lan moi giay, va ghi het khi app xuong nen / thoat. Truoc day moi dong
    /// xu nhat duoc deu ghi save xuong may + tinh lai trang bi, gay giat khung hinh.
    /// </summary>
    public static class SaveCoordinator
    {
        public static void RequestSave()
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogWarning("[SAVE] Save requested before SaveManager was initialized.");
                return;
            }

            SaveManager.Instance.RequestSave();
        }

        /// <summary>Ghi ngay cac yeu cau dang cho (dung truoc khi doi scene / ket thuc tran).</summary>
        public static void Flush()
        {
            SaveManager.Instance?.FlushPendingSave();
        }
    }
}
