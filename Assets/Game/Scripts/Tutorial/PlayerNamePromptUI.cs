using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.Tutorial
{
    /// <summary>
    /// Bang nhap ten dung san trong scene Town (Canvas/Man_Hinh_Khac/Nhap_Ten).
    /// UI duoc dung cap trong editor, script chi dieu khien mo/dong va ghi ten.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerNamePromptUI : MonoBehaviour
    {
        private const int MaxNameLength = 24;

        [SerializeField] private TMP_InputField nameInput;

        /// <summary>Bam nut Xac Nhan voi ten hop le. Ten da duoc Trim.</summary>
        public event System.Action<string> NameSubmitted;

        private void Awake()
        {
            if (nameInput == null)
                nameInput = GetComponentInChildren<TMP_InputField>(true);
        }

        public void Show(string currentName)
        {
            transform.SetAsLastSibling();
            gameObject.SetActive(true);

            if (nameInput != null)
            {
                nameInput.text = currentName ?? string.Empty;
                nameInput.ActivateInputField();
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>Wire vao nut Xac Nhan (Button OnClick) ngay trong editor.</summary>
        public void Submit()
        {
            if (nameInput == null)
                return;

            string nameValue = SanitizeName(nameInput.text);
            if (string.IsNullOrWhiteSpace(nameValue))
                return;

            NameSubmitted?.Invoke(nameValue);
        }

        /// <summary>Bo ky tu dieu khien va the rich text (< >), gioi han do dai truoc khi luu.</summary>
        private static string SanitizeName(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            var sb = new System.Text.StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                if (!char.IsControl(c) && c != '<' && c != '>')
                    sb.Append(c);
            }

            string cleaned = sb.ToString().Trim();
            return cleaned.Length <= MaxNameLength
                ? cleaned
                : cleaned.Substring(0, MaxNameLength).Trim();
        }
    }
}
