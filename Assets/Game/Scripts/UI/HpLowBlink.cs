using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.UI
{
    /// <summary>
    /// Gan vao object thanh mau ("Hp" trong Town). Khi Player bam GO ma mau chua du
    /// de vao tran, lop ruot do (Hp_Fill) nhap nhay de bao "chua du mau".
    ///
    /// Hp_Fill do TownStatPanelController tao luc chay, nen script tim lai moi lan
    /// nhap nhay thay vi giu san tham chieu.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HpLowBlink : MonoBehaviour
    {
        [Header("Nhap nhay")]
        [Tooltip("So lan nhap nhay.")]
        [SerializeField, Min(1)] private int blinkCount = 3;
        [Tooltip("Thoi gian mot lan nhap nhay (mo roi sang lai), tinh bang giay.")]
        [SerializeField, Min(0.05f)] private float blinkDuration = 0.18f;
        [Tooltip("Do mo nhat khi tat (0 = an han, 1 = khong doi).")]
        [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.15f;

        private static readonly List<HpLowBlink> active = new List<HpLowBlink>();

        private Image fill;
        private Coroutine routine;

        // Mau goc cua ruot do, ghi nho MOT lan. Truoc day moi lan nhap nhay deu doc
        // mau hien tai lam goc: bam GO lien tuc luc dang mo se lay chinh mau mo do lam
        // goc (0.15 -> 0.02 -> ...) nen thanh mau toi dan roi bien mat.
        private Color baseColor = Color.white;
        private bool baseCaptured;

        private void OnEnable()
        {
            if (!active.Contains(this))
                active.Add(this);
        }

        private void OnDisable()
        {
            active.Remove(this);
            RestoreAlpha();
        }

        /// <summary>Cho moi thanh mau dang hien nhap nhay (goi khi bam GO ma thieu mau).</summary>
        public static void BlinkAll()
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i] != null)
                    active[i].Blink();
            }
        }

        public void Blink()
        {
            if (!isActiveAndEnabled)
                return;

            // Dang nhap nhay do: dung lai va tra ve mau goc truoc khi bat dau lan moi.
            StopBlink();
            routine = StartCoroutine(BlinkRoutine());
        }

        private void StopBlink()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            if (fill != null && baseCaptured)
                fill.color = baseColor;
        }

        private IEnumerator BlinkRoutine()
        {
            Image resolved = ResolveFill();
            if (resolved == null)
            {
                routine = null;
                yield break;
            }

            // Doi sang Image khac (vd Hp_Fill vua duoc tao) thi ghi nho lai mau goc.
            if (!baseCaptured || resolved != fill)
            {
                fill = resolved;
                baseColor = fill.color;
                baseCaptured = true;
            }

            float half = Mathf.Max(0.02f, blinkDuration * 0.5f);

            for (int i = 0; i < blinkCount; i++)
            {
                yield return Fade(baseColor.a, baseColor.a * dimAlpha, half);
                yield return Fade(baseColor.a * dimAlpha, baseColor.a, half);
            }

            fill.color = baseColor;
            routine = null;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            // Dung thoi gian thuc de khong bi dung khi game tam dung.
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (fill == null)
                    yield break;

                float a = Mathf.Lerp(from, to, t / duration);
                fill.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                yield return null;
            }

            if (fill != null)
                fill.color = new Color(baseColor.r, baseColor.g, baseColor.b, to);
        }

        private Image ResolveFill()
        {
            Transform child = transform.Find(HpBarSprites.FillObjectName);
            if (child != null)
                return child.GetComponent<Image>();

            // Chua co lop ruot (vd chua vao Town xong): nhap nhay chinh anh thanh mau.
            return GetComponent<Image>();
        }

        private void RestoreAlpha()
        {
            StopBlink();
        }
    }
}
