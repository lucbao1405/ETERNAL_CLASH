using System;
using UnityEngine;
using EternalClash.Character;
using EternalClash.Core.Save;

namespace EternalClash.Core
{
    public enum PlayerCondition
    {
        Normal = 0,
        Injured = 1
    }

    /// <summary>
    /// Owns the player's combat readiness state.
    /// Tracks injured condition, recovery over time, and gates battle access.
    /// </summary>
    public class PlayerConditionSystem : MonoBehaviour
    {
        public static PlayerConditionSystem Instance { get; private set; }

        public PlayerCondition Condition { get; private set; } = PlayerCondition.Normal;

        /// <summary>
        /// Hoi mau theo TUNG DOT: cu moi <see cref="RECOVERY_TICK_SECONDS"/> giay thi
        /// cong mot lan bang phan tram nay cua Max HP. Tinh theo phan tram nen nhan
        /// vat mau nhieu hay it deu hoi day trong cung mot khoang thoi gian.
        /// </summary>
        public const float DEFAULT_RECOVERY_RATE = 5f;

        /// <summary>Khoang cach giua hai dot hoi mau, tinh bang giay.</summary>
        public const float RECOVERY_TICK_SECONDS = 5f;

        /// <summary>Hoi den bao nhieu phan tram Max HP thi coi la khoi han.</summary>
        public const int DEFAULT_RECOVERY_TARGET_PERCENT = 100;

        /// <summary>Phan tram Max HP duoc hoi ngay khi ve lang sau khi chet.</summary>
        public const int REVIVE_HP_PERCENT = 10;

        public event Action<PlayerCondition> OnConditionChanged;
        public event Action<int, int> OnRecoveredHpChanged;
        public event Action OnRecoveryCompleted;

        private float recoveryRatePerSecond = DEFAULT_RECOVERY_RATE;
        private int recoveryTargetPercent = DEFAULT_RECOVERY_TARGET_PERCENT;
        private int currentHp;
        private int maxHp;
        private float recoveryAccumulator;

        public int CurrentHp => currentHp;
        public int MaxHp => maxHp;
        public float RecoveryRate => recoveryRatePerSecond;
        public int RecoveryTargetPercent => recoveryTargetPercent;
        public bool IsInjured => Condition == PlayerCondition.Injured;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// So HP hoi duoc trong mot dot. Lam tron len va toi thieu 1 de nhan vat
        /// mau thap khong bi ket vinh vien vi moi dot lam tron xuong thanh 0.
        /// </summary>
        private int HealPerTick()
        {
            return Mathf.Max(1, Mathf.CeilToInt(maxHp * recoveryRatePerSecond / 100f));
        }

        private void Update()
        {
            if (Condition != PlayerCondition.Injured)
                return;

            if (maxHp <= 0)
                return;

            int targetHp = Mathf.CeilToInt(maxHp * recoveryTargetPercent / 100f);

            if (currentHp >= targetHp)
            {
                CompleteRecovery();
                return;
            }

            // Hoi theo tung dot: dem thoi gian, du mot chu ky thi cong mot cuc.
            // recoveryAccumulator o day la SO GIAY da tich, khong phai so HP.
            recoveryAccumulator += Time.deltaTime;

            int ticks = Mathf.FloorToInt(recoveryAccumulator / RECOVERY_TICK_SECONDS);
            if (ticks <= 0)
                return;

            recoveryAccumulator -= ticks * RECOVERY_TICK_SECONDS;

            currentHp = Mathf.Min(currentHp + HealPerTick() * ticks, maxHp);
            OnRecoveredHpChanged?.Invoke(currentHp, maxHp);
            SyncToSave();

            if (currentHp >= targetHp)
                CompleteRecovery();
        }

        /// <summary>
        /// Danh dau nguoi choi da guc va bat dau hoi phuc. Mau duoc dat lai o
        /// <see cref="REVIVE_HP_PERCENT"/>% Max HP - nguoi choi chet voi 0 mau nhung
        /// ve lang thi hoi sinh voi mot phan mau, roi tu hoi tiep theo thoi gian.
        /// </summary>
        public void MarkInjured(int playerMaxHp)
        {
            maxHp = Mathf.Max(1, playerMaxHp);
            currentHp = Mathf.Clamp(
                Mathf.CeilToInt(maxHp * REVIVE_HP_PERCENT / 100f), 1, maxHp);
            recoveryRatePerSecond = DEFAULT_RECOVERY_RATE;
            recoveryTargetPercent = DEFAULT_RECOVERY_TARGET_PERCENT;
            recoveryAccumulator = 0f;

            SetCondition(PlayerCondition.Injured);

            var data = SaveManager.Instance?.Data;
            if (data != null)
            {
                data.maxHp = maxHp;
                data.recoveryStartUnixTime = NowUnix();
                data.recoveryRatePerSecond = recoveryRatePerSecond;
                data.recoveryTargetPercent = recoveryTargetPercent;
                SyncToSave();
            }

            Debug.Log($"[CONDITION] Player marked Injured. HP {currentHp}/{maxHp}, recovering to {recoveryTargetPercent}%.");
        }

