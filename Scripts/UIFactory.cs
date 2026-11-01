using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZamaninMektubu
{
    /// <summary>Kodla arayüz parçası üretmek için küçük yardımcılar.</summary>
    public static class UIFactory
    {
        /// <summary>Kurulum sırasında atanırsa bütün yeni yazılar bu fontla oluşturulur.</summary>
        public static TMP_FontAsset DefaultFont;

        public static RectTransform MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Anchored(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
            => MakeRect(name, parent, anchor, anchor, anchor, pos, size);

        /// <summary>Ebeveyni kaplar; left/bottom/right/top kenar boşlukları.</summary>
        public static RectTransform Fill(string name, Transform parent, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            var rt = MakeRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image Img(RectTransform rt, Sprite sprite, Color color, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            return img;
        }

        /// <summary>
        /// Görsel varsa onu (esnetilebilir ise Sliced, mult = kenar inceltme), yoksa yedek görünümü kullanır.
        /// </summary>
        public static Image Skin(RectTransform rt, Sprite art, float mult, Sprite fallback, Color fallbackColor,
            bool raycast = false, bool preserveAspect = false)
        {
            var img = rt.gameObject.GetComponent<Image>();
            if (img == null) img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = raycast;
            ApplySkin(img, art, mult, fallback, fallbackColor, preserveAspect);
            return img;
        }

        public static void ApplySkin(Image img, Sprite art, float mult, Sprite fallback, Color fallbackColor, bool preserveAspect = false)
        {
            if (art != null)
            {
                img.sprite = art;
                img.color = Color.white;
                if (art.border != Vector4.zero)
                {
                    img.type = Image.Type.Sliced;
                    img.pixelsPerUnitMultiplier = Mathf.Max(0.01f, mult);
                }
                else
                {
                    img.type = Image.Type.Simple;
                    img.preserveAspect = preserveAspect;
                }
            }
            else
            {
                img.sprite = fallback;
                img.color = fallbackColor;
                img.type = fallback != null && fallback.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            }
        }

        public static TextMeshProUGUI Txt(RectTransform rt, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (DefaultFont) t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            if (bold) t.fontStyle = FontStyles.Bold;
            return t;
        }

        public static void AutoSize(TMP_Text t, float min, float max)
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = min;
            t.fontSizeMax = max;
        }

        public static Button Btn(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size,
            Sprite sprite, Color color, string label, float fontSize, out TextMeshProUGUI labelText)
        {
            var rt = Anchored(name, parent, anchor, pos, size);
            var img = Img(rt, sprite, color, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
            cb.pressedColor = new Color(0.78f, 0.78f, 0.78f);
            b.colors = cb;
            labelText = Txt(Fill("Yazi", rt, 10, 6, 10, 6), label, fontSize, Color.white, TextAlignmentOptions.Center, true);
            return b;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        public static Color Alpha(Color c, float a) { c.a = a; return c; }
    }
}
