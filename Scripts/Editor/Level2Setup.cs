using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZamaninMektubu.EditorTools
{
    /// <summary>
    /// "Zamanın Mektubu > Bölüm 2'yi Kur (Dere)"
    /// Bölüm 1 sahnesini kopyalayıp Bölüm 2'ye çevirir: veriler, görseller, yuvalar, sel suyu katmanı.
    /// Bölüm 1'in "Sonraki Bölüm" butonunu da Bölüm 2'ye bağlar.
    /// </summary>
    public static class Level2Setup
    {
        const string Root = "Assets/ZamaninMektubu";
        const string ArtDir = Root + "/Art/L2";
        const string DataDir = Root + "/Data/Bolum2";
        const string L1Scene = Root + "/Scenes/Bolum1.unity";
        const string L2Scene = Root + "/Scenes/Bolum2.unity";
        const float BgPPU = 114.33f; // 1093 px yükseklik -> Bölüm 1 ile aynı dünya boyutu

        // id, 2026 ölçeği, 2050 ölçeği (PPU 100 sprite için)
        static readonly Dictionary<string, Vector2> Scales = new Dictionary<string, Vector2>
        {
            { "agaclandirma", new Vector2(0.4202f, 0.4202f) },
            { "site",         new Vector2(0.4202f, 0.4202f) },
            { "yesil_cati",   new Vector2(0.2088f, 0.2088f) },
            { "dere_ac",      new Vector2(0.2473f, 0.2448f) },
            { "beton_kanal",  new Vector2(0.2525f, 0.2526f) },
            { "gecirgen",     new Vector2(0.2927f, 0.2928f) },
            { "asfalt",       new Vector2(0.2925f, 0.2926f) },
        };

        [MenuItem("Zamanın Mektubu/Bölüm 2'yi Kur (Dere)")]
        public static void Run() => RunInternal(true);

        public static bool RunInternal(bool showDialog)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(L1Scene) == null)
            {
                EditorUtility.DisplayDialog("Önce Bölüm 1", "Önce 'Bölüm 1'i Baştan Kur (Her Şey)' komutunu çalıştır.", "Tamam");
                return false;
            }
            if (!AssetDatabase.IsValidFolder(ArtDir))
            {
                EditorUtility.DisplayDialog("Görseller yok", "Assets/ZamaninMektubu/Art/L2 klasörü bulunamadı.", "Tamam");
                return false;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

            // 1) Görseller
            Import("L2_bg_2026", BgPPU, new Vector2(0.5f, 0.5f));
            Import("L2_bg_2050", BgPPU, new Vector2(0.5f, 0.5f));
            Import("L2_sel", BgPPU, new Vector2(0.5f, 0.5f));
            Import("UI_damla", 100f, new Vector2(0.5f, 0.5f));
            foreach (var id in Scales.Keys)
            {
                Import("L2_" + id + "_2026", 100f, new Vector2(0.5f, 0f));
                Import("L2_" + id + "_2050", 100f, new Vector2(0.5f, 0f));
                Import("Icon_" + id, 100f, new Vector2(0.5f, 0.5f));
            }

            // 2) Veri
            var level = BuildData();

            // Bölüm 1 -> Bölüm 2 bağlantısı
            var l1 = AssetDatabase.LoadAssetAtPath<LevelData>(Root + "/Data/Bolum1/Bolum1_Sokak.asset");
            if (l1) { l1.nextSceneName = "Bolum2"; EditorUtility.SetDirty(l1); }
            AssetDatabase.SaveAssets();

            // 3) Sahne: Bölüm 1'in kopyası
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(L2Scene) != null) AssetDatabase.DeleteAsset(L2Scene);
            AssetDatabase.CopyAsset(L1Scene, L2Scene);
            var scene = EditorSceneManager.OpenScene(L2Scene);

            var gm = Object.FindFirstObjectByType<GameManager>();
            gm.level = level;
            gm.heatOverlayMaxAlpha = 0.18f;
            EditorUtility.SetDirty(gm);

            // Yer tutucu dünyayı kapat
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name.StartsWith("YerTutucu")) t.gameObject.SetActive(false);

            // Arka planlar
            Transform world = null;
            foreach (var ev in Object.FindObjectsByType<EraVisual>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!ev.gameObject.name.StartsWith("Arkaplan_")) continue;
                var sr = ev.GetComponent<SpriteRenderer>();
                sr.sprite = Load(ev.gameObject.name.StartsWith("Arkaplan_2026") ? "L2_bg_2026" : "L2_bg_2050");
                ev.transform.localPosition = new Vector3(0f, 0.19f, 0f);
                ev.transform.localScale = Vector3.one;
                world = ev.transform.parent;
            }

            // Sel suyu katmanı (risk düştükçe çekilir)
            var water = new GameObject("SelSuyu");
            water.transform.SetParent(world, false);
            water.transform.localPosition = new Vector3(0f, 0.19f, 0f);
            var wsr = water.AddComponent<SpriteRenderer>();
            wsr.sprite = Load("L2_sel");
            wsr.sortingOrder = 10;
            wsr.color = new Color(1f, 1f, 1f, 0f);
            water.AddComponent<RiskVisual>().maxAlpha = 1f;

            // 4) Yuvalar: Bölüm 1'in dört yuvasını yeniden kullan
            var slots = Object.FindObjectsByType<Slot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var s in slots)
            {
                string n = s.gameObject.name;
                if (n.EndsWith("Durak"))
                    SetupSlot(s, "Yuva_Yamac", "Yamac", "Yanmış Yamaç", new Vector2(-7.33f, -2.29f),
                        new Vector2(7.6f, 3.8f), new Vector2(0f, 1.9f), new Vector2(5.5f, 0.5f), 2.2f);
                else if (n.EndsWith("Cati"))
                    SetupSlot(s, "Yuva_Teras", "Teras", "Teras", new Vector2(-3.44f, 1.66f),
                        new Vector2(4.8f, 1.3f), new Vector2(0f, 0.45f), new Vector2(4.2f, 0.45f), 0.55f);
                else if (n.EndsWith("Otopark"))
                    SetupSlot(s, "Yuva_Dere", "Dere", "Betona Gömülü Dere", new Vector2(-0.16f, -2.33f),
                        new Vector2(5.4f, 2.2f), new Vector2(0f, 1.0f), new Vector2(4.6f, 0.5f), 0.9f);
                else if (n.EndsWith("Okul"))
                    SetupSlot(s, "Yuva_Arsa", "Arsa", "Boş Arsa", new Vector2(6.18f, -2.33f),
                        new Vector2(6.6f, 1.5f), new Vector2(0f, 0.65f), new Vector2(5.8f, 0.5f), 0.75f);
            }

            // 5) Ölçüt ikonu (damla)
            var ui = Object.FindFirstObjectByType<GameUI>();
            if (ui && ui.meterIcon) { ui.meterIcon.sprite = Load("UI_damla"); ui.meterIcon.preserveAspect = true; EditorUtility.SetDirty(ui.meterIcon); }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddSceneToBuild(L2Scene);
            AssetDatabase.SaveAssets();

            if (showDialog)
                EditorUtility.DisplayDialog("Bölüm 2 hazır!",
                    "Bolum2 sahnesi kuruldu ve Bölüm 1'in 'Sonraki Bölüm' butonuna bağlandı.\n\nPlay'e basıp deneyebilirsin.", "Süper");
            return true;
        }

        // ------------------------------------------------------------------
        static LevelData BuildData()
        {
            System.IO.Directory.CreateDirectory(DataDir);
            AssetDatabase.Refresh();
            string[] yamac = { "Yamac" }, teras = { "Teras" }, dere = { "Dere" }, arsa = { "Arsa" };

            var agac = Card("agaclandirma", "Ağaçlandırma", 2, 15f, false, yamac,
                "Yanan yamaca fidan dik. Kökler toprağı tutar, yağmuru emer.",
                "Yamaç yeniden orman oldu! Kökler toprağı tutuyor, yağmur suyu yavaşça toprağa sızıyor.");
            var site = Card("site", "Yamaca Site", -3, -20f, true, yamac,
                "Bir şirket yanan yamaca villa yapmak istiyor ve mahalleye 3 yaprak bağışlayacak!",
                "Deniz: \"Yamaç betonla kaplandı. Yağmur suyu toprağa sızamıyor; çamurla birlikte doğrudan sokağımıza akıyor.\"");
            var cati = Card("yesil_cati", "Yeşil Teras", 1, 7f, false, teras,
                "Terası toprak ve bitkilerle kapla. Yağmuru sünger gibi tutar.",
                "Teras yağmuru tuttu; fazla su fıçıda birikiyor. Bölüm 1'de serinletmede zayıftı, ama selde işe yarıyor!");
            var dereAc = Card("dere_ac", "Dereyi Aç", 3, 18f, false, dere,
                "Betonun altındaki dereyi gün yüzüne çıkar; geniş yeşil kıyılar suya yer açar.",
                "Dere yeniden nefes alıyor: söğütler, sazlıklar, ördekler... Taşkın suyu kıyılara yayılıp yavaşlıyor.");
            var kanal = Card("beton_kanal", "Beton Kanal", 1, 3f, true, dere,
                "Dereyi daha geniş bir beton kanala al. Hızlı ve ucuz!",
                "Kanal dallar ve plastik şişelerle tıkandı; hızlanan su kenarlardan taştı.");
            var gecirgen = Card("gecirgen", "Geçirgen Zemin", 2, 12f, false, arsa,
                "Taşların arasından çimen biter; yağmur toprağa sızar.",
                "Yağmur yağınca su birikmiyor, toprağa sızıyor. Çocuklar zıplayacak su birikintisi bile bulamıyor!");
            var asfalt = Card("asfalt", "Otopark Asfaltı", 1, -6f, true, arsa,
                "Boş arsayı asfaltla, otopark yap. Düzenli ve kullanışlı!",
                "Asfalt suyu hiç emmiyor; yağmur suyu bir anda sokağa doldu, şişeler yüzüyor.");

            string path = DataDir + "/Bolum2_Dere.asset";
            var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            bool isNew = level == null;
            if (isNew) level = ScriptableObject.CreateInstance<LevelData>();

            level.levelTitle = "Bölüm 2: Dere";
            level.introLetter =
                "Merhaba, yine ben, Deniz.\n\n" +
                "Sokağımız artık serin, teşekkürler! Ama bu sefer derdimiz su.\n\n" +
                "Geçen hafta bir saatlik sağanak mahalleyi göle çevirdi. Yamaçtaki orman yıllar önce yanmış; çıplak topraktan çamur aktı. " +
                "Eskiden burada bir dere varmış, ama betonun altına gömülmüş. Su gidecek yer bulamayınca sokağa taştı. " +
                "Bakkal Hasan Amca dükkânını kum torbalarıyla korumaya çalıştı.\n\n" +
                "Yağmuru durduramayız. Ama belki ona gidecek bir yer açabilirsin?\n\nDeniz";
            level.winLetter =
                "Sevgili geçmişteki dostum,\n\n" +
                "Dün yine sağanak yağdı. Ama bu sefer sokak su altında kalmadı! Yağmur yamaçtaki ağaçların köklerine, " +
                "dere kıyısındaki sazlıklara ve taşların arasındaki toprağa sızdı.\n\n" +
                "Hasan Amca kum torbalarını kaldırdı. Dere kenarında artık ördekleri besliyoruz.\n\n" +
                "Ama sıradaki mektubum denizden gelecek... Orada da bize ihtiyaç var.\n\nDeniz";
            level.fact =
                "<b>Gerçek Bilgi:</b> Orman yangınından sonra çıplak kalan yamaçlarda yağmur suyu toprağa sızamaz ve hızla akar; " +
                "bu yüzden yangın sonrası sel ve heyelan riski artar. Güney Kore'nin başkenti Seul, betonun altına gömülü Cheonggyecheon " +
                "deresini 2005'te yeniden açtı; dere bugün hem taşkın suyuna yer açıyor hem de şehrin en sevilen parklarından biri.";
            level.metricLabel = "Sel Riski";
            level.unit = "%";
            level.unitBefore = true;
            level.metricIcon = Load("UI_damla");
            level.overlayColor = new Color(0.12f, 0.17f, 0.28f, 1f);
            level.overlayMaxAlpha = 0.18f;
            level.todayValue = 50f;
            level.untreatedFutureValue = 85f;
            level.targetValue = 45f;
            level.goalText = "Sel riskini 2050'de %45'e indir.";
            level.budget = 7;
            level.hand = new[] { agac, site, cati, dereAc, kanal, gecirgen, asfalt };
            level.combos = new[]
            {
                new ComboRule
                {
                    comboName = "Sünger Mahalle", a = dereAc, b = gecirgen, bonus = 8f,
                    note = "Açılan dere ve geçirgen zemin birlikte çalıştı: mahalle yağmuru bir sünger gibi emiyor."
                }
            };
            level.nextSceneName = AssetDatabase.LoadAssetAtPath<SceneAsset>(Root + "/Scenes/Bolum3.unity") != null ? "Bolum3" : "";
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
            d.sprite2026 = Load("L2_" + id + "_2026");
            d.sprite2050 = Load("L2_" + id + "_2050");
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
