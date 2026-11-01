using UnityEngine;

namespace ZamaninMektubu
{
    [System.Serializable]
    public class ComboRule
    {
        public string comboName = "Kombo";
        [Tooltip("İki kart da sahnede varsa kombo açılır. A ve B aynıysa o karttan en az 2 tane gerekir.")]
        public InterventionData a;
        public InterventionData b;
        public float bonus = 2f;
        [TextArea(2, 4)] public string note;
    }

    /// <summary>
    /// Bir bölümün bütün verisi. Yeni bölüm = yeni LevelData + yeni sahne.
    /// </summary>
    [CreateAssetMenu(menuName = "Zamanın Mektubu/Bölüm", fileName = "Bolum_")]
    public class LevelData : ScriptableObject
    {
        [Header("Metinler")]
        public string levelTitle = "Bölüm";
        [TextArea(6, 16)] public string introLetter;
        [TextArea(4, 12)] public string winLetter;
        [TextArea(3, 8)] public string fact;

        [Header("Ölçüt (düşük olan iyidir)")]
        public string metricLabel = "Sokak Sıcaklığı";
        public string unit = "°C";
        [Tooltip("Birim sayının önüne mi yazılsın? (%45 gibi)")]
        public bool unitBefore;
        [Tooltip("Ölçüt kutusundaki ikon (boşsa termometre kalır)")]
        public Sprite metricIcon;
        [Tooltip("2050'de risk yüksekken sahneyi boyayan perde rengi")]
        public Color overlayColor = new Color(1f, 0.42f, 0f, 1f);
        [Tooltip("-1 ise OyunYoneticisi'ndeki değer kullanılır")]
        public float overlayMaxAlpha = -1f;
        [Tooltip("2026'da (bugün) gösterilen değer.")]
        public float todayValue = 40f;
        [Tooltip("Hiçbir şey yapılmazsa 2050'deki değer.")]
        public float untreatedFutureValue = 48f;
        [Tooltip("Bu değere veya altına inince bölüm kazanılır.")]
        public float targetValue = 38f;
        public string goalText = "Sokağı 2050'de 38°C'ye kadar serinlet.";

        [Header("Kaynaklar")]
        public int budget = 6;
        public InterventionData[] hand;
        public ComboRule[] combos;

        [Header("Akış")]
        [Tooltip("Boş bırakılırsa 'Sonraki Bölüm' butonu gizlenir.")]
        public string nextSceneName;
    }
}
