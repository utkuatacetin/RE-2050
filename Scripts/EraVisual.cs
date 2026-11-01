using UnityEngine;

namespace ZamaninMektubu
{
    /// <summary>
    /// Zaman kaydırıcısına göre rengi / görünürlüğü / boyutu değişen sahne objesi.
    /// Tint: 2026 rengi ile 2050 rengi arasında geçiş yapar.
    /// Only2026 / Only2050: sadece o dönemde görünür (arka plan görselleri için).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class EraVisual : MonoBehaviour
    {
        public enum Mode { Tint, Only2026, Only2050 }

        public Mode mode = Mode.Tint;
        public Color color2026 = Color.white;
        public Color color2050 = Color.white;

        [Header("Boyut değişimi (opsiyonel)")]
        public bool scaleWithEra;
        public Vector3 scale2026 = Vector3.one;
        public Vector3 scale2050 = Vector3.one;

        SpriteRenderer sr;

        public void Apply(float t)
        {
            if (!sr) sr = GetComponent<SpriteRenderer>();
            Color c;
            switch (mode)
            {
                case Mode.Only2026:
                    c = color2026; c.a *= 1f - t; break;
                case Mode.Only2050:
                    c = color2050; c.a *= t; break;
                default:
                    c = Color.Lerp(color2026, color2050, t); break;
            }
            sr.color = c;
            if (scaleWithEra) transform.localScale = Vector3.Lerp(scale2026, scale2050, t);
        }
    }
}
