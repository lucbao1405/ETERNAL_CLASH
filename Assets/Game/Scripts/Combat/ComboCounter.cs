using System;
using UnityEngine;

namespace EternalClash.Combat
{
    /// <summary>
    /// Dem so don danh lien tiep trong cua so thoi gian (Postknight combo).
    /// Het gio khong ghi don moi thi combo reset ve 0. Combo tu 2 tro len
    /// hien popup "x3" tai vi tri quai bi danh.
    /// </summary>
    public class ComboCounter : MonoBehaviour
    {
        private static ComboCounter instance;

        public static ComboCounter Instance
        {
            get
            {
                if (instance == null)
                    instance = new GameObject("_ComboCounter").AddComponent<ComboCounter>();
                return instance;
            }
        }

        /// <summary>Go Interface muon: 0 = het combo.</summary>
        public event Action<int> OnComboChanged;

        [Tooltip("So giay khong ghi don moi thi combo reset")]
        [SerializeField] private float decayWindow = 1.2f;
        [SerializeField] private int maxCombo = 99;

        public int Current { get; private set; }
        private float timer;

        public static void RegisterHit(Vector3 hitPosition)
        {
            Instance.Register(hitPosition);
        }

        private void Register(Vector3 hitPosition)
        {
            Current = Mathf.Min(Current + 1, maxCombo);
            timer = decayWindow;
            OnComboChanged?.Invoke(Current);

            // Don dau tien khong can bao combo.
            if (Current >= 2 && EternalClash.UI.DamagePopupManager.Instance != null)
                EternalClash.UI.DamagePopupManager.Instance.ShowCombo(hitPosition, Current);
        }

        private void Update()
        {
            if (Current <= 0)
                return;

            if (timer > 0f)
                timer -= Time.deltaTime;

            if (timer <= 0f)
                ResetCombo();
        }

        public void ResetCombo()
        {
            if (Current == 0)
                return;

            Current = 0;
            timer = 0f;
            OnComboChanged?.Invoke(0);
        }
    }
}
