using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static ZamaninMektubu.UIFactory;

namespace ZamaninMektubu.EditorTools
{
    /// <summary>
    /// Üst menüde "Zamanın Mektubu > Bölüm 1 Sahnesini Kur" komutu.
    /// Yer tutucu görsellerle oynanabilir bir sahne, arayüz ve bölüm verisi oluşturur.
    /// </summary>
    public static class SceneBuilder
    {
        const string Root = "Assets/ZamaninMektubu";
        const string GenDir = Root + "/Generated";
        const string DataDir = Root + "/Data/Bolum1";
        const string SceneDir = Root + "/Scenes";

        static Sprite sqC, sqB, circle, uiRounded, uiBackground, uiKnob;

        [MenuItem("Zamanın Mektubu/Bölüm 1 Sahnesini Kur")]
        public static void BuildLevel1() => Build(true);

        [MenuItem("Zamanın Mektubu/Bölüm 1'i Baştan Kur (Her Şey)", priority = 0)]
        public static void BuildEverything()
        {
            if (!Build(false)) return;
            Level1Art.RunInternal(false);
            EditorUtility.DisplayDialog("Bölüm 1 hazır!",
                "Sahne, arayüz, kartlar ve bütün görseller kuruldu.\n\nGame penceresini 2400x1080 yapıp Play'e bas.", "Süper");
        }

        public static bool Build(bool showDialog)
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                EditorUtility.DisplayDialog("TextMeshPro gerekli",
                    "Önce üst menüden Window > TextMeshPro > Import TMP Essential Resources komutunu çalıştır, sonra bu kurulumu tekrar başlat.",
                    "Tamam");
                return false;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

            Directory.CreateDirectory(GenDir);
            Directory.CreateDirectory(DataDir);
            Directory.CreateDirectory(SceneDir);
            AssetDatabase.Refresh();

            sqC = MakeSprite("Kare.png", 8, false, new Vector2(0.5f, 0.5f), 8);
            sqB = MakeSprite("KareAlt.png", 8, false, new Vector2(0.5f, 0f), 8);
            circle = MakeSprite("Daire.png", 128, true, new Vector2(0.5f, 0.5f), 128);
            uiRounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            uiBackground = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            uiKnob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var level = Level1Content.CreateOrLoad(DataDir);
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------- Kamera & olay sistemi ----------
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("#8FD0F2");
            camGo.transform.position = new Vector3(0, -0.35f, -10);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<Physics2DRaycaster>();
            CreateEventSystem();

            // ---------- Dünya ----------
            var world = new GameObject("Sahne").transform;
            var heat = BuildWorld(world);
            var slotsRoot = new GameObject("Yuvalar").transform;
            slotsRoot.SetParent(world, false);
            MakeSlot(slotsRoot, "Cati", "Cati", "Apartman Çatısı", new Vector2(-6f, 2.4f),
                new Vector2(3.6f, 1.4f), new Vector2(0, 0.5f), new Vector2(3.2f, 0.5f), -0.5f);
            MakeSlot(slotsRoot, "Durak", "Zemin", "Otobüs Durağı", new Vector2(-0.9f, -2.2f),
                new Vector2(1.9f, 3f), new Vector2(0, 1.3f), new Vector2(1.6f, 0.45f));
            MakeSlot(slotsRoot, "Otopark", "Zemin", "Otopark", new Vector2(3.1f, -2.2f),
                new Vector2(1.9f, 3f), new Vector2(0, 1.3f), new Vector2(1.6f, 0.45f));
            MakeSlot(slotsRoot, "Okul", "Zemin", "Okul Bahçesi", new Vector2(5.5f, -2.2f),
                new Vector2(1.9f, 3f), new Vector2(0, 1.3f), new Vector2(1.6f, 0.45f));

