using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static ZamaninMektubu.UIFactory;

namespace ZamaninMektubu.EditorTools
{
    /// <summary>
    /// "Zamanın Mektubu > Bütün Oyunu Baştan Kur": fontlar, üç bölüm, açılış ekranı, final, uygulama ikonu.
    /// </summary>
    public static class MenuFinalSetup
    {
        const string Root = "Assets/ZamaninMektubu";
        const string SceneDir = Root + "/Scenes";
        const string MenuScene = SceneDir + "/Menu.unity";
        const string FinalScene = SceneDir + "/Final.unity";

        static readonly Color Navy = Hex("#13203A");
        static readonly Color Ink = Hex("#3E2A17");
        static readonly Color Green = Hex("#2E6E34");
        static readonly Color Orange = Hex("#D65420");
        static readonly Color Paper = Hex("#F6EBD2");

        [MenuItem("Zamanın Mektubu/Bütün Oyunu Baştan Kur", priority = -10)]
        public static void BuildAll()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                EditorUtility.DisplayDialog("TextMeshPro gerekli",
                    "Önce Window > TextMeshPro > Import TMP Essential Resources komutunu çalıştır.", "Tamam");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EditorUtility.DisplayProgressBar("RE: 2050", "Fontlar hazırlanıyor...", 0.05f);
                FontSetup.EnsureFonts();

                EditorUtility.DisplayProgressBar("RE: 2050", "Bölüm 1 kuruluyor...", 0.15f);
                if (!SceneBuilder.Build(false)) return;
                Level1Art.RunInternal(false);
                EditorUtility.DisplayProgressBar("RE: 2050", "Bölüm 2 kuruluyor...", 0.35f);
                if (!Level2Setup.RunInternal(false)) return;
                EditorUtility.DisplayProgressBar("RE: 2050", "Bölüm 3 kuruluyor...", 0.5f);
                if (!Level3Setup.RunInternal(false)) return;

                EditorUtility.DisplayProgressBar("RE: 2050", "Açılış ekranı ve final kuruluyor...", 0.65f);
                BuildMenu();
                BuildFinal();

                EditorUtility.DisplayProgressBar("RE: 2050", "Fontlar ve sesler uygulanıyor...", 0.85f);
                ImportAudio();
                foreach (var path in new[] { SceneDir + "/Bolum1.unity", SceneDir + "/Bolum2.unity", SceneDir + "/Bolum3.unity", MenuScene, FinalScene })
                {
                    var sc = EditorSceneManager.OpenScene(path);
                    FontSetup.ApplyToOpenScene();
                    ApplyAudio(path);
                    EditorSceneManager.MarkSceneDirty(sc);
                    EditorSceneManager.SaveScene(sc);
                }

                SetBuildOrder();
                SetupPlayer();
                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(MenuScene);
            }
            finally { EditorUtility.ClearProgressBar(); }

