namespace EternalClash.Audio
{
    /// <summary>
    /// Cac su kien am thanh trong game. Clip cho tung su kien duoc gan trong
    /// Resources/SoundLibrary.asset (khong hard-code clip trong code).
    /// Them su kien moi: them vao CUOI enum de gia tri cu trong SoundLibrary khong bi lech.
    /// </summary>
    public enum SoundId
    {
        None = 0,

        // UI
        UIClick = 1,

        // Player
        PlayerAttack = 10,
        PlayerCharge = 11,
        PlayerShieldUp = 12,
        PlayerShieldBlock = 13,
        PlayerPotion = 14,
        PlayerHurt = 15,
        PlayerDeath = 16,
        PlayerLevelUp = 17,

        // Nhat do
        PickupCoin = 30,
        PickupGem = 31,
        PickupMaterial = 32,

        // Ket qua tran
        ChestOpen = 40,
        Win = 41,
        Lose = 42,

        // Lang
        StatSelect = 50,
        UpgradeAccept = 51,
        ItemUpgrade = 52,

        // Truot (cuon) panel mo/dong o Town
        PanelScroll = 53
    }

    /// <summary>Loai am thanh cua quai, gan theo tung quai trong SoundLibrary.</summary>
    public enum EnemySound
    {
        Attack,
        Hit,
        Death
    }
}
