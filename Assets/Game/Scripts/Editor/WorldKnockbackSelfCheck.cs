using UnityEditor;
using UnityEngine;
using EternalClash.World;

namespace EternalClash.EditorTools
{
    /// <summary>
    /// Self-check for the WorldScroller knockback curve. Menu:
    /// Tools > EternalClash > Check World Knockback Curve
    /// </summary>
    public static class WorldKnockbackSelfCheck
    {
        [MenuItem("Tools/EternalClash/Check World Knockback Curve")]
        public static void Run()
        {
            const float peak = 2f;
            const float impact = 0.08f;
            const float recovery = 0.15f;
            float total = impact + recovery;

            Check(Mathf.Approximately(
                WorldScroller.EvaluateKnockbackMultiplier(0f, peak, impact, recovery), 0f), "starts at 0");
            Check(Mathf.Approximately(
                WorldScroller.EvaluateKnockbackMultiplier(impact, peak, impact, recovery), -peak), "reaches -peak");
            Check(Mathf.Approximately(
                WorldScroller.EvaluateKnockbackMultiplier(total, peak, impact, recovery), 0f), "returns to 0");
            Check(
                WorldScroller.EvaluateKnockbackMultiplier(total + 10f, peak, impact, recovery) == 0f,
                "stays 0 past the end");
            Check(
                WorldScroller.EvaluateKnockbackMultiplier(1f, 0f, impact, recovery) == 0f,
                "zero force stays 0");

            float maxSlope = 1.5f * peak / Mathf.Min(impact, recovery) + 1f;
            float previous = 0f;
            for (float t = 0f; t <= total + 0.001f; t += 0.001f)
            {
                float value = WorldScroller.EvaluateKnockbackMultiplier(t, peak, impact, recovery);
                Check(value <= 0.0001f && value >= -peak - 0.0001f, $"range at t={t:0.000}");
                Check(Mathf.Abs(value - previous) / 0.001f <= maxSlope, $"slope at t={t:0.000}");
                previous = value;
            }

            Debug.Log("[WorldKnockbackSelfCheck] Knockback curve OK");
        }

        private static void Check(bool condition, string label)
        {
            if (!condition)
                throw new System.InvalidOperationException("Knockback curve check failed: " + label);
        }
    }
}