            EditorUtility.DisplayDialog("RE: 2050 hazır!",
                "Açılış ekranı, üç bölüm ve final kuruldu.\n\nMenu sahnesi açık; Game penceresini 2400x1080 yapıp Play'e bas.", "Süper");
        }

        // =====================================================================
        //  ORTAK
        // =====================================================================

        static Sprite Art(string path, Vector4 border = default)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogWarning("Görsel yok: " + path); return null; }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.maxTextureSize = 4096;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            st.spriteBorder = border;
            imp.SetTextureSettings(st);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Transform NewCanvas(out GameObject cameraGo)
        {
            cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Navy;
            cameraGo.AddComponent<AudioListener>();
            SceneBuilder.CreateEventSystem();

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            return canvasGo.transform;
        }

        static Image FullImage(string name, Transform parent, Sprite sprite, Color color, bool envelope = true)
        {
            var holder = Fill(name, parent);
            var img = Img(holder, sprite, color);
            if (envelope && sprite != null)
            {
                var fit = holder.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = sprite.rect.width / sprite.rect.height;
            }
            return img;
        }

        // =====================================================================
        //  AÇILIŞ EKRANI: Gelen kutusu
        // =====================================================================

        static void BuildMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var c = NewCanvas(out _);

            var bg = Art(Root + "/Art/Final/menu_arkaplan.png");
            var emblem = Art(Root + "/Art/Logo/logo_amblem.png");
            var envelope = Art(Root + "/Art/Final/zarf.png");
            var panel = SceneBuilder.UiArt("UI_panel", new Vector4(75, 75, 75, 75));
            var btnOrange = SceneBuilder.UiArt("UI_buton_turuncu", new Vector4(62, 40, 62, 40));

            FullImage("Arkaplan", c, bg, Color.white);

            // Logo amblemi
            var logo = Anchored("Logo", c, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(320, 320));
            var li = Img(logo, emblem, Color.white); li.preserveAspect = true;

            // Başlık + imleç
            var titleRt = Anchored("Baslik", c, new Vector2(0.5f, 1f), new Vector2(0, -385), new Vector2(1200, 170));
            var title = Txt(titleRt, "RE: 2050", 150, Paper, TextAlignmentOptions.Center);
            title.richText = true;

            var subRt = Anchored("AltBaslik", c, new Vector2(0.5f, 1f), new Vector2(0, -540), new Vector2(1200, 70));
            Txt(subRt, "GELECEĞE CEVAP VER", 46, Hex("#9BD48F"), TextAlignmentOptions.Center).characterSpacing = 12;
            var subGroup = subRt.gameObject.AddComponent<CanvasGroup>();

            // Gelen kutusu kartı
            var mailRoot = Anchored("GelenKutusu", c, new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(1060, 300));
            var mailGroup = mailRoot.gameObject.AddComponent<CanvasGroup>();
            Txt(MakeRect("KutuBaslik", mailRoot, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(10, 6), new Vector2(-20, 44)),
                "Gelen Kutusu", 30, new Color(1, 1, 1, 0.85f), TextAlignmentOptions.Left);

            var card = MakeRect("MesajKarti", mailRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var cardImg = Skin(card, panel, 2.4f, null, Paper, true);
            var btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = cardImg;

            var env = MakeRect("Zarf", card, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(46, 0), new Vector2(170, 170));
            Img(env, envelope, Color.white).preserveAspect = true;

            Txt(MakeRect("Kimden", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(250, -46), new Vector2(-470, 56)),
                "Kimden: <b>Deniz</b>", 40, Ink, TextAlignmentOptions.Left);
            Txt(MakeRect("Tarih", card, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-50, -50), new Vector2(330, 50)),
                "12 Temmuz 2050", 30, Hex("#7A5A3A"), TextAlignmentOptions.Right);
            Txt(MakeRect("Konu", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(250, -112), new Vector2(-300, 56)),
                "Konu: Lütfen yardım et, mahallemiz...", 34, Ink, TextAlignmentOptions.Left);
            var hint = Txt(MakeRect("Ipucu", card, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(250, 40), new Vector2(-300, 46)),
                "Mesajı açmak için dokun", 28, Green, TextAlignmentOptions.Left);

            var badge = MakeRect("YeniRozet", card, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-150, 4), new Vector2(240, 62));
            Skin(badge, btnOrange, 2.6f, null, Orange);
            Txt(Fill("Yazi", badge, 10, 6, 10, 10), "1 YENİ MESAJ", 26, Color.white, TextAlignmentOptions.Center);

            var ctrl = c.gameObject.AddComponent<MenuController>();
            ctrl.titleText = title;
            ctrl.subtitleGroup = subGroup;
            ctrl.mailGroup = mailGroup;
            ctrl.mailCard = card;
            ctrl.mailButton = btn;
            ctrl.logo = logo;

            EditorSceneManager.SaveScene(scene, MenuScene);
        }

        // =====================================================================
        //  FİNAL
        // =====================================================================

        static void BuildFinal()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var c = NewCanvas(out _);
            Img(Fill("Siyah", c), null, Color.black);

            var s1 = Art(Root + "/Art/Final/final_sokak.png");
            var s2 = Art(Root + "/Art/Final/final_dere.png");
            var s3 = Art(Root + "/Art/Final/final_sahil.png");
            var star = SceneBuilder.UiArt("yildiz", Vector4.zero);
            var letterArt = SceneBuilder.UiArt("UI_mektup", Vector4.zero);
            var btnGreen = SceneBuilder.UiArt("UI_buton_yesil", new Vector4(62, 40, 62, 40));
            var btnOrange = SceneBuilder.UiArt("UI_buton_turuncu", new Vector4(62, 40, 62, 40));
            var emblem = Art(Root + "/Art/Logo/logo_amblem.png");

            // Sahneler (çapraz geçiş için iki katman)
            var slides = Fill("Sahneler", c);
            var a = FullImage("SahneA", slides, s1, new Color(1, 1, 1, 0));
            var b = FullImage("SahneB", slides, s1, new Color(1, 1, 1, 0));

            var cap = Anchored("Altyazi", slides, new Vector2(0.5f, 1f), new Vector2(0, -36), new Vector2(760, 150));
            Img(cap, null, new Color(0.07f, 0.12f, 0.22f, 0.72f));
            var capGroup = cap.gameObject.AddComponent<CanvasGroup>();
            var capText = Txt(MakeRect("Yazi", cap, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(-40, 70)),
                "Sokak · 2050", 50, Paper, TextAlignmentOptions.Center);
            var stars = new List<Image>();
            for (int i = 0; i < 3; i++)
            {
                var st = MakeRect("Yildiz" + i, cap, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2((i - 1) * 70, 12), new Vector2(58, 58));
                var si = Img(st, star, Color.white); si.preserveAspect = true; stars.Add(si);
            }

            // Mektup
            var letter = Fill("MektupKatmani", c);
            Img(letter, null, new Color(0, 0, 0, 0.6f), true);
            var letterGroup = letter.gameObject.AddComponent<CanvasGroup>();
            var paper = Anchored("Kagit", letter, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1250, 875));
            Skin(paper, letterArt, 1f, null, Paper);
            var lTitle = Txt(MakeRect("Baslik", paper, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -70), new Vector2(760, 70)),
                "RE: 2050", 46, Hex("#7A4A22"), TextAlignmentOptions.Left);
            var lBody = Txt(Fill("FinalMektup", paper, 110, 175, 110, 160), "", 30, Ink, TextAlignmentOptions.TopLeft);
            AutoSize(lBody, 20, 34);
            var total = Txt(MakeRect("Toplam", paper, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(110, 92), new Vector2(0, 60)),
                "", 32, Green, TextAlignmentOptions.Left);
            var cont = Btn("DevamEt", paper, new Vector2(1, 0), new Vector2(-300, 120), new Vector2(380, 92), null, Hex("#2E9E5B"), "Devam Et", 34, out var contLabel);
            if (btnGreen) ApplySkin((Image)cont.targetGraphic, btnGreen, 1.3f, null, Hex("#2E9E5B"));

            // Devam edecek
            var end = Fill("DevamEdecek", c);
            Img(end, null, Navy, true);
            var endGroup = end.gameObject.AddComponent<CanvasGroup>();
            var endLogo = Anchored("Logo", end, new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(230, 230));
            Img(endLogo, emblem, Color.white).preserveAspect = true;
            var line = Txt(Anchored("Satir", end, new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(1500, 80)),
                "Deniz'in bir sonraki mektubu yolda...", 44, Hex("#C9D6E8"), TextAlignmentOptions.Center);
            var bigTitle = Txt(Anchored("DevamEdecekYazi", end, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1700, 150)),
                "DEVAM EDECEK", 120, Paper, TextAlignmentOptions.Center);

            var extras = Anchored("Ekstralar", end, new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(1600, 300));
            var extrasGroup = extras.gameObject.AddComponent<CanvasGroup>();
            Txt(MakeRect("Siradaki", extras, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(0, 110)),
                "Sıradaki mektuplar:  <color=#FFC46B>Enerji Kesintisi</color>  ·  <color=#9BD48F>Pazar ve Sıfır Atık</color>  ·  <color=#7FD0E0>Kuraklık ve Gıda</color>",
                32, Hex("#C9D6E8"), TextAlignmentOptions.Center);
            var replay = Btn("BastanOyna", extras, new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(400, 92), null, Orange, "Baştan Oyna", 34, out _);
            if (btnOrange) ApplySkin((Image)replay.targetGraphic, btnOrange, 1.3f, null, Orange);

            var ctrl = c.gameObject.AddComponent<FinalController>();
            ctrl.slides = new[]
            {
                new FinalController.Slide { sceneName = "Bolum1", caption = "Sokak · 2050", image = s1 },
                new FinalController.Slide { sceneName = "Bolum2", caption = "Dere · 2050", image = s2 },
                new FinalController.Slide { sceneName = "Bolum3", caption = "Sahil · 2050", image = s3 },
            };
            ctrl.slideA = a; ctrl.slideB = b;
            ctrl.captionText = capText; ctrl.captionStars = stars.ToArray(); ctrl.captionGroup = capGroup;
            ctrl.letterGroup = letterGroup; ctrl.letterTitle = lTitle; ctrl.letterBody = lBody; ctrl.totalText = total;
            ctrl.continueButton = cont;
            ctrl.endGroup = endGroup; ctrl.endLine = line; ctrl.endTitle = bigTitle; ctrl.endExtras = extrasGroup;
            ctrl.replayButton = replay;
            ctrl.finalLetter =
                "Sevgili geçmişteki dostum,\n\n" +
                "Bugün mahallemizin üç köşesini de dolaştım. Çınarların altı serin, dere kıyısında ördekler var, " +
                "kumsalda yavru kaplumbağaların izleri... Sana teşekkür etmenin bir yolunu bulamıyorum.\n\n" +
                "Ama bilmen gereken bir şey var: Bu sabah elektrikler yine kesildi ve pazardaki tezgâhların yarısı boştu. " +
                "Demek ki daha bitmedi.\n\n" +
                "Yakında yine yazacağım. Lütfen cevap vermeyi bırakma.\n\nDeniz";

            EditorSceneManager.SaveScene(scene, FinalScene);
        }

        // =====================================================================
        //  SESLER
        // =====================================================================

        const string AudioDir = Root + "/Audio";
        static readonly string[] Sfx = { "sfx_koy", "sfx_geri_al", "sfx_zaman", "sfx_kombo", "sfx_tuzak", "sfx_kazanma", "sfx_hata", "sfx_tik", "sfx_kagit", "sfx_yildiz" };

        static void ImportAudio()
        {
            foreach (var m in new[] { "muzik_menu", "muzik_oyun", "muzik_final" })
            {
                var imp = AssetImporter.GetAtPath(AudioDir + "/" + m + ".ogg") as AudioImporter;
                if (imp == null) continue;
                var st = imp.defaultSampleSettings;
                st.loadType = AudioClipLoadType.Streaming;      // uzun müzik: belleği doldurmasın
                st.compressionFormat = AudioCompressionFormat.Vorbis;
                st.quality = 0.6f;
                imp.defaultSampleSettings = st;
                imp.loadInBackground = true;
                imp.SaveAndReimport();
            }
            foreach (var n in Sfx)
            {
                var imp = AssetImporter.GetAtPath(AudioDir + "/" + n + ".wav") as AudioImporter;
                if (imp == null) continue;
                var st = imp.defaultSampleSettings;
                st.loadType = AudioClipLoadType.DecompressOnLoad;  // kısa efekt: gecikmesiz çalsın
                st.compressionFormat = AudioCompressionFormat.ADPCM;
                imp.defaultSampleSettings = st;
                imp.forceToMono = true;
                imp.SaveAndReimport();
            }
        }

        static AudioClip Clip(string name)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "/" + name + ".ogg");
            return c ? c : AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "/" + name + ".wav");
        }

        static void ApplyAudio(string scenePath)
        {
            var go = GameObject.Find("SahneSesi");
            if (go == null) go = new GameObject("SahneSesi");
            var sa = go.GetComponent<SceneAudio>();
            if (sa == null) sa = go.AddComponent<SceneAudio>();
            sa.clickClip = Clip("sfx_tik");
            sa.music = scenePath == MenuScene ? Clip("muzik_menu") : scenePath == FinalScene ? Clip("muzik_final") : null;
            EditorUtility.SetDirty(sa);

            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.music = Clip("muzik_oyun");
                gm.placeClip = Clip("sfx_koy");
                gm.removeClip = Clip("sfx_geri_al");
                gm.whooshClip = Clip("sfx_zaman");
                gm.comboClip = Clip("sfx_kombo");
                gm.trapClip = Clip("sfx_tuzak");
                gm.winClip = Clip("sfx_kazanma");
                gm.errorClip = Clip("sfx_hata");
                gm.paperClip = Clip("sfx_kagit");
                gm.starClip = Clip("sfx_yildiz");
                if (gm.sfxSource) gm.sfxSource.volume = 0.9f;
                if (gm.musicSource) gm.musicSource.enabled = false;   // müziği artık MusicManager çalıyor
                EditorUtility.SetDirty(gm);
            }
        }

        // =====================================================================
        //  BUILD AYARLARI
        // =====================================================================

        static void SetBuildOrder()
        {
            var order = new[] { MenuScene, SceneDir + "/Bolum1.unity", SceneDir + "/Bolum2.unity", SceneDir + "/Bolum3.unity", FinalScene };
            var list = new List<EditorBuildSettingsScene>();
            foreach (var p in order) list.Add(new EditorBuildSettingsScene(p, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static void SetupPlayer()
        {
            PlayerSettings.productName = "RE: 2050";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Logo/uygulama_ikonu.png");
            if (tex != null)
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath(Root + "/Art/Logo/uygulama_ikonu.png");
                imp.textureType = TextureImporterType.Default;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Logo/uygulama_ikonu.png");
#pragma warning disable 618
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { tex });
#pragma warning restore 618
            }
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }
    }
}
