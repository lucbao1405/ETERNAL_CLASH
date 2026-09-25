namespace EternalClash.Enemy
{
    /// <summary>
    /// Trang thai thu thach boss chon tu panel "select boss" o Town (chi ton tai
    /// trong phien choi, khong luu save): khi Active thi Battle spawn dung 1 con
    /// quai duoc chon voi nhieu mau hon binh thuong thay vi cac wave thuong, va
    /// thang se khong tang stageLevel (choi lai thoai mai).
    /// </summary>
    public static class BossChallenge
    {
        /// <summary>He so chi so (mau, thuong roi ra, exp) cua boss thu thach so voi con quai cung loai binh thuong.</summary>
        public const float HpMultiplier = 2f;

        public static bool Active { get; private set; }

        /// <summary>Ten nut duoc bam o panel Town ("slime", "wolf", "goblin mage", "goblin boss").</summary>
        public static string EnemyId { get; private set; }

        public static void Start(string enemyId)
        {
            EnemyId = string.IsNullOrEmpty(enemyId) ? null : enemyId.ToLowerInvariant().Trim();
            Active = EnemyId != null;
        }

        /// <summary>Huy thu thach (nut GO thuong o Town, hoac khong tim thay prefab khi vao tran).</summary>
        public static void Cancel()
        {
            Active = false;
            EnemyId = null;
        }

        /// <summary>Match ten nut o panel voi ten prefab quai ("goblin boss" ung voi prefab "Boss").</summary>
        public static bool MatchesPrefab(string prefabName, string wanted)
        {
            string name = string.IsNullOrEmpty(prefabName) ? "" : prefabName.ToLowerInvariant().Trim();
            if (name == wanted)
                return true;

            return wanted == "goblin boss" && name == "boss";
        }
    }
}