        public void BeginRecoveryIfNeeded()
        {
            var data = SaveManager.Instance?.Data;
            if (data == null) return;

            if ((PlayerCondition)data.playerCondition != PlayerCondition.Injured)
                return;

            maxHp = Mathf.Max(1, data.maxHp);
            currentHp = Mathf.Clamp(data.currentHp, 0, maxHp);
            recoveryRatePerSecond = data.recoveryRatePerSecond > 0f ? data.recoveryRatePerSecond : DEFAULT_RECOVERY_RATE;
            recoveryTargetPercent = data.recoveryTargetPercent > 0 ? data.recoveryTargetPercent : DEFAULT_RECOVERY_TARGET_PERCENT;

            if (currentHp >= Mathf.CeilToInt(maxHp * recoveryTargetPercent / 100f))
            {
                CompleteRecovery();
                return;
            }

            SetCondition(PlayerCondition.Injured);
            ApplyElapsedOfflineRecovery(data);
        }

        public void ApplyElapsedOfflineRecovery(SaveData data)
        {
            if (data == null) return;
            if (data.recoveryStartUnixTime <= 0) return;

            long nowUnix = NowUnix();
            long elapsed = Math.Max(0L, nowUnix - data.recoveryStartUnixTime);
            if (elapsed <= 0) return;

            // Cung cach tinh voi Update(): dem xem da qua bao nhieu dot tron ven.
            int ticks = Mathf.FloorToInt(elapsed / RECOVERY_TICK_SECONDS);
            if (ticks <= 0) return;

            int recovered = HealPerTick() * ticks;
            if (recovered <= 0) return;

            int targetHp = Mathf.CeilToInt(maxHp * recoveryTargetPercent / 100f);
            currentHp = Mathf.Clamp(currentHp + recovered, 0, maxHp);
            data.currentHp = currentHp;
            SyncToSave();

            OnRecoveredHpChanged?.Invoke(currentHp, maxHp);

            if (currentHp >= targetHp)
            {
                CompleteRecovery();
            }
        }

        private void CompleteRecovery()
        {
            if (Condition == PlayerCondition.Normal) return;

            currentHp = Mathf.Max(currentHp, Mathf.CeilToInt(maxHp * recoveryTargetPercent / 100f));
            SetCondition(PlayerCondition.Normal);
            SaveManager.Instance?.Save();
            OnRecoveryCompleted?.Invoke();
            Debug.Log("[CONDITION] Player recovered from Injured.");
        }

        public bool CanStartBattle()
        {
            if (Condition == PlayerCondition.Normal) return true;
            if (maxHp <= 0) return true;

            int targetHp = Mathf.CeilToInt(maxHp * recoveryTargetPercent / 100f);
            return currentHp >= targetHp;
        }

        public string GetInjuredBlockReason()
        {
            int targetHp = Mathf.CeilToInt(maxHp * recoveryTargetPercent / 100f);
            return $"Character is injured.\nWait until HP recovers.\n({currentHp}/{targetHp})";
        }

        public void LoadFromSave(SaveData data)
        {
            if (data == null) return;

            maxHp = Mathf.Max(0, data.maxHp);
            currentHp = Mathf.Clamp(data.currentHp, 0, maxHp);
            recoveryRatePerSecond = data.recoveryRatePerSecond > 0f ? data.recoveryRatePerSecond : DEFAULT_RECOVERY_RATE;
            recoveryTargetPercent = data.recoveryTargetPercent > 0 ? data.recoveryTargetPercent : DEFAULT_RECOVERY_TARGET_PERCENT;
            recoveryAccumulator = 0f;

            var cond = (PlayerCondition)data.playerCondition;
            if (cond == PlayerCondition.Injured && maxHp > 0)
            {
                int targetHp = Mathf.CeilToInt(maxHp * recoveryTargetPercent / 100f);
                if (currentHp >= targetHp)
                {
                    data.playerCondition = (int)PlayerCondition.Normal;
                    SetCondition(PlayerCondition.Normal);
                }
                else
                {
                    SetCondition(PlayerCondition.Injured);
                }
            }
            else
            {
                SetCondition(PlayerCondition.Normal);
            }
        }

        public void SyncCurrentHp(int hp, int max)
        {
            maxHp = Mathf.Max(1, max);
            currentHp = Mathf.Clamp(hp, 0, maxHp);

            if (Condition == PlayerCondition.Injured)
            {
                int targetHp = Mathf.CeilToInt(maxHp * recoveryTargetPercent / 100f);
                if (currentHp >= targetHp)
                {
                    CompleteRecovery();
                }
            }

            SyncToSave();
        }

        private void SetCondition(PlayerCondition next)
        {
            if (Condition == next) return;
            Condition = next;
            OnConditionChanged?.Invoke(next);
        }

        private void SyncToSave()
        {
            var data = SaveManager.Instance?.Data;
            if (data == null) return;

            data.playerCondition = (int)Condition;
            data.currentHp = currentHp;
            data.maxHp = maxHp;
            data.recoveryRatePerSecond = recoveryRatePerSecond;
            data.recoveryTargetPercent = recoveryTargetPercent;
            if (Condition == PlayerCondition.Injured && data.recoveryStartUnixTime <= 0)
            {
                data.recoveryStartUnixTime = NowUnix();
            }
        }

        private static long NowUnix()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
