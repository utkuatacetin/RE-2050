using UnityEngine;

namespace ZamaninMektubu
{
    /// <summary>
    /// Bir müdahale kartı: ağaç, gölgelik, klima...
    /// Proje penceresinde sağ tık > Create > Zamanın Mektubu > Müdahale Kartı ile yenisi oluşturulur.
    /// </summary>
    [CreateAssetMenu(menuName = "Zamanın Mektubu/Müdahale Kartı", fileName = "Kart_")]
    public class InterventionData : ScriptableObject
    {
        [Header("Kimlik")]
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;

        [Header("Oyun Değerleri")]
        [Min(0)] public int cost = 1;
        [Tooltip("2050'deki ölçüte etkisi. Pozitif = iyileştirir (ör. serinletir), negatif = kötüleştirir.")]
        public float impact = 2f;
        [Tooltip("Tuzak kart: ucuz/cazip ama geleceği kötüleştirir.")]
        public bool isTrap;
        [Tooltip("Bu kartın konabileceği yuva türleri (ör. Zemin, Cati).")]
        public string[] allowedSlotTags = { "Zemin" };

        [Header("2050'ye bakınca çıkan not")]
        [TextArea(2, 4)] public string futureNote;

        [Header("Görseller (boşsa renkli kutu gösterilir)")]
        public Sprite icon;
        public Sprite sprite2026;
        public Sprite sprite2050;
        [Tooltip("Gerçek görsel kullanılırken 2026 ve 2050'deki ölçek.")]
        public float spriteScale2026 = 0.35f;
        public float spriteScale2050 = 1f;
        [Tooltip("Açıksa kaydırırken 2026 hali büyüyerek 2050 haline dönüşür (ağaçlar için). Kapalıysa sadece geçiş yapar (çim, panel...).")]
        public bool growsOverTime = true;

        [Header("Yer tutucu kutu (görsel yokken)")]
        public Color placeholderColor = Color.green;
        public Vector2 placeholderSize2026 = new Vector2(0.4f, 0.6f);
        public Vector2 placeholderSize2050 = new Vector2(1.6f, 2.4f);

        public bool CanGoTo(string slotTag)
        {
            if (allowedSlotTags == null) return false;
            foreach (var t in allowedSlotTags)
                if (t == slotTag) return true;
            return false;
        }
    }
}
