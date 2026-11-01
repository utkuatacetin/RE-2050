using UnityEditor;
using UnityEngine;

namespace ZamaninMektubu.EditorTools
{
    /// <summary>
    /// Bölüm 1 "Sokak" verisini oluşturur. Var olan dosyalara dokunmaz,
    /// böylece Inspector'da yaptığın değişiklikler (ör. görsel atamaları) korunur.
    /// Değerleri sıfırlamak istersen Data/Bolum1 klasörünü silip kurulumu tekrar çalıştır.
    /// </summary>
    public static class Level1Content
    {
        static readonly string[] Zemin = { "Zemin" };
        static readonly string[] Cati = { "Cati" };

        public static LevelData CreateOrLoad(string dir)
        {
            var agac = Card(dir, "agac", "Ağaç", 2, 4f, false, Zemin,
                "Bugün küçücük bir fidan. Ama zamanla büyür...",
                "O fidan artık dev bir çınar! Gölgesinde insanlar serinliyor.",
                "#3FA34D", new Vector2(0.35f, 0.7f), new Vector2(1.8f, 3.2f));

            var golgelik = Card(dir, "golgelik", "Gölgelik", 1, 2f, false, Zemin,
                "Ucuz ve hızlı bir gölge. Ama büyümez.",
                "Gölgelik hâlâ duruyor; biraz solmuş ama işe yarıyor.",
                "#E67E22", new Vector2(1.4f, 1.6f), new Vector2(1.4f, 1.6f));

            var cesme = Card(dir, "cesme", "Çeşme", 2, 2f, false, Zemin,
                "Serin su. Yanında gölge olursa çok daha iyi...",
                "Çeşmenin başında çocuklar su içiyor.",
                "#3498DB", new Vector2(0.6f, 0.9f), new Vector2(0.7f, 1.0f));

            var yesilCati = Card(dir, "yesil_cati", "Yeşil Çatı", 3, 1f, false, Cati,
                "Çatıyı bitkilerle kapla. Kuşlara yuva, yağmura sünger olur.",
                "Çatı yemyeşil, kuşlar geri gelmiş. Gündüz biraz serinletiyor ama gece biriktirdiği ısıyı geri veriyor. Asıl gücü yağmuru tutmak!",
                "#27AE60", new Vector2(3.0f, 0.15f), new Vector2(3.2f, 0.6f));

            var beyazCati = Card(dir, "beyaz_cati", "Beyaz Çatı", 1, 3f, false, Cati,
                "Açık renk boya güneşi geri yansıtır.",
                "Beyaz çatı güneşi geri yansıtıyor; hem bina hem sokak serinledi.",
                "#DDE7EE", new Vector2(3.2f, 0.12f), new Vector2(3.2f, 0.12f));

            var klima = Card(dir, "klima", "Klima", 1, -3f, true, Cati,
                "Evin içini hemen serinletir! Üstelik çok ucuz.",
                "Deniz: \"Herkes kendi klimasını aldı. Evler serin ama klimalar dışarıya sıcak hava üflüyor; sokak daha da ısındı, elektrik kesintileri başladı.\"",
                "#95A5A6", new Vector2(0.6f, 0.5f), new Vector2(2.6f, 0.9f));

            string path = dir + "/Bolum1_Sokak.asset";
            var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (level != null) return level;

            level = ScriptableObject.CreateInstance<LevelData>();
            level.levelTitle = "Bölüm 1: Sokak";
            level.introLetter =
                "Merhaba,\n\n" +
                "Adım Deniz. 2050 yılında, senin şu an yaşadığın mahallede yaşıyorum.\n\n" +
                "Bu yaz sıcaklık 48 dereceyi buldu. Sokakta tek bir ağaç yok; öğlen asfalt ayakkabılarımızın tabanını yumuşatıyor. " +
                "Geçen hafta otobüs durağında beklerken bir teyze sıcaktan bayıldı. Okul bahçesi öğleden sonra kapanıyor, çocuklar dışarı çıkamıyor.\n\n" +
                "Annem hep \"Keşke yıllar önce birileri bir şey yapsaydı\" der.\n\n" +
                "Belki o \"birileri\" sensindir.\n\n" +
                "Deniz";
            level.winLetter =
                "Sevgili geçmişteki dostum,\n\n" +
                "Bu mektubu, senin zamanında dikilen o ağacın gölgesinde yazıyorum. Hava yine sıcak ama sokakta insanlar var; " +
                "durakta kimse bayılmıyor, çocuklar yine bahçede oynuyor.\n\n" +
                "Bir fidan yirmi dört yılda bu kadar büyük bir fark yaratıyormuş. Teşekkür ederim.\n\n" +
                "Ama mahallemizin başka dertleri de var... Yakında yine yazacağım.\n\nDeniz";
            level.fact =
                "<b>Gerçek Bilgi:</b> ABD Çevre Koruma Ajansı'na (EPA) göre ağaç gölgesindeki yüzeyler, güneş alan yüzeylerden " +
                "11–25°C daha serin olabilir. Ağaçlar yapraklarından su buharlaştırarak havayı da serinletir. " +
                "Ama bir fidanın gölge veren bir ağaca dönüşmesi yıllar sürer; bu yüzden en iyi zaman bugün.";
            level.metricLabel = "Sokak Sıcaklığı";
            level.unit = "°C";
            level.todayValue = 40f;
            level.untreatedFutureValue = 48f;
            level.targetValue = 38f;
            level.goalText = "Sokağı 2050'de 38°C'ye kadar serinlet.";
            level.budget = 6;
            level.hand = new[] { agac, golgelik, cesme, yesilCati, beyazCati, klima };
            level.combos = new[]
            {
                new ComboRule
                {
                    comboName = "Yeşil Vaha",
                    a = agac, b = cesme, bonus = 2f,
                    note = "Ağacın gölgesindeki çeşme, mahallenin buluşma noktası oldu."
                }
            };
            level.nextSceneName = "";
            AssetDatabase.CreateAsset(level, path);
            return level;
        }

        static InterventionData Card(string dir, string id, string name, int cost, float impact, bool trap,
            string[] tags, string desc, string note, string color, Vector2 size26, Vector2 size50)
        {
            string path = dir + "/Kart_" + id + ".asset";
            var d = AssetDatabase.LoadAssetAtPath<InterventionData>(path);
            if (d != null) return d;

            d = ScriptableObject.CreateInstance<InterventionData>();
            d.id = id;
            d.displayName = name;
            d.cost = cost;
            d.impact = impact;
            d.isTrap = trap;
            d.allowedSlotTags = tags;
            d.description = desc;
            d.futureNote = note;
            d.placeholderColor = UIFactory.Hex(color);
            d.placeholderSize2026 = size26;
            d.placeholderSize2050 = size50;
            AssetDatabase.CreateAsset(d, path);
            return d;
        }
    }
}