            // ---------- Ses ----------
            var musicGo = new GameObject("Muzik");
            var musicSrc = musicGo.AddComponent<AudioSource>();
            musicSrc.playOnAwake = false;
            musicSrc.volume = 0.6f;
            var lowpass = musicGo.AddComponent<AudioLowPassFilter>();
            lowpass.cutoffFrequency = 22000f;
            var sfxGo = new GameObject("Efektler");
            var sfxSrc = sfxGo.AddComponent<AudioSource>();
            sfxSrc.playOnAwake = false;

            // ---------- Arayüz ----------
            var ui = BuildUI();

            // ---------- Yönetici ----------
            var gmGo = new GameObject("OyunYoneticisi");
            var gm = gmGo.AddComponent<GameManager>();
            gm.level = level;
            gm.ui = ui;
            gm.heatOverlay = heat;
            gm.musicSource = musicSrc;
            gm.musicFilter = lowpass;
            gm.sfxSource = sfxSrc;

            // Yeni 6 kartlık set (v2)
            Level1Update.ApplyData();
            Level1Update.ApplyScene(Object.FindObjectsByType<Slot>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            AssetDatabase.SaveAssets();

            string scenePath = SceneDir + "/Bolum1.unity";
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
            AddSceneToBuild(scenePath);
            SetupPlayer();

            if (showDialog)
                EditorUtility.DisplayDialog("Hazır!",
                    "Bölüm 1 kuruldu.\n\nŞimdi 'Bölüm 1 Görsellerini Kur' komutunu çalıştır.", "Süper");
            return true;
        }

        // =====================================================================
        //  DÜNYA (yer tutucu sokak)
        // =====================================================================

