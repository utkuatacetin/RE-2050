using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZamaninMektubu.EditorTools
{
    /// <summary>
    /// Bölüm 1'i yeni 6 kartlık sete (3 ikilem) günceller.
    /// Açık sahneyi yeniden KURMAZ: arka planlar, yuva konumları ve ayarlar korunur.
    /// Kartlara atadığın görseller (icon / sprite2026 / sprite2050) korunur.
    /// </summary>
    public static class Level1Update
    {
        const string DataDir = "Assets/ZamaninMektubu/Data/Bolum1";

        [MenuItem("Zamanın Mektubu/Bölüm 1 Kartlarını Güncelle (v2)")]
        public static void Run()
        {
            var slots = Object.FindObjectsByType<Slot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (slots.Length == 0)
            {
                EditorUtility.DisplayDialog("Sahne açık değil",
                    "Önce Assets/ZamaninMektubu/Scenes/Bolum1 sahnesini aç, sonra bu komutu tekrar çalıştır.", "Tamam");
                return;
            }
            ApplyData();
            ApplyScene(slots);
            EditorSceneManager.MarkSceneDirty(slots[0].gameObject.scene);
            EditorSceneManager.SaveScene(slots[0].gameObject.scene);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Güncellendi",
                "Bölüm 1 artık 6 kartlık yeni sette.\n\nÇınar • Palmiye • Gerçek Bahçe • Suni Çim • Güneş Paneli • Klima\n\nPlay'e basıp deneyebilirsin.",
                "Süper");
        }

        // ------------------------------------------------------------------
        public static void ApplyData()
        {
            System.IO.Directory.CreateDirectory(DataDir);
            AssetDatabase.Refresh();

            string[] agacYuva = { "Durak", "Otopark" };
            string[] okul = { "Okul" };
            string[] cati = { "Cati" };

            var cinar = Card("agac", "Çınar", 2, 4f, false, agacYuva,
                "Yavaş büyür ama kocaman bir gölge verir.",
                "O fidan artık dev bir çınar! Gölgesinde insanlar serinliyor.",
                "#3FA34D", new Vector2(0.35f, 0.7f), new Vector2(1.8f, 3.2f));

            var palmiye = Card("palmiye", "Palmiye", 1, 1f, true, agacYuva,
                "Hızlı büyür, çok güzel görünür. Antalya'nın simgesi!",
                "Palmiye boy atmış ama gölgesi minicik. Altında kimse duramıyor, sokak hâlâ kavruluyor.",
                "#8BC34A", new Vector2(0.4f, 1.0f), new Vector2(1.0f, 3.4f));

            var bahce = Card("bahce", "Gerçek Bahçe", 2, 3f, false, okul,
                "Çim, toprak ve sebze tarhları. Biraz su ister ama serinletir.",
                "Bahçe yemyeşil! Toprak serin kalıyor, çocuklar domates topluyor; sebzeler okulun yemekhanesine gidiyor.",
                "#6DBE45", new Vector2(3.8f, 0.3f), new Vector2(3.8f, 1.1f));

            var suniCim = Card("suni_cim", "Suni Çim", 1, -2f, true, okul,
                "Hep yeşil, su istemez, bakım gerektirmez!",
                "Suni çim güneşte kızgın tava gibi oldu; çocuklar üstüne basamıyor. Üstelik plastik: zamanla parçalanıp çöpe ve denize karışıyor.",
                "#2ECC71", new Vector2(3.8f, 0.15f), new Vector2(3.8f, 0.15f));

            var panel = Card("gunes_paneli", "Güneş Paneli", 2, 2f, false, cati,
                "Çatıyı gölgeler, güneşten temiz elektrik üretir.",
                "Paneller çatıyı gölgeliyor ve mahallenin elektriğini güneşten üretiyor. Kesinti yok!",
                "#2C3E8F", new Vector2(3.0f, 0.4f), new Vector2(3.4f, 0.5f));

            var klima = Card("klima", "Klima", 1, -3f, true, cati,
                "Evin içini hemen serinletir! Üstelik çok ucuz.",
                "Deniz: \"Herkes kendi klimasını aldı. Evler serin ama klimalar dışarıya sıcak hava üflüyor; sokak daha da ısındı, elektrik kesintileri başladı.\"",
                "#95A5A6", new Vector2(0.6f, 0.5f), new Vector2(2.6f, 0.9f));

            string path = DataDir + "/Bolum1_Sokak.asset";
            var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            bool isNew = level == null;
            if (isNew) level = Level1Content.CreateOrLoad(DataDir);

            level.budget = 7;
            level.todayValue = 40f;
            level.untreatedFutureValue = 48f;
            level.targetValue = 38f;
            level.goalText = "Sokağı 2050'de 38°C'ye kadar serinlet.";
            level.hand = new[] { cinar, palmiye, bahce, suniCim, panel, klima };
            level.combos = new[]
            {
                new ComboRule
                {
                    comboName = "Yeşil Vaha",
                    a = cinar, b = bahce, bonus = 2f,
                    note = "Çınarın gölgesi ve serin bahçe birleşti; okulun önü mahallenin buluşma noktası oldu."
                }
            };
            level.fact =
                "<b>Gerçek Bilgi:</b> ABD Çevre Koruma Ajansı'na (EPA) göre ağaç gölgesindeki yüzeyler, güneş alan yüzeylerden " +
                "11–25°C daha serin olabilir. Palmiyeler ise çok az gölge verir; bu yüzden Miami Beach palmiyelerin oranını azaltıp " +
                "yerine gölge ağaçları dikmeyi planlıyor. Bir fidanın büyümesi yıllar sürer; en iyi zaman bugün.";
            EditorUtility.SetDirty(level);
        }

        static InterventionData Card(string id, string name, int cost, float impact, bool trap, string[] tags,
            string desc, string note, string color, Vector2 size26, Vector2 size50)
        {
            string path = DataDir + "/Kart_" + id + ".asset";
            var d = AssetDatabase.LoadAssetAtPath<InterventionData>(path);
            bool isNew = d == null;
            if (isNew) d = ScriptableObject.CreateInstance<InterventionData>();

            // Değerleri güncelle (atanmış görseller korunur)
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

            if (isNew) AssetDatabase.CreateAsset(d, path);
            else EditorUtility.SetDirty(d);
            return d;
        }

        // ------------------------------------------------------------------
        public static void ApplyScene(Slot[] slots)
        {
            foreach (var s in slots)
            {
                string n = s.gameObject.name;
                if (n.EndsWith("Cati")) Setup(s, "Cati", "Apartman Çatısı", null, null, null);
                else if (n.EndsWith("Durak")) Setup(s, "Durak", "Otobüs Durağı", null, null, null);
                else if (n.EndsWith("Otopark")) Setup(s, "Otopark", "Otopark", null, null, null);
                else if (n.EndsWith("Okul"))
                    Setup(s, "Okul", "Okul Önü Bahçesi", new Vector2(4.2f, 2f), new Vector2(0f, 0.9f), new Vector2(3.8f, 0.45f));
            }
        }

        static void Setup(Slot s, string tag, string area, Vector2? colSize, Vector2? colOffset, Vector2? markerScale)
        {
            Undo.RecordObject(s, "Yuva güncelle");
            s.slotTag = tag;
            s.areaName = area;
            if (s.areaLabel)
            {
                Undo.RecordObject(s.areaLabel, "Yuva güncelle");
                s.areaLabel.text = "+ " + area;
            }
            var col = s.GetComponent<BoxCollider2D>();
            if (col && colSize.HasValue)
            {
                Undo.RecordObject(col, "Yuva güncelle");
                col.size = colSize.Value;
                col.offset = colOffset ?? Vector2.zero;
            }
            if (s.marker && markerScale.HasValue)
            {
                Undo.RecordObject(s.marker.transform, "Yuva güncelle");
                s.marker.transform.localScale = new Vector3(markerScale.Value.x, markerScale.Value.y, 1f);
            }
            EditorUtility.SetDirty(s);
        }
    }
}
