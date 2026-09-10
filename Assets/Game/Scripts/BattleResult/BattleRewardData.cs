using System.Collections.Generic;
using EternalClash.Data;
using EternalClash.UI;

namespace EternalClash.BattleResult
{
    /// <summary>
    /// Dữ liệu phần thưởng cuối trận.
    ///
    ///  - battleLoot : item nhặt được trong trận (Ore/Leather/Wood từ ItemPickup).
    ///  - chestReward: phần thưởng từ rương cuối màn.
    ///  - gold / exp : tổng vàng & kinh nghiệm nhận được trong trận (đã bao gồm
    ///                 phần thưởng vàng của rương nếu có).
    ///  - battleTime : thời gian trận đấu (giây).
    ///
    /// Khi hiển thị Win Popup, battleLoot và chestReward được gộp cùng loại
    /// (GetMergedRewards) để không tạo item duplicate.
    /// </summary>
    public class BattleRewardData
    {
        public readonly List<ItemReward> battleLoot = new List<ItemReward>();
        public readonly List<ItemReward> chestReward = new List<ItemReward>();

        public int gold;
        public int exp;
        public float battleTime;

        /// <summary>
        /// Gộp battleLoot + chestReward, cộng dồn quantity các item trùng loại.
        /// So sánh theo itemId khi có, ngược lại so sánh tham chiếu ItemData.
        /// </summary>
        public List<ItemReward> GetMergedRewards()
        {
            var merged = new List<ItemReward>();
            AddRange(merged, battleLoot);
            AddRange(merged, chestReward);
            return merged;
        }

        /// <summary>
        /// Gộp để hiển thị ở Win Popup: giống GetMergedRewards nhưng bỏ các entry
        /// Vàng (itemId "Gold") vì vàng được hiển thị riêng ở dòng Gold.
        /// </summary>
        public List<ItemReward> GetWinDisplayItems()
        {
            var merged = new List<ItemReward>();
            AddRangeDisplay(merged, battleLoot);
            AddRangeDisplay(merged, chestReward);
            return merged;
        }

        private static void AddRangeDisplay(List<ItemReward> target, IEnumerable<ItemReward> source)
        {
            if (target == null || source == null)
                return;

            foreach (ItemReward entry in source)
            {
                if (entry == null || entry.item == null)
                    continue;
                if (string.Equals(entry.item.itemId, "Gold", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                AddOrMerge(target, entry);
            }
        }

        /// <summary>Thêm một entry vào list, cộng dồn nếu đã có item trùng loại.</summary>
        public static void AddOrMerge(List<ItemReward> list, ItemReward entry)
        {
            if (list == null || entry == null)
                return;

            if (entry.item == null)
            {
                list.Add(entry);
                return;
            }

            foreach (ItemReward existing in list)
            {
                if (existing == null || existing.item == null)
                    continue;

                if (!SameIdentity(existing, entry))
                    continue;

                existing.quantity += entry.quantity;
                return;
            }

            list.Add(entry);
        }

        private static void AddRange(List<ItemReward> target, IEnumerable<ItemReward> source)
        {
            if (target == null || source == null)
                return;

            foreach (ItemReward entry in source)
                AddOrMerge(target, entry);
        }

        private static bool SameIdentity(ItemReward a, ItemReward b)
        {
            if (a == null || b == null)
                return false;
            if (a.item == null || b.item == null)
                return false;

            bool aHasId = !string.IsNullOrEmpty(a.item.itemId);
            bool bHasId = !string.IsNullOrEmpty(b.item.itemId);

            if (aHasId && bHasId)
                return string.Equals(a.item.itemId, b.item.itemId, System.StringComparison.Ordinal);

            return ReferenceEquals(a.item, b.item);
        }

        /// <summary>
        /// Tạo một ItemReward từ dữ liệu tối giản (dùng cho Ore/Leather/Wood/Gem
        /// không có catalog riêng). ItemData này chỉ phục vụ hiển thị tên/số lượng.
        /// </summary>
        public static ItemReward CreateEntry(string itemId, string itemName, int quantity, string iconSpriteName = null)
        {
            var data = new ItemData
            {
                itemId = itemId ?? string.Empty,
                itemName = itemName ?? itemId,
                itemType = string.Equals(itemId, "Gold", System.StringComparison.OrdinalIgnoreCase)
                    ? "Currency"
                    : "Material",
                iconSpriteName = iconSpriteName ?? string.Empty
            };

            return new ItemReward(data, quantity < 1 ? 1 : quantity);
        }
    }
}