        static SpriteRenderer BuildWorld(Transform world)
        {
            var ph = new GameObject("YerTutucu (gorsel gelince kapatilabilir)").transform;
            ph.SetParent(world, false);

            Shape("Gokyuzu", ph, sqC, new Vector2(0, 0), new Vector2(30, 12), "#8FD0F2", "#F2B27A", 0);
            var sun = Shape("Gunes", ph, circle, new Vector2(6f, 2.6f), new Vector2(1.3f, 1.3f), "#FFF4B8", "#FF8A3D", 1);
            var sunEv = sun.GetComponent<EraVisual>();
            sunEv.scaleWithEra = true;
            sunEv.scale2026 = new Vector3(1.3f, 1.3f, 1);
            sunEv.scale2050 = new Vector3(2.6f, 2.6f, 1);

            float[] cx = { -3.2f, -0.8f, 1.2f, 4.0f };
            float[] cw = { 1.4f, 1.8f, 1.2f, 1.6f };
            float[] chh = { 3.0f, 3.8f, 2.6f, 3.4f };
            for (int i = 0; i < cx.Length; i++)
                Shape("UzakBina" + i, ph, sqC, new Vector2(cx[i], -2.2f + chh[i] / 2f), new Vector2(cw[i], chh[i]), "#B9D6E6", "#D8A27E", 2);

            Shape("Asfalt", ph, sqC, new Vector2(0, -3.9f), new Vector2(30, 3.4f), "#4F5663", "#5E4B42", 3);
            Shape("Kaldirim", ph, sqC, new Vector2(0, -2.35f), new Vector2(30, 0.3f), "#CFCFCF", "#C9AE8E", 4);

            // Apartman
            Shape("Apartman", ph, sqC, new Vector2(-6f, 0.05f), new Vector2(3.4f, 4.5f), "#EED9B6", "#C79A6E", 5);
            Shape("CatiKenari", ph, sqC, new Vector2(-6f, 2.35f), new Vector2(3.6f, 0.12f), "#8A6E57", "#6E4F3A", 6);
            float[] wx = { -7f, -6f, -5f };
            float[] wy = { -1.2f, -0.1f, 1.0f };
            foreach (var x in wx)
                foreach (var y in wy)
                    Shape("Pencere", ph, sqC, new Vector2(x, y), new Vector2(0.6f, 0.7f), "#7FB7E0", "#7A4B33", 6);

            // Otobüs durağı
            Shape("DurakDirek", ph, sqC, new Vector2(-2f, -1.1f), new Vector2(0.12f, 2.2f), "#7F8C8D", "#6F6A66", 6);
            Shape("DurakCati", ph, sqC, new Vector2(-1.45f, 0f), new Vector2(1.4f, 0.12f), "#7F8C8D", "#6F6A66", 6);
            Shape("DurakBank", ph, sqC, new Vector2(-1.4f, -1.85f), new Vector2(1.0f, 0.15f), "#8E6B4A", "#6E5039", 6);
            Shape("DurakTabela", ph, circle, new Vector2(-2f, 0.25f), new Vector2(0.4f, 0.4f), "#2E86C1", "#5D6D7E", 7);

            // Otopark
            Shape("Araba1", ph, sqC, new Vector2(1.7f, -1.85f), new Vector2(1.5f, 0.6f), "#E74C3C", "#A1453A", 6);
            Shape("Araba1Ust", ph, sqC, new Vector2(1.7f, -1.4f), new Vector2(0.9f, 0.35f), "#E74C3C", "#A1453A", 6);
            Shape("Araba2", ph, sqC, new Vector2(4.5f, -1.85f), new Vector2(1.5f, 0.6f), "#3D7DCA", "#5B5F7A", 6);
            Shape("Araba2Ust", ph, sqC, new Vector2(4.5f, -1.4f), new Vector2(0.9f, 0.35f), "#3D7DCA", "#5B5F7A", 6);

            // Okul
            Shape("Okul", ph, sqC, new Vector2(6.9f, -0.5f), new Vector2(3.4f, 3.4f), "#F6C1B5", "#C88F7E", 5);
            Shape("OkulTabela", ph, sqC, new Vector2(6.9f, 0.9f), new Vector2(1.6f, 0.45f), "#FFFFFF", "#E8D6C0", 6);
            WorldText("OkulYazi", ph, new Vector2(6.9f, 0.9f), "OKUL", 3f, Hex("#C0392B"), 7);
            Shape("OkulPencere1", ph, sqC, new Vector2(6.1f, -0.2f), new Vector2(0.7f, 0.7f), "#7FB7E0", "#7A4B33", 6);
            Shape("OkulPencere2", ph, sqC, new Vector2(7.7f, -0.2f), new Vector2(0.7f, 0.7f), "#7FB7E0", "#7A4B33", 6);
            Shape("OkulCit", ph, sqC, new Vector2(6.9f, -1.8f), new Vector2(3.6f, 0.08f), "#FFFFFF", "#BFA88A", 7);

            // Gerçek arka plan görselleri için boş yuvalar
            var bg26 = new GameObject("Arkaplan_2026 (gorselini buraya ata)");
            bg26.transform.SetParent(world, false);
            bg26.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            var bg26sr = bg26.AddComponent<SpriteRenderer>();
            bg26sr.sortingOrder = 8;
            var ev26 = bg26.AddComponent<EraVisual>();
            ev26.mode = EraVisual.Mode.Tint;

            var bg50 = new GameObject("Arkaplan_2050 (gorselini buraya ata)");
            bg50.transform.SetParent(world, false);
            bg50.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            var bg50sr = bg50.AddComponent<SpriteRenderer>();
            bg50sr.sortingOrder = 9;
            var ev50 = bg50.AddComponent<EraVisual>();
            ev50.mode = EraVisual.Mode.Only2050;

            // Sıcaklık perdesi
            var heatGo = new GameObject("SicaklikPerdesi");
            heatGo.transform.SetParent(world, false);
            heatGo.transform.localScale = new Vector3(30, 14, 1);
            var heat = heatGo.AddComponent<SpriteRenderer>();
            heat.sprite = sqC;
            heat.color = Alpha(Hex("#FF6A00"), 0f);
            heat.sortingOrder = 50;
            return heat;
        }

        static SpriteRenderer Shape(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size,
            string c26, string c50, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = Hex(c50);
            var ev = go.AddComponent<EraVisual>();
            ev.mode = EraVisual.Mode.Tint;
            ev.color2026 = Hex(c26);
            ev.color2050 = Hex(c50);
            return sr;
        }

