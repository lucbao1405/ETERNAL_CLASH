using System.Collections;
using UnityEngine;

namespace EternalClash.Combat
{
    /// <summary>
    /// Hit stop cua Postknight: dung toan bo game vai tram giay khi don danh trung
    /// de tao cam giac "trong tay". Dung timeScale thay vi chi log.
    /// </summary>
    public class HitStopImpactSystem : MonoBehaviour
    {
        private static HitStopImpactSystem _instance;

        public static HitStopImpactSystem Instance
        {
            get
            {
                // Tu tao giong ImpactFeedbackSystem / CombatDamageResolver: scene khong
                // can setup gi, lan danh dau tien cung co hit stop.
                if (_instance == null)
                {
                    var obj = new GameObject("_HitStopImpactSystem");
                    _instance = obj.AddComponent<HitStopImpactSystem>();
                }
                return _instance;
            }
        }

        [Header("Impact Settings")]
        public float lightHitPause = 0.03f;
        public float heavyHitPause = 0.08f;

        private Coroutine routine;
        private float savedTimeScale = 1f;
        private float currentPause;

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else if (_instance != this)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // Domain reload / huy doi tuong giua chu dung thi tra lai timescale,
            // neu khong ca scene bi kem toc do vinh vien. Dung field thay vi property
            // (property se tu tao doi tuong moi khi cu bi huy).
            if (_instance == this && routine != null)
                Time.timeScale = savedTimeScale;
            if (_instance == this)
                _instance = null;
        }

        public void LightHit()
        {
            ApplyHitStop(lightHitPause);
        }

        public void HeavyHit()
        {
            ApplyHitStop(heavyHitPause);
        }

        private void ApplyHitStop(float duration)
        {
            // Man dang pause (timeScale = 0 va khong co hit stop nao chay):
            // khong dong vao de tranh "resume" nham khi nguoi choi dang mo pause.
            if (routine == null && Time.timeScale <= 0f)
                return;

            if (routine != null)
            {
                // Dang dung: chi keo dai them neu don moi nang hon don dang tinh.
                if (duration <= currentPause)
                    return;
                StopCoroutine(routine);
            }
            else
            {
                savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            currentPause = duration;
            routine = StartCoroutine(ResumeAfter(duration));
        }

        private IEnumerator ResumeAfter(float duration)
        {
            // Realtime: timeScale = 0 thi WaitForSeconds thong thuong khong chay duoc.
            yield return new WaitForSecondsRealtime(duration);

            routine = null;
            currentPause = 0f;
            // Ai do vua set timeScale trong slit giay dung (pause/ slowmo) thi khong de len.
            if (Mathf.Approximately(Time.timeScale, 0f))
                Time.timeScale = savedTimeScale;
        }
    }
}
