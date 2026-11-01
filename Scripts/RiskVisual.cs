using UnityEngine;

namespace ZamaninMektubu
{
    /// <summary>
    /// 2050'de, risk ne kadar yüksekse o kadar görünen katman (ör. sel suyu).
    /// Oyuncu hedefe ulaşınca tamamen kaybolur.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class RiskVisual : MonoBehaviour
    {
        [Range(0f, 1f)] public float maxAlpha = 1f;
        SpriteRenderer sr;

        /// <param name="era">0 = 2026, 1 = 2050</param>
        /// <param name="risk">0 = hedefte veya altında, 1 = hiçbir şey yapılmamış</param>
        public void Apply(float era, float risk)
        {
            if (!sr) sr = GetComponent<SpriteRenderer>();
            var c = sr.color;
            c.a = maxAlpha * Mathf.SmoothStep(0f, 1f, era) * Mathf.Clamp01(risk);
            sr.color = c;
        }
    }
}