        static TextMeshPro WorldText(string name, Transform parent, Vector2 pos, string text, float size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshPro>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.rectTransform.sizeDelta = new Vector2(4f, 1f);
            t.sortingOrder = order;
            go.transform.localPosition = pos;
            return t;
        }

        static void MakeSlot(Transform parent, string id, string tag, string area, Vector2 pos,
            Vector2 colSize, Vector2 colOffset, Vector2 markerSize, float labelY = 0.6f)
        {
            var go = new GameObject("Yuva_" + id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = colSize;
            col.offset = colOffset;

            var marker = new GameObject("Isaret").AddComponent<SpriteRenderer>();
            marker.transform.SetParent(go.transform, false);
            marker.transform.localScale = new Vector3(markerSize.x, markerSize.y, 1f);
            marker.sprite = circle;
            marker.sortingOrder = 15;

            var visual = new GameObject("Gorsel").transform;
            visual.SetParent(go.transform, false);

            var o26 = new GameObject("Obje2026").AddComponent<SpriteRenderer>();
            o26.transform.SetParent(visual, false);
            o26.sortingOrder = 20;
            o26.enabled = false;
            var o50 = new GameObject("Obje2050").AddComponent<SpriteRenderer>();
            o50.transform.SetParent(visual, false);
            o50.sortingOrder = 21;
            o50.enabled = false;

            var itemLabel = WorldText("KartAdi", visual, new Vector2(0, 1f), "", 2.6f, Hex("#1E1E1E"), 25);
            itemLabel.enabled = false;
            var areaLabel = WorldText("YuvaAdi", go.transform, new Vector2(0, labelY), "+ " + area, 2.8f, Hex("#1F3A4D"), 25);
            areaLabel.fontStyle = FontStyles.Bold;

            var slot = go.AddComponent<Slot>();
            slot.slotTag = tag;
            slot.areaName = area;
            slot.visualRoot = visual;
            slot.obj2026 = o26;
            slot.obj2050 = o50;
            slot.marker = marker;
            slot.itemLabel = itemLabel;
            slot.areaLabel = areaLabel;
            slot.placeholderSprite = sqB;
        }

        // =====================================================================
        //  ARAYÜZ
        // =====================================================================

        // ---------- Arayüz görselleri (Art/UI) ----------
        const string UiDir = Root + "/Art/UI";

        /// <summary>Art/UI içindeki bir görseli esnetme kenarlarıyla içe aktarır; yoksa null döner.</summary>
        internal static Sprite UiArt(string name, Vector4 border)
        {
            string path = UiDir + "/" + name + ".png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return null;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.Center;
            st.spriteMeshType = SpriteMeshType.FullRect;
            st.spriteBorder = border; // (sol, alt, sağ, üst)
            imp.SetTextureSettings(st);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static GameUI BuildUI()
        {
            // Görseller (yoksa eski düz görünüm kullanılır)
            var aPanel = UiArt("UI_panel", new Vector4(75, 75, 75, 75));
            var aToast = UiArt("UI_bildirim", new Vector4(40, 40, 40, 40));
            var aShelf = UiArt("UI_el", new Vector4(40, 30, 40, 40));
            var aCard = UiArt("UI_kart", new Vector4(60, 60, 60, 60));
            var aBtnG = UiArt("UI_buton_yesil", new Vector4(62, 40, 62, 40));
            var aBtnO = UiArt("UI_buton_turuncu", new Vector4(62, 40, 62, 40));
            var aRail = UiArt("UI_kaydirici_ray", new Vector4(39, 0, 39, 0));
            var aKnob = UiArt("UI_kaydirici_tutamac", Vector4.zero);
            var aThermo = UiArt("UI_termometre", Vector4.zero);
            var aLetter = UiArt("UI_mektup", Vector4.zero);
            var aFact = UiArt("UI_bilgi_kutusu", new Vector4(110, 60, 130, 120));
            var aLeaf = UiArt("yaprak", Vector4.zero);
            var aStar = UiArt("yildiz", Vector4.zero);
            bool art = aPanel != null;

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            var c = canvasGo.transform;
            var ui = canvasGo.AddComponent<GameUI>();
            ui.roundedSprite = uiRounded;
            ui.circleSprite = circle;
            ui.cardSprite = aCard;
            ui.cardSpriteMultiplier = 2f;
            ui.badgeSprite = aLeaf;

            Color panelDark = new Color(0f, 0f, 0f, 0.38f);
            Color paper = Hex("#F6EBD2");
            Color ink = Hex("#3E2A17");
            Color barText = art ? Hex("#4A2F1A") : Color.white;      // üst bar yazıları
            Color barSoft = art ? Hex("#7A5A3A") : new Color(1, 1, 1, 0.8f);
            Color green = art ? Hex("#3E7A2E") : Hex("#BFF0C8");
            Color orange = art ? Hex("#B5532A") : Hex("#FFC9A8");

            // ----- Bütçe (sol üst) -----
            var budget = Anchored("Butce", c, new Vector2(0, 1), new Vector2(150, -24), new Vector2(260, 110));
            Skin(budget, aPanel, 2.7f, uiRounded, panelDark);
            var leaf = MakeRect("Yaprak", budget, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(72, 72));
            Skin(leaf, aLeaf, 1f, circle, Hex("#2E9E5B"), false, true);
            ui.budgetText = Txt(MakeRect("Sayi", budget, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(104, 10), new Vector2(-110, -30)),
                "6", 50, barText, TextAlignmentOptions.Left, true);
            Txt(MakeRect("Etiket", budget, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(158, 18), new Vector2(-160, 30)),
                "yaprak", 22, barSoft, TextAlignmentOptions.Left);

            // ----- Zaman (orta üst) -----
            var time = Anchored("Zaman", c, new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(980, 165));
            Skin(time, aPanel, 2.7f, uiRounded, panelDark);
            ui.yearText = Txt(MakeRect("Yil", time, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(400, 72)),
                "2050", 58, barText, TextAlignmentOptions.Center, true);

            var res = new DefaultControls.Resources { standard = uiRounded, background = uiBackground, knob = uiKnob };
            var sliderGo = DefaultControls.CreateSlider(res);
            sliderGo.name = "ZamanKaydirici";
            var srt = (RectTransform)sliderGo.transform;
            srt.SetParent(time, false);
            srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 1f);
            srt.anchoredPosition = new Vector2(0, -102);
            srt.sizeDelta = new Vector2(680, 36);
            var slider = sliderGo.GetComponent<Slider>();
            slider.value = 1f;
            var sBg = sliderGo.transform.Find("Background");
            if (sBg) ApplySkin(sBg.GetComponent<Image>(), aRail, 2.17f, uiBackground, new Color(1, 1, 1, 0.35f));
            var sFill = sliderGo.transform.Find("Fill Area/Fill");
            if (sFill) sFill.GetComponent<Image>().color = art ? new Color(1f, 0.6f, 0.2f, 0.45f) : Hex("#FFB347");
            var sHandle = sliderGo.transform.Find("Handle Slide Area/Handle");
            if (sHandle)
            {
                ((RectTransform)sHandle).sizeDelta = art ? new Vector2(78, 46) : new Vector2(60, 24);
                ApplySkin(sHandle.GetComponent<Image>(), aKnob, 1f, uiKnob, Color.white, true);
            }
            ui.timeSlider = slider;

            Txt(MakeRect("Etiket2026", time, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(-415, -120), new Vector2(110, 44)),
                "2026", 30, green, TextAlignmentOptions.Center, true);
            Txt(MakeRect("Etiket2050", time, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(415, -120), new Vector2(110, 44)),
                "2050", 30, orange, TextAlignmentOptions.Center, true);

            // ----- Ölçüt (sağ üst) -----
            var meter = Anchored("Olcut", c, new Vector2(1, 1), new Vector2(-60, -24), new Vector2(380, 170));
            Skin(meter, aPanel, 2.7f, uiRounded, panelDark);
            float ml = 0f;
            if (aThermo)
            {
                var th = MakeRect("Termometre", meter, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(48, 128));
                Skin(th, aThermo, 1f, null, Color.white, false, true);
                ui.meterIcon = th.GetComponent<Image>();
                ml = 62f;
            }
            ui.meterLabelText = Txt(MakeRect("Baslik", meter, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(ml / 2, -16), new Vector2(-30 - ml, 32)),
                "Sokak Sıcaklığı", 23, barSoft);
            ui.meterValueText = Txt(MakeRect("Deger", meter, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(ml / 2, -44), new Vector2(-30 - ml, 54)),
                "48°C", 48, barText, TextAlignmentOptions.Center, true);
            var bar = MakeRect("Cubuk", meter, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(ml / 2, 50), new Vector2(300 - ml, 20));
            Skin(bar, aRail, 3.9f, uiRounded, new Color(1, 1, 1, 0.25f));
            var fill = MakeRect("Dolgu", bar, Vector2.zero, new Vector2(0.5f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            fill.offsetMin = new Vector2(4, 4); fill.offsetMax = new Vector2(-4, -4);
            ui.meterFillImage = Img(fill, uiRounded, Hex("#F24D26"));
            ui.meterFill = fill;
            var tick = MakeRect("HedefCizgisi", bar, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5, 14));
            Img(tick, null, art ? Hex("#3E7A2E") : Color.white);
            ui.meterTargetTick = tick;
            ui.goalText = Txt(MakeRect("Hedef", meter, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(ml / 2, 16), new Vector2(-30 - ml, 30)),
                "Hedef: 38°C veya altı", 20, green);

            // ----- Kart eli (alt) -----
            var handPanel = MakeRect("KartEli", c, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 320));
            Skin(handPanel, aShelf, 1f, null, new Color(0.05f, 0.08f, 0.1f, 0.5f), true);
            var info = MakeRect("BilgiSeridi", handPanel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(-200, 44));
            ui.infoText = Txt(info, "", 26, art ? Hex("#4A2F1A") : Color.white);
            AutoSize(ui.infoText, 16, 26);

            var hand = MakeRect("Kartlar", handPanel, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 266));
            var hlg = hand.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 22;
            hlg.padding = new RectOffset(20, 20, 26, 10);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            ui.handContainer = hand;

            // ----- Onay butonu (sağ üst, ölçütün altında) -----
            ui.confirmButton = MakeButton("GelecegiMuhurle", c, new Vector2(1, 1), new Vector2(-60, -210), new Vector2(380, 96),
                aBtnG, Hex("#2E9E5B"), "Geleceği Mühürle", 32, out _);

            // ----- Bildirim -----
            var toast = Anchored("Bildirim", c, new Vector2(0.5f, 1), new Vector2(0, -190), new Vector2(1080, 124));
            Skin(toast, aToast, 1.4f, uiRounded, new Color(0.08f, 0.1f, 0.14f, 0.85f));
            var tcg = toast.gameObject.AddComponent<CanvasGroup>();
            tcg.blocksRaycasts = false;
            tcg.interactable = false;
            tcg.alpha = 0f;
            ui.toastGroup = tcg;
            ui.toastText = Txt(Fill("Yazi", toast, 40, 16, 40, 16), "", 30, art ? Hex("#3E2A17") : Color.white);
            AutoSize(ui.toastText, 18, 30);

            // ----- Sürükleme katmanı -----
            ui.dragLayer = Fill("SuruklemeKatmani", c);

            // ----- Mektup paneli -----
            var letter = Fill("MektupPaneli", c);
            Img(letter, null, new Color(0, 0, 0, 0.65f), true);
            var lp = Anchored("Kagit", letter, new Vector2(0.5f, 0.5f), Vector2.zero, art && aLetter ? new Vector2(1250, 875) : new Vector2(1150, 820));
            Skin(lp, aLetter, 1f, uiRounded, paper);
            if (aLetter)
            {
                ui.letterTitle = Txt(MakeRect("Baslik", lp, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -70), new Vector2(720, 70)),
                    "Bölüm", 46, Hex("#7A4A22"), TextAlignmentOptions.Left, true);
                ui.letterBody = Txt(Fill("Metin", lp, 110, 150, 110, 270), "", 28, ink, TextAlignmentOptions.TopLeft);
            }
            else
            {
                ui.letterTitle = Txt(MakeRect("Baslik", lp, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(-80, 70)),
                    "Bölüm", 46, Hex("#7A4A22"), TextAlignmentOptions.Center, true);
                ui.letterBody = Txt(Fill("Metin", lp, 80, 150, 80, 130), "", 30, ink, TextAlignmentOptions.TopLeft);
            }
            AutoSize(ui.letterBody, 18, 30);
            ui.letterButton = MakeButton("Buton", lp, new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(380, 92),
                aBtnO, Hex("#C8553D"), "Geleceği Gör", 34, out var lbt);
            ui.letterButtonText = lbt;
            ui.letterPanel = letter.gameObject;

