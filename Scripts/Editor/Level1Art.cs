using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ZamaninMektubu.EditorTools
{
    /// <summary>
    /// "Zamanın Mektubu > Bölüm 1 Görsellerini Kur"
    /// Art/L1 klasöründeki hazır görselleri doğru ayarlarla içe aktarır,
    /// kartlara ve arka planlara atar, yuvaları ve kamerayı ayarlar.
    /// Kart verisini (v2, 6 kart) de kendisi günceller; ayrıca başka komut çalıştırmaya gerek yok.
    /// </summary>
    public static class Level1Art
    {
        const string ArtDir = "Assets/ZamaninMektubu/Art/L1";
        const string DataDir = "Assets/ZamaninMektubu/Data/Bolum1";

        // id, 2026 ölçeği, 2050 ölçeği, büyüyerek mi dönüşür (PPU 100 için hesaplandı)
        static readonly (string id, float s26, float s50, bool grow)[] Cards =
        {
            ("agac",         0.102f,  0.2596f, true),
            ("palmiye",      0.0891f, 0.1731f, true),
            ("bahce",        0.1667f, 0.1669f, false),
            ("suni_cim",     0.163f,  0.163f,  false),
            ("gunes_paneli", 0.1863f, 0.1863f, false),
            ("klima",        0.0774f, 0.138f,  false),
        };

        [MenuItem("Zamanın Mektubu/Bölüm 1 Görsellerini Kur")]
        public static void Run() => RunInternal(true);

        public static void RunInternal(bool showDialog)
        {
            var slots = Object.FindObjectsByType<Slot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (slots.Length == 0)
            {
                EditorUtility.DisplayDialog("Sahne açık değil",
                    "Önce Assets/ZamaninMektubu/Scenes/Bolum1 sahnesini aç, sonra bu komutu tekrar çalıştır.", "Tamam");
                return;
            }
            if (!AssetDatabase.IsValidFolder(ArtDir))
            {
                EditorUtility.DisplayDialog("Görseller bulunamadı",
                    "Art/L1 klasörü yok. Zip'teki Assets/ZamaninMektubu/Art/L1 klasörünü projene kopyala.", "Tamam");
                return;
            }

            // 1) Kart verisi (6 kartlık set) ve yuva etiketleri
            Level1Update.ApplyData();
            Level1Update.ApplyScene(slots);

            // 2) Görselleri içe aktar
            Import("L1_bg_2026", 86f, new Vector2(0.5f, 0.5f));
            Import("L1_bg_2050", 86f, new Vector2(0.5f, 0.5f));
            int missing = 0;
            foreach (var c in Cards)
            {
                Import("L1_" + c.id + "_2026", 100f, new Vector2(0.5f, 0f));
                Import("L1_" + c.id + "_2050", 100f, new Vector2(0.5f, 0f));
                Import("Icon_" + c.id, 100f, new Vector2(0.5f, 0.5f));
            }
            AssetDatabase.Refresh();

            // 3) Kartlara ata
            foreach (var c in Cards)
            {
                var d = AssetDatabase.LoadAssetAtPath<InterventionData>(DataDir + "/Kart_" + c.id + ".asset");
                if (d == null) { missing++; continue; }
                d.sprite2026 = Load("L1_" + c.id + "_2026");
                d.sprite2050 = Load("L1_" + c.id + "_2050");
                d.icon = Load("Icon_" + c.id);
                d.spriteScale2026 = c.s26;
                d.spriteScale2050 = c.s50;
                d.growsOverTime = c.grow;
                EditorUtility.SetDirty(d);
            }

            // 4) Arka planlar
            foreach (var ev in Object.FindObjectsByType<EraVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string n = ev.gameObject.name;
                if (!n.StartsWith("Arkaplan_")) continue;
                var sr = ev.GetComponent<SpriteRenderer>();
                Undo.RecordObject(sr, "Arka plan");
                Undo.RecordObject(ev.transform, "Arka plan");
                sr.sprite = Load(n.StartsWith("Arkaplan_2026") ? "L1_bg_2026" : "L1_bg_2050");
                ev.transform.localPosition = new Vector3(0f, 0.19f, 0f);
                ev.transform.localScale = Vector3.one;
            }

            // 5) Yuva konumları (görsele göre ölçüldü)
            foreach (var s in slots)
            {
                string n = s.gameObject.name;
                Vector2 p;
                if (n.EndsWith("Cati")) p = new Vector2(-8f, 2.15f);
                else if (n.EndsWith("Durak")) p = new Vector2(-2f, -2.3f);
                else if (n.EndsWith("Otopark")) p = new Vector2(0.67f, -2.3f);
                else if (n.EndsWith("Okul")) p = new Vector2(7.6f, -2.3f);
                else continue;
                Undo.RecordObject(s.transform, "Yuva");
                s.transform.localPosition = p;
                if (n.EndsWith("Cati"))
                {
                    var col = s.GetComponent<BoxCollider2D>();
                    Undo.RecordObject(col, "Yuva");
                    col.size = new Vector2(5.2f, 1.6f);
                    col.offset = new Vector2(0f, 0.6f);
                    if (s.marker) s.marker.transform.localScale = new Vector3(4.6f, 0.5f, 1f);
                }
            }

            // 6) Sıcaklık perdesi (2050 görseli zaten turuncu) ve kamera
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm) { Undo.RecordObject(gm, "Ayar"); gm.heatOverlayMaxAlpha = 0.15f; EditorUtility.SetDirty(gm); }
            var cam = Camera.main;
            if (cam && !cam.GetComponent<CameraFit>()) Undo.AddComponent<CameraFit>(cam.gameObject);

            var scene = slots[0].gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            if (showDialog) EditorUtility.DisplayDialog("Bölüm 1 hazır!",
                missing == 0
                    ? "Bütün görseller yerleştirildi.\n\nGame penceresini 2400x1080 yapıp Play'e bas."
                    : missing + " kart dosyası bulunamadı. Console'a bak.",
                "Süper");
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
    }
}
