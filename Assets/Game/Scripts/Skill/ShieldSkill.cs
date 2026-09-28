using UnityEngine;
using EternalClash.Player;
using EternalClash.Combat;
using EternalClash.Village;
using EternalClash.World;

namespace EternalClash.Skill
{
    public class ShieldSkill : SkillBase
    {
        [Header("Shield Stats")]
        public float damageReduction = 0.8f;
        public float shieldDuration = 1.0f;
        public int reflectDamage = 5;

        [Header("Postknight Block Walk")]
        [Tooltip("Toc do cuon the gioi khi giơ khiên so voi binh thuong (1 = normal). Postknight: van bu di cham thay vi dung yen.")]
        [SerializeField] private float walkSpeedMultiplier = 0.35f;

        public bool Active { get; private set; }

        private PlayerController playerController;
        private WorldScroller worldScroller;

        // Chi ha toc neu chinh Khien la thu da ha no. Tranh truong hop man
        // da ket thuc (StageCompleteController goi StopScroll) ma Khien lai bat len.
        private bool slowedWorld;

        protected override void Awake()
        {
            skillName = "Shield";
            cooldown = 5.0f;
            base.Awake();
            playerController = GetComponentInParent<PlayerController>();
        }

        public bool IsActive()
        {
            return Active;
        }

        protected override void Execute()
        {
            if (AlchemistUpgradeSystem.Instance != null)
                reflectDamage = AlchemistUpgradeSystem.Instance.GetShieldValue();

            Active = true;
            Debug.Log("[SKILL] Shield ACTIVATED (Duration: 1s, -80% DMG, Reflect 5 DMG, walk x" + walkSpeedMultiplier + ")");

            if (playerController != null)
                playerController.StopMovement();

            SlowWorld();

            CancelInvoke(nameof(DisableShield));
            Invoke(nameof(DisableShield), shieldDuration);
        }

        private void DisableShield()
        {
            Active = false;
            Debug.Log("[SKILL] Shield DEACTIVATED");

            if (playerController != null)
                playerController.ResumeMovement();

            RestoreWorld();
        }

        /// <summary>
        /// Block kieu Postknight: khong dung yen ma VAN DI CHAM. Player bi khoa vi
        /// tri, tien do = cuon nen, nen chi can ha speed multiplier xuong thap thay
        /// vi StopScroll(). Truoc day dung yen nguyen man hinh, khac voi Postknight
        /// (giuong khiên van bu tu tu).
        /// </summary>
        private void SlowWorld()
        {
            if (worldScroller == null)
                worldScroller = FindObjectOfType<WorldScroller>();

            if (worldScroller == null || !worldScroller.IsScrolling)
                return;

            worldScroller.SetSpeedMultiplier(walkSpeedMultiplier);
            slowedWorld = true;
        }

        private void RestoreWorld()
        {
            if (!slowedWorld)
                return;

            slowedWorld = false;

            if (worldScroller != null)
                worldScroller.ResetSpeed();
        }

        public int BlockDamage(int incomingDamage, GameObject attacker = null)
        {
            if (!Active)
                return incomingDamage;

            int mitigatedDamage = Mathf.RoundToInt(incomingDamage * (1f - damageReduction));
            if (mitigatedDamage < 1 && incomingDamage > 0)
                mitigatedDamage = 1;

            EternalClash.Audio.GameAudio.Play(EternalClash.Audio.SoundId.PlayerShieldBlock);

            // Reflect damage to attacker
            if (attacker != null && reflectDamage > 0)
            {
                CombatDamageResolver.Instance?.DealDamage(
                    attacker,
                    reflectDamage,
                    DamageSource.Reflect
                );
            }

            return mitigatedDamage;
        }

        public bool IsPerfectCounterWindow()
        {
            return Active;
        }
    }
}