            // ----- Karne paneli -----
            var result = Fill("KarnePaneli", c);
            Img(result, null, new Color(0, 0, 0, 0.7f), true);
            var rp = Anchored("Kagit", result, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 940));
            Skin(rp, aCard, 0.9f, uiRounded, paper);
            Txt(MakeRect("Baslik", rp, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(-120, 64)),
                "2050 Karnesi", 46, Hex("#7A4A22"), TextAlignmentOptions.Center, true);
            var stars = new List<Image>();
            for (int i = 0; i < 3; i++)
            {
                var st = MakeRect("Yildiz" + (i + 1), rp, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f),
                    new Vector2((i - 1) * 130, -170), new Vector2(104, 104));
                stars.Add(Skin(st, aStar, 1f, circle, Hex("#C8BFB0"), false, true));
            }
            ui.resultStars = stars.ToArray();
            if (aStar) { ui.starOn = Color.white; ui.starOff = new Color(0.35f, 0.33f, 0.3f, 0.45f); }
            ui.resultSummary = Txt(MakeRect("Ozet", rp, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -232), new Vector2(-140, 84)),
                "", 26, ink);
            AutoSize(ui.resultSummary, 18, 26);
            ui.resultLetter = Txt(Fill("Mektup", rp, 110, 345, 110, 325), "", 26, ink, TextAlignmentOptions.TopLeft);
            ui.resultLetter.fontStyle = FontStyles.Italic;
            AutoSize(ui.resultLetter, 16, 26);
            var factBox = MakeRect("BilgiKutusu", rp, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(-160, 180));
            Skin(factBox, aFact, 1.6f, uiRounded, Hex("#DCEFD6"));
            ui.resultFact = Txt(Fill("Yazi", factBox, aFact ? 80 : 24, 18, aFact ? 90 : 24, aFact ? 34 : 12), "", 23, Hex("#24492E"), TextAlignmentOptions.MidlineLeft);
            AutoSize(ui.resultFact, 15, 23);
            ui.continueButton = MakeButton("TekrarDene", rp, new Vector2(0.5f, 0), new Vector2(-210, 62), new Vector2(360, 86),
                aBtnO, Hex("#8C7B6B"), "Tekrar Dene", 30, out _);
            ui.nextButton = MakeButton("SonrakiBolum", rp, new Vector2(0.5f, 0), new Vector2(210, 62), new Vector2(360, 86),
                aBtnG, Hex("#2E9E5B"), "Sonraki Bölüm", 30, out _);
            ui.resultPanel = result.gameObject;
            result.gameObject.SetActive(false);

            BuildPause(c, aPanel, aBtnG, aBtnO, aLetter, paper, ink);
            return ui;
        }

        // ----- Duraklatma -----
        static void BuildPause(Transform c, Sprite aPanel, Sprite aBtnG, Sprite aBtnO, Sprite aLetter, Color paper, Color ink)
        {
            // Sol üstte yuvarlak duraklatma butonu
            var pb = Anchored("DurdurButonu", c, new Vector2(0, 1), new Vector2(40, -30), new Vector2(96, 96));
            var pbImg = Skin(pb, aPanel, 3.2f, uiRounded, new Color(0, 0, 0, 0.38f), true);
            var pauseBtn = pb.gameObject.AddComponent<Button>();
            pauseBtn.targetGraphic = pbImg;
            for (int i = 0; i < 2; i++)
            {
                var bar = MakeRect("Cizgi" + i, pb, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(i == 0 ? -11 : 11, 0), new Vector2(13, 40));
                Img(bar, uiRounded, aPanel ? Hex("#4A2F1A") : Color.white);
            }

            // Panel
            var panel = Fill("DurdurmaPaneli", c);
            Img(panel, null, new Color(0.03f, 0.05f, 0.1f, 0.72f), true);
            var card = Anchored("Kagit", panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 600));
            Skin(card, aPanel, 1.6f, uiRounded, paper);
            Txt(MakeRect("Baslik", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(-80, 80)),
                "Oyun Durduruldu", 52, Hex("#7A4A22"), TextAlignmentOptions.Center, true);
            Txt(MakeRect("Alt", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -122), new Vector2(-80, 44)),
                "2050 seni bekliyor...", 28, Hex("#7A5A3A"), TextAlignmentOptions.Center);
            var resume = MakeButton("DevamEt", card, new Vector2(0.5f, 1), new Vector2(0, -205), new Vector2(480, 100),
                aBtnG, Hex("#2E9E5B"), "Devam Et", 36, out _);
            var restart = MakeButton("BastanBaslat", card, new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(480, 96),
                aBtnO, Hex("#C8553D"), "Bölümü Baştan Başlat", 30, out _);
            var menu = MakeButton("AnaMenu", card, new Vector2(0.5f, 1), new Vector2(0, -450), new Vector2(480, 96),
                aBtnO, Hex("#8C7B6B"), "Ana Menüye Dön", 30, out _);
            if (aBtnO) ((Image)menu.targetGraphic).color = new Color(0.82f, 0.78f, 0.74f); // ikincil buton: daha soluk

            var pm = c.gameObject.AddComponent<PauseMenu>();
            pm.pauseButton = pauseBtn;
            pm.panel = panel.gameObject;
            pm.resumeButton = resume;
            pm.restartButton = restart;
            pm.menuButton = menu;
            panel.gameObject.SetActive(false);
        }

        /// <summary>Görselli (varsa) buton.</summary>
        static Button MakeButton(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size,
            Sprite art, Color fallback, string label, float fontSize, out TextMeshProUGUI labelText)
        {
            var b = Btn(name, parent, anchor, pos, size, uiRounded, fallback, label, fontSize, out labelText);
            if (art != null)
            {
                ApplySkin((Image)b.targetGraphic, art, 1.3f, uiRounded, fallback);
                ((RectTransform)labelText.transform).offsetMin = new Vector2(20, 10);
                ((RectTransform)labelText.transform).offsetMax = new Vector2(-20, -4);
            }
            return b;
        }

        // =====================================================================
        //  YARDIMCILAR
        // =====================================================================

        internal static void CreateEventSystem()
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_LEGACY_INPUT_MANAGER
            es.AddComponent<StandaloneInputModule>();
#else
            var t = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            // Varsayılan UI kontrollerini Input System oyun başlarken kendisi atar.
            if (t != null) es.AddComponent(t);
            else es.AddComponent<StandaloneInputModule>();
#endif
        }

        static Sprite MakeSprite(string file, int size, bool round, Vector2 pivot, float ppu)
        {
            string path = GenDir + "/" + file;
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var px = new Color32[size * size];
                float r = size / 2f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        byte a = 255;
                        if (round)
                        {
                            float dx = x + 0.5f - r, dy = y + 0.5f - r;
                            a = (byte)(Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy)) * 255f);
                        }
                        px[y * size + x] = new Color32(255, 255, 255, a);
                    }
                tex.SetPixels32(px);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = ppu;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void AddSceneToBuild(string path)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == path)) list.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static void SetupPlayer()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }
    }
}
