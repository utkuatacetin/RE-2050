using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZamaninMektubu.EditorTools
{
    /// <summary>
    /// "Zamanın Mektubu > Bölüm 3'ü Kur (Sahil)"
    /// Bölüm 2 sahnesini kopyalayıp Bölüm 3'e çevirir: veriler, görseller, yuvalar, iki ayrı 2050 (iyi/kötü).
    /// Bölüm 2'nin "Sonraki Bölüm" butonunu da Bölüm 3'e bağlar.
    /// </summary>
    public static class Level3Setup
    {
        const string Root = "Assets/ZamaninMektubu";
        const string ArtDir = Root + "/Art/L3";
        const string DataDir = Root + "/Data/Bolum3";
        const string L1Scene = Root + "/Scenes/Bolum2.unity";
        const string L2Scene = Root + "/Scenes/Bolum3.unity";
        const float BgPPU = 114.33f; // 1093 px yükseklik -> Bölüm 1 ile aynı dünya boyutu

        // id, 2026 ölçeği, 2050 ölçeği (PPU 100 sprite için)
        static readonly Dictionary<string, Vector2> Scales = new Dictionary<string, Vector2>
        {
            { "amber_isik",      new Vector2(0.3737f, 0.3737f) },
            { "projektor",       new Vector2(0.48f,   0.48f) },
            { "sifir_atik_bufe", new Vector2(0.2777f, 0.2777f) },
            { "plastik_bufe",    new Vector2(0.2903f, 0.2903f) },
            { "kumul",           new Vector2(0.2906f, 0.2906f) },
            { "beton_duvar",     new Vector2(0.2891f, 0.2891f) },
            { "deniz_cayiri",    new Vector2(0.3305f, 0.3305f) },
            { "yosun_temizle",   new Vector2(0.3301f, 0.3301f) },
        };

        [MenuItem("Zamanın Mektubu/Bölüm 3'ü Kur (Sahil)")]
        public static void Run() => RunInternal(true);

        public static bool RunInternal(bool showDialog)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(L1Scene) == null)
            {
                EditorUtility.DisplayDialog("Önce Bölüm 2", "Önce Bölüm 2'yi kur.", "Tamam");
                return false;
            }
            if (!AssetDatabase.IsValidFolder(ArtDir))
            {
                EditorUtility.DisplayDialog("Görseller yok", "Assets/ZamaninMektubu/Art/L3 klasörü bulunamadı.", "Tamam");
                return false;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

            // 1) Görseller
            Import("L3_bg_2026", BgPPU, new Vector2(0.5f, 0.5f));
            Import("L3_bg_2050", BgPPU, new Vector2(0.5f, 0.5f));
            Import("L3_bg_2050_iyi", BgPPU, new Vector2(0.5f, 0.5f));
            Import("UI_kaplumbaga", 100f, new Vector2(0.5f, 0.5f));
            foreach (var id in Scales.Keys)
            {
                Import("L3_" + id + "_2026", 100f, new Vector2(0.5f, 0f));
                Import("L3_" + id + "_2050", 100f, new Vector2(0.5f, 0f));
                Import("Icon_" + id, 100f, new Vector2(0.5f, 0.5f));
            }

            // 2) Veri
            var level = BuildData();

            // Bölüm 2 -> Bölüm 3 bağlantısı (Bölüm 2 sahnesi de güncellenir, çünkü kopyalanan sahne aynı veriyi kullanır)
            var l2 = AssetDatabase.LoadAssetAtPath<LevelData>(Root + "/Data/Bolum2/Bolum2_Dere.asset");
            if (l2) { l2.nextSceneName = "Bolum3"; EditorUtility.SetDirty(l2); }
            AssetDatabase.SaveAssets();

            // 3) Sahne: Bölüm 2'nin kopyası
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(L2Scene) != null) AssetDatabase.DeleteAsset(L2Scene);
            AssetDatabase.CopyAsset(L1Scene, L2Scene);
            var scene = EditorSceneManager.OpenScene(L2Scene);

            var gm = Object.FindFirstObjectByType<GameManager>();
            gm.level = level;
            gm.heatOverlayMaxAlpha = 0.12f;
            EditorUtility.SetDirty(gm);

            // Yer tutucu dünyayı kapat
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name.StartsWith("YerTutucu")) t.gameObject.SetActive(false);

            // Bölüm 2'den gelen sel suyunu kaldır
            foreach (var rv in Object.FindObjectsByType<RiskVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(rv.gameObject);

            // Arka planlar: 2026 + iyi 2050 (alttaki), kötü 2050 risk katmanı (üstteki)
            Transform world = null;
            foreach (var ev in Object.FindObjectsByType<EraVisual>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!ev.gameObject.name.StartsWith("Arkaplan_")) continue;
                var sr = ev.GetComponent<SpriteRenderer>();
                sr.sprite = Load(ev.gameObject.name.StartsWith("Arkaplan_2026") ? "L3_bg_2026" : "L3_bg_2050_iyi");
                ev.transform.localPosition = new Vector3(0f, 0.19f, 0f);
                ev.transform.localScale = Vector3.one;
                world = ev.transform.parent;
            }
            var bad = new GameObject("KotuGelecek_2050");
            bad.transform.SetParent(world, false);
            bad.transform.localPosition = new Vector3(0f, 0.19f, 0f);
            var bsr = bad.AddComponent<SpriteRenderer>();
            bsr.sprite = Load("L3_bg_2050");
            bsr.sortingOrder = 10;
            bsr.color = new Color(1f, 1f, 1f, 0f);
            bad.AddComponent<RiskVisual>().maxAlpha = 1f;

            // 4) Yuvalar (Bölüm 2'nin yuvaları yeniden adlandırılır)
            var slots = Object.FindObjectsByType<Slot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var s in slots)
            {
                string n = s.gameObject.name;
                if (n.EndsWith("Yamac"))
                    SetupSlot(s, "Yuva_Isik", "Isik", "Otel Önü Işığı", new Vector2(-6.33f, -2.07f),
                        new Vector2(2.4f, 3.2f), new Vector2(0f, 1.5f), new Vector2(2.2f, 0.45f), 0.6f);
                else if (n.EndsWith("Teras"))
                    SetupSlot(s, "Yuva_Bufe", "Bufe", "Plaj Büfesi", new Vector2(2.33f, -0.40f),
                        new Vector2(2.6f, 2.6f), new Vector2(0f, 1.2f), new Vector2(2.4f, 0.45f), 0.55f);
                else if (n.EndsWith("Dere"))
                    SetupSlot(s, "Yuva_Kumsal", "Kumsal", "Kumsalın Arkası", new Vector2(-2.04f, -2.46f),
                        new Vector2(5.4f, 2.0f), new Vector2(0f, 0.9f), new Vector2(4.6f, 0.5f), 0.7f);
                else if (n.EndsWith("Arsa"))
                    SetupSlot(s, "Yuva_Su", "Su", "Su Kenarı", new Vector2(7.67f, -2.51f),
                        new Vector2(6.4f, 2.0f), new Vector2(0f, 0.9f), new Vector2(5.6f, 0.5f), 0.75f);
            }

            // 5) Ölçüt ikonu (kaplumbağa)
            var ui = Object.FindFirstObjectByType<GameUI>();
            if (ui && ui.meterIcon) { ui.meterIcon.sprite = Load("UI_kaplumbaga"); ui.meterIcon.preserveAspect = true; EditorUtility.SetDirty(ui.meterIcon); }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddSceneToBuild(L2Scene);
            AssetDatabase.SaveAssets();

            if (showDialog)
                EditorUtility.DisplayDialog("Bölüm 3 hazır!",
                    "Bolum3 sahnesi kuruldu ve Bölüm 2'nin 'Sonraki Bölüm' butonuna bağlandı.\n\nPlay'e basıp deneyebilirsin.", "Süper");
            return true;
        }

        // ------------------------------------------------------------------
        static LevelData BuildData()
        {
            System.IO.Directory.CreateDirectory(DataDir);
            AssetDatabase.Refresh();
            string[] isik = { "Isik" }, bufe = { "Bufe" }, kumsal = { "Kumsal" }, su = { "Su" };

            var amber = Card("amber_isik", "Kaplumbağa Dostu Işık", 1, 12f, false, isik,
                "Alçak, turuncu ve yalnızca yere bakan ışık. Gece kumsal karanlık kalır.",
                "Yavrular ışığa değil, ayın denizdeki parıltısına yöneldi. Hepsi denize ulaştı!");
            var proj = Card("projektor", "Parlak Projektör", -2, -12f, true, isik,
                "Otel kumsalı gece de aydınlatmak istiyor ve mahalleye 2 yaprak verecek. Turist çeker!",
                "Deniz: \"Yavrular yumurtadan çıkınca denize değil, projektörlere doğru yürüdü. Sabah otoparkta bulduk onları.\"");
            var sifir = Card("sifir_atik_bufe", "Sıfır Atık Büfe", 2, 13f, false, bufe,
                "Cam şişe, su doldurma musluğu ve geri dönüşüm kutuları.",
                "Büfede herkes kendi şişesini dolduruyor; kumsalda tek bir plastik şişe yok.");
            var plastik = Card("plastik_bufe", "Plastik Şişe Büfe", 1, -6f, true, bufe,
                "Bol bol plastik şişe, bardak ve pipet. Ucuz ve pratik!",
                "Rüzgâr poşetleri denize taşıdı. Deniz kaplumbağaları poşetleri denizanası sanıp yutuyor.");
            var kumul = Card("kumul", "Kumul ve Bitkiler", 2, 17f, false, kumsal,
                "Kumsalın arkasında doğal kumulları ve kum zambaklarını koru.",
                "Kumul büyüdü, kum zambakları açtı. Kaplumbağalar burada yumurtladı; yavrular kumdan çıkıyor!");
            var duvar = Card("beton_duvar", "Beton Deniz Duvarı", 1, -10f, true, kumsal,
                "Kumsalın arkasına sağlam bir beton duvar ör. Denize karşı güvenli!",
                "Dalgalar duvara çarpıp kumu alıp götürdü. Kumsal kalmadı; yavru bir kaplumbağa duvarın dibinde sıkıştı.");
            var cayir = Card("deniz_cayiri", "Deniz Çayırını Koru", 2, 15f, false, su,
                "Kıyıya vuran kuru deniz çayırı yapraklarını yerinde bırak, sudaki çayırı koru.",
                "Kuru yapraklar kumu dalgalara karşı korudu; sudaki çayırda balıklar ve bir kaplumbağa dolaşıyor.");
            var yosun = Card("yosun_temizle", "Yosunları Temizle", 1, -6f, true, su,
                "Kıyıdaki kahverengi \"yosunları\" kepçeyle kaldır. Plaj bembeyaz görünsün!",
                "Koruyucu yapraklar gidince dalgalar kumu sildi; geriye kayalar ve paslı bir kepçe kaldı.");

            string path = DataDir + "/Bolum3_Sahil.asset";
            var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            bool isNew = level == null;
            if (isNew) level = ScriptableObject.CreateInstance<LevelData>();

            level.levelTitle = "Bölüm 3: Sahil";
            level.introLetter =
                "Merhaba, son mektubum bu.\n\n" +
                "Büyükannem çocukken bu kumsalda her yaz Caretta yavrularının denize koşmasını izlermiş. " +
                "Ben hiç görmedim. Geceleri otelin projektörleri kumsalı gündüz gibi aydınlatıyor; " +
                "kumsalın arkasına beton duvar örüldü, deniz yükseldikçe kum kayboldu. Sahil plastik şişelerle dolu.\n\n" +
                "Geçen yıl kumsalda tek bir yuva bile bulunamadı.\n\n" +
                "Ne olur, kaplumbağalar için bir şey yap.\n\nDeniz";
            level.winLetter =
                "Sevgili geçmişteki dostum,\n\n" +
                "Dün gece büyükannemle kumsala indik. Ay denizin üstündeydi, ışıklar alçak ve turuncuydu. " +
                "Kumulun dibinden önce bir, sonra yüzlerce minik kaplumbağa çıktı ve hepsi denize koştu.\n\n" +
                "Büyükannem ağladı. \"Tıpkı benim çocukluğumdaki gibi\" dedi.\n\n" +
                "Sokağımız serin, derelerimiz açık, kumsalımız canlı. Bütün bunlar senin bugün verdiğin kararlar sayesinde.\n\n" +
                "2050'den sevgiyle,\nDeniz";
            level.fact =
                "<b>Gerçek Bilgi:</b> Yumurtadan çıkan Caretta yavruları en parlak ufka, yani normalde denize yönelir; " +
                "kıyıdaki yapay ışıklar onları karaya doğru şaşırtır. Bu yüzden yuvalama kumsallarında alçak, turuncu " +
                "ve aşağı bakan ışıklar önerilir. Akdeniz'in Posidonia deniz çayırı ise karbon depolar ve kıyıya vuran " +
                "yaprakları kumsalı dalga aşınmasına karşı korur.";
            level.metricLabel = "Caretta Tehlikesi";
            level.unit = "%";
            level.unitBefore = true;
            level.metricIcon = Load("UI_kaplumbaga");
            level.overlayColor = new Color(1f, 0.45f, 0.1f, 1f);
            level.overlayMaxAlpha = 0.12f;
            level.todayValue = 60f;
            level.untreatedFutureValue = 90f;
            level.targetValue = 45f;
            level.goalText = "Caretta tehlikesini 2050'de %45'e indir.";
            level.budget = 6;
            level.hand = new[] { amber, proj, sifir, plastik, kumul, duvar, cayir, yosun };
            level.combos = new[]
            {
                new ComboRule
                {
                    comboName = "Caretta Yolu", a = kumul, b = amber, bonus = 10f,
                    note = "Kumulda yumurtadan çıkan yavrular, karanlık kumsalda ayın ışığını takip edip denize ulaştı."
                }
            };
            level.nextSceneName = "Final";
            if (isNew) AssetDatabase.CreateAsset(level, path);
            else EditorUtility.SetDirty(level);
            return level;
        }

        static InterventionData Card(string id, string name, int cost, float impact, bool trap, string[] tags, string desc, string note)
        {
            string path = DataDir + "/Kart_" + id + ".asset";
            var d = AssetDatabase.LoadAssetAtPath<InterventionData>(path);
            bool isNew = d == null;
            if (isNew) d = ScriptableObject.CreateInstance<InterventionData>();
            d.id = id; d.displayName = name; d.cost = cost; d.impact = impact; d.isTrap = trap;
            d.allowedSlotTags = tags; d.description = desc; d.futureNote = note;
            d.sprite2026 = Load("L3_" + id + "_2026");
            d.sprite2050 = Load("L3_" + id + "_2050");
            d.icon = Load("Icon_" + id);
            var sc = Scales[id];
            d.spriteScale2026 = sc.x; d.spriteScale2050 = sc.y;
            d.growsOverTime = false;
            d.placeholderColor = new Color(0.4f, 0.6f, 0.4f);
            if (isNew) AssetDatabase.CreateAsset(d, path);
            else EditorUtility.SetDirty(d);
            return d;
        }

        static void SetupSlot(Slot s, string objName, string tag, string area, Vector2 pos,
            Vector2 colSize, Vector2 colOffset, Vector2 markerScale, float labelY)
        {
            s.gameObject.name = objName;
            s.slotTag = tag;
            s.areaName = area;
            s.transform.localPosition = pos;
            var col = s.GetComponent<BoxCollider2D>();
            col.size = colSize; col.offset = colOffset;
            if (s.marker) s.marker.transform.localScale = new Vector3(markerScale.x, markerScale.y, 1f);
            if (s.areaLabel)
            {
                s.areaLabel.text = "+ " + area;
                s.areaLabel.transform.localPosition = new Vector3(0f, labelY, 0f);
                EditorUtility.SetDirty(s.areaLabel);
            }
            EditorUtility.SetDirty(s);
        }

        static void Import(string name, float ppu, Vector2 pivot)
        {
            string path = ArtDir + "/" + name + ".png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogWarning("Görsel bulunamadı: " + path); return; }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = ppu;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.maxTextureSize = 4096;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.Custom;
            st.spritePivot = pivot;
            st.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(st);
            imp.SaveAndReimport();
        }

        static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/" + name + ".png");

        static void AddSceneToBuild(string path)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(x => x.path == path)) list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
