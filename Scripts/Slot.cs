using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZamaninMektubu
{
    /// <summary>
    /// Sahnedeki bir müdahale yuvası (otopark, durak, çatı...).
    /// Kart buraya bırakılır; dokununca boşaltılır.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Slot : MonoBehaviour, IPointerClickHandler
    {
        public enum Highlight { None, Valid }
        public static readonly List<Slot> All = new List<Slot>();

        [Header("Yuva")]
        [Tooltip("Hangi kartların konabileceğini belirler (ör. Zemin, Cati).")]
        public string slotTag = "Zemin";
        public string areaName = "Yuva";

        [Header("Bağlantılar (kurulum otomatik doldurur)")]
        public Transform visualRoot;
        public SpriteRenderer obj2026;
        public SpriteRenderer obj2050;
        public SpriteRenderer marker;
        public TMP_Text itemLabel;
        public TMP_Text areaLabel;
        public Sprite placeholderSprite;
        public Color markerColor = new Color(1f, 0.96f, 0.66f, 1f);

        public InterventionData Current { get; private set; }

        float era = 1f;
        Highlight highlight;
        Coroutine popRoutine;

        void Awake()
        {
            if (areaLabel) areaLabel.text = "+ " + areaName;
        }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Place(InterventionData d)
        {
            Current = d;
            ShowEra(era);
            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = StartCoroutine(Pop());
        }

        public InterventionData Clear()
        {
            var d = Current;
            Current = null;
            ShowEra(era);
            return d;
        }

        public void SetHighlight(Highlight h) { highlight = h; }

        public void ShowEra(float t)
        {
            era = t;
            float present = 1f - Mathf.Clamp01(t * 2f);
            if (areaLabel) areaLabel.alpha = Current == null ? present : 0f;

            if (Current == null)
            {
                obj2026.enabled = false;
                obj2050.enabled = false;
                if (itemLabel) itemLabel.enabled = false;
                return;
            }

            var d = Current;
            bool hasArt = d.sprite2026 != null || d.sprite2050 != null;
            if (hasArt)
            {
                var s26 = d.sprite2026 ? d.sprite2026 : d.sprite2050;
                var s50 = d.sprite2050 ? d.sprite2050 : d.sprite2026;
                obj2026.sprite = s26;
                obj2050.sprite = s50;

                if (s26 == s50)
                {
                    float scale = Mathf.Lerp(d.spriteScale2026, d.spriteScale2050, t);
                    obj2026.transform.localScale = Vector3.one * scale;
                    obj2026.enabled = true; obj2026.color = Color.white;
                    obj2050.enabled = false;
                }
                else
                {
                    float sc26 = d.spriteScale2026, sc50 = d.spriteScale2050;
                    if (d.growsOverTime)
                    {
                        // Fidan büyüyerek ağaca dönüşür: iki görsel de aynı yükseklikte geçiş yapar
                        float h26 = Mathf.Max(0.001f, s26.bounds.size.y * d.spriteScale2026);
                        float h50 = Mathf.Max(0.001f, s50.bounds.size.y * d.spriteScale2050);
                        float k = h50 / h26;
                        sc26 = d.spriteScale2026 * Mathf.Lerp(1f, k, t);
                        sc50 = d.spriteScale2050 * Mathf.Lerp(1f / k, 1f, t);
                    }
                    obj2026.transform.localScale = Vector3.one * sc26;
                    obj2050.transform.localScale = Vector3.one * sc50;
                    // Yumuşak geçiş: ortada ikisi de görünür
                    float a50 = Mathf.SmoothStep(0f, 1f, t);
                    obj2026.enabled = true; obj2026.color = new Color(1f, 1f, 1f, 1f - a50);
                    obj2050.enabled = true; obj2050.color = new Color(1f, 1f, 1f, a50);
                }
                if (itemLabel) itemLabel.enabled = false;
            }
            else
            {
                Vector2 size = Vector2.Lerp(d.placeholderSize2026, d.placeholderSize2050, t);
                obj2026.sprite = placeholderSprite;
                obj2026.enabled = true;
                obj2026.transform.localScale = new Vector3(size.x, size.y, 1f);
                obj2026.color = Color.Lerp(Color.Lerp(d.placeholderColor, Color.white, 0.35f), d.placeholderColor, t);
                obj2050.enabled = false;
                if (itemLabel)
                {
                    itemLabel.enabled = true;
                    itemLabel.text = d.displayName;
                    itemLabel.transform.localPosition = new Vector3(0f, size.y + 0.25f, 0f);
                }
            }
        }

        void Update()
        {
            if (!marker) return;
            float present = 1f - Mathf.Clamp01(era * 2f);
            Color c = markerColor;
            float a;
            if (highlight == Highlight.Valid)
            {
                c = new Color(0.35f, 1f, 0.5f);
                a = 0.6f + 0.3f * Mathf.Sin(Time.time * 10f);
            }
            else if (Current == null)
            {
                a = (0.4f + 0.25f * Mathf.Sin(Time.time * 3f)) * present;
            }
            else a = 0f;
            c.a = a;
            marker.color = c;
        }

        IEnumerator Pop()
        {
            if (!visualRoot) yield break;
            float dur = 0.3f, time = 0f;
            while (time < dur)
            {
                time += Time.deltaTime;
                float k = time / dur;
                float s = k < 0.6f ? Mathf.Lerp(0.2f, 1.15f, k / 0.6f) : Mathf.Lerp(1.15f, 1f, (k - 0.6f) / 0.4f);
                visualRoot.localScale = Vector3.one * s;
                yield return null;
            }
            visualRoot.localScale = Vector3.one;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (GameManager.Instance) GameManager.Instance.TryClear(this);
        }
    }
}
