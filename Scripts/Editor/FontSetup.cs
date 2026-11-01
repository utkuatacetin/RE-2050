using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ZamaninMektubu.EditorTools
{
    /// <summary>
    /// Nunito (arayüz) ve Caveat (mektuplar) fontlarını TextMeshPro font asset'ine çevirir ve sahnedeki yazılara uygular.
    /// </summary>
    public static class FontSetup
    {
        const string Dir = "Assets/ZamaninMektubu/Fonts";
        const string Chars =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "çÇğĞıİöÖşŞüÜâÂîÎûÛ°•…–—‘’“”«»×";

        public static TMP_FontAsset Ui, UiLight, Hand;

        public static void EnsureFonts()
        {
            Ui = Make("RE2050-Nunito-ExtraBold");
            UiLight = Make("RE2050-Nunito-SemiBold");
            Hand = Make("RE2050-Caveat-SemiBold");
            if (Ui != null)
            {
                if (UiLight != null) AddFallback(Ui, UiLight);
                UIFactory.DefaultFont = Ui;
                SetDefault(Ui);
            }
        }

        static TMP_FontAsset Make(string name)
        {
            string assetPath = Dir + "/" + name + " SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(Dir + "/" + name + ".ttf");
            if (font == null) { Debug.LogWarning("Font bulunamadı: " + name); return null; }

            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048,
                AtlasPopulationMode.Dynamic, true);
            if (fa == null) { Debug.LogWarning("Font asset oluşturulamadı: " + name); return null; }
            fa.name = name + " SDF";
            AssetDatabase.CreateAsset(fa, assetPath);
            fa.TryAddCharacters(Chars, out _);
            if (fa.atlasTextures != null)
                for (int i = 0; i < fa.atlasTextures.Length; i++)
                {
                    var tex = fa.atlasTextures[i];
                    if (tex == null || AssetDatabase.Contains(tex)) continue;
                    tex.name = name + " Atlas " + i;
                    AssetDatabase.AddObjectToAsset(tex, fa);
                }
            if (fa.material != null && !AssetDatabase.Contains(fa.material))
            {
                fa.material.name = name + " Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        static void AddFallback(TMP_FontAsset main, TMP_FontAsset fb)
        {
            if (main.fallbackFontAssetTable == null) main.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            if (!main.fallbackFontAssetTable.Contains(fb)) { main.fallbackFontAssetTable.Add(fb); EditorUtility.SetDirty(main); }
        }

        static void SetDefault(TMP_FontAsset fa)
        {
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings == null) return;
            var so = new SerializedObject(settings);
            var p = so.FindProperty("m_defaultFontAsset");
            if (p != null) { p.objectReferenceValue = fa; so.ApplyModifiedProperties(); }
        }

        /// <summary>Açık sahnedeki bütün yazılara fontları uygular. Mektup gövdeleri el yazısı olur.</summary>
        public static void ApplyToOpenScene()
        {
            if (Ui == null) EnsureFonts();
            if (Ui == null) return;
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bool hand = Hand != null && IsLetterBody(t.transform);
                Undo.RecordObject(t, "Font");
                if (hand)
                {
                    t.font = Hand;
                    t.fontStyle = FontStyles.Normal;
                    if (t.enableAutoSizing) { t.fontSizeMax = Mathf.Max(t.fontSizeMax, 30f) * 1.3f; t.fontSizeMin *= 1.2f; }
                    else t.fontSize *= 1.3f;
                    t.lineSpacing = -8f;
                }
                else
                {
                    bool soft = UiLight != null && IsSoftText(t.transform);
                    t.font = soft ? UiLight : Ui;
                    t.fontStyle &= ~FontStyles.Bold;   // font zaten kalın; sahte kalınlığı kapat
                }
                EditorUtility.SetDirty(t);
            }
        }

        static bool IsLetterBody(Transform t)
        {
            string n = t.name;
            if (n == "Mektup") return true;                                  // karnedeki mektup
            if (n == "Metin" && t.parent && t.parent.parent && t.parent.parent.name == "MektupPaneli") return true;
            if (n == "FinalMektup") return true;
            return false;
        }

        static bool IsSoftText(Transform t)
        {
            string n = t.name;
            if (n == "BilgiSeridi" || n == "Ozet") return true;
            if (n == "Yazi" && t.parent && (t.parent.name == "Bildirim" || t.parent.name == "BilgiKutusu")) return true;
            return false;
        }
    }
}
