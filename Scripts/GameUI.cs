using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZamaninMektubu
{
    /// <summary>
    /// Bütün ekran arayüzü: üst bar, kartlar, bildirimler, mektup ve karne.
    /// Referansları sahne kurulum menüsü otomatik doldurur.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("Üst bar")]
        public TMP_Text budgetText;
        public TMP_Text yearText;
        public Slider timeSlider;
        public TMP_Text meterLabelText;
        public TMP_Text meterValueText;
        public TMP_Text goalText;
        public RectTransform meterFill;
        public Image meterFillImage;
        public RectTransform meterTargetTick;
        public Image meterIcon;

        [Header("Kartlar")]
        public RectTransform handContainer;
        public RectTransform dragLayer;
        public TMP_Text infoText;
        public Sprite roundedSprite;
        public Sprite circleSprite;
        [Tooltip("Kart çerçevesi görseli (boşsa düz kutu)")]
        public Sprite cardSprite;
        public float cardSpriteMultiplier = 2f;
        [Tooltip("Maliyet rozeti görseli (boşsa yeşil daire)")]
        public Sprite badgeSprite;

        [Header("Bildirim")]
        public CanvasGroup toastGroup;
        public TMP_Text toastText;

        [Header("Onay")]
        public Button confirmButton;

        [Header("Mektup")]
        public GameObject letterPanel;
        public TMP_Text letterTitle;
        public TMP_Text letterBody;
        public TMP_Text letterButtonText;
        public Button letterButton;
        public float secondsPerChar = 0.02f;

        [Header("Karne")]
        public GameObject resultPanel;
        public Image[] resultStars;
        public TMP_Text resultSummary;
        public TMP_Text resultLetter;
        public TMP_Text resultFact;
        public Button continueButton;
        public Button nextButton;

        [Header("Renkler")]
        public Color meterGood = new Color(0.25f, 0.7f, 0.95f);
        public Color meterBad = new Color(0.95f, 0.3f, 0.15f);
        public Color starOn = new Color(1f, 0.78f, 0.2f);
        public Color starOff = new Color(0.78f, 0.74f, 0.68f);

        GameManager gm;
        readonly List<CardUI> cards = new List<CardUI>();
        readonly Queue<KeyValuePair<string, float>> toasts = new Queue<KeyValuePair<string, float>>();
        Coroutine toastRoutine, letterRoutine;
        Action letterClosed;
        string letterButtonLabel;
        bool typing;

        float MeterMin => gm.level.targetValue - 8f;
        float MeterMax => gm.level.untreatedFutureValue + 2f;

        public void Init(GameManager manager)
        {
            gm = manager;

            timeSlider.minValue = 0f;
            timeSlider.maxValue = 1f;
            timeSlider.onValueChanged.AddListener(gm.OnSliderChanged);
            timeSlider.interactable = false;

            confirmButton.onClick.AddListener(gm.ConfirmFuture);
            confirmButton.gameObject.SetActive(false);
            letterButton.onClick.AddListener(OnLetterButton);
            continueButton.onClick.AddListener(gm.ContinuePlaying);
            nextButton.onClick.AddListener(gm.LoadNext);

            resultPanel.SetActive(false);
            toastGroup.alpha = 0f;
            infoText.text = "Bir karta dokun, ne işe yaradığını gör.";

            meterLabelText.text = gm.level.metricLabel;
            if (meterIcon && gm.level.metricIcon) { meterIcon.sprite = gm.level.metricIcon; meterIcon.preserveAspect = true; }
            goalText.text = "Hedef: " + gm.Format(gm.level.targetValue) + " veya altı";
            if (meterTargetTick)
            {
                float x = Mathf.InverseLerp(MeterMin, MeterMax, gm.level.targetValue);
                meterTargetTick.anchorMin = new Vector2(x, 0f);
                meterTargetTick.anchorMax = new Vector2(x, 1f);
                meterTargetTick.anchoredPosition = Vector2.zero;
            }

            foreach (Transform child in handContainer) Destroy(child.gameObject);
            cards.Clear();
            foreach (var d in gm.level.hand)
                if (d != null) cards.Add(CardUI.Create(this, d, handContainer));

            UpdateBudget(gm.BudgetLeft);
        }

        // ---------------- Üst bar ----------------

        public void UpdateBudget(int budget)
        {
            budgetText.text = budget.ToString();
            foreach (var c in cards) c.Refresh(budget);
        }

        public void UpdateEra(float t, float value, bool updateSlider)
        {
            if (updateSlider) timeSlider.SetValueWithoutNotify(t);
            yearText.text = Mathf.RoundToInt(Mathf.Lerp(2026f, 2050f, t)).ToString();
            meterValueText.text = gm.Format(value);

            float f = Mathf.InverseLerp(MeterMin, MeterMax, value);
            meterFill.anchorMax = new Vector2(f, 1f);
            float heat = Mathf.InverseLerp(gm.level.targetValue, gm.level.untreatedFutureValue, value);
            meterFillImage.color = Color.Lerp(meterGood, meterBad, heat);
        }

        public void SetSliderInteractable(bool on) => timeSlider.interactable = on;

        public void ShowConfirm(bool on)
        {
            if (confirmButton.gameObject.activeSelf != on) confirmButton.gameObject.SetActive(on);
        }

        public void ShowInfo(string msg) => infoText.text = msg;

        // ---------------- Bildirimler ----------------

        public void Toast(string msg, float duration = 3f)
        {
            toasts.Enqueue(new KeyValuePair<string, float>(msg, duration));
            if (toastRoutine == null) toastRoutine = StartCoroutine(ToastLoop());
        }

        public void ClearToasts()
        {
            toasts.Clear();
            if (toastRoutine != null) { StopCoroutine(toastRoutine); toastRoutine = null; }
            toastGroup.alpha = 0f;
        }

        IEnumerator ToastLoop()
        {
            while (toasts.Count > 0)
            {
                var item = toasts.Dequeue();
                toastText.text = item.Key;
                float dur = toasts.Count > 0 ? Mathf.Min(item.Value, 2.8f) : item.Value;
                yield return Fade(toastGroup, toastGroup.alpha, 1f, 0.2f);
                yield return new WaitForSeconds(dur);
                yield return Fade(toastGroup, 1f, 0f, 0.25f);
            }
            toastRoutine = null;
        }

        static IEnumerator Fade(CanvasGroup g, float from, float to, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                g.alpha = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }
            g.alpha = to;
        }

        // ---------------- Mektup ----------------

        public void ShowLetter(string title, string body, string buttonLabel, Action onClosed)
        {
            letterClosed = onClosed;
            letterButtonLabel = buttonLabel;
            if (gm) gm.Play(gm.paperClip);
            letterPanel.SetActive(true);
            letterTitle.text = title;
            letterBody.text = body;
            if (letterRoutine != null) StopCoroutine(letterRoutine);
            letterRoutine = StartCoroutine(TypeLetter());
        }

        IEnumerator TypeLetter()
        {
            typing = true;
            letterButtonText.text = "Atla";
            letterBody.maxVisibleCharacters = 0;
            letterBody.ForceMeshUpdate();
            int total = letterBody.textInfo.characterCount;
            float shown = 0f;
            while (shown < total)
            {
                shown += Time.deltaTime / Mathf.Max(0.001f, secondsPerChar);
                letterBody.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
                yield return null;
            }
            FinishTyping();
        }

        void FinishTyping()
        {
            typing = false;
            letterBody.maxVisibleCharacters = 99999;
            letterButtonText.text = letterButtonLabel;
        }

        void OnLetterButton()
        {
            if (typing)
            {
                if (letterRoutine != null) StopCoroutine(letterRoutine);
                FinishTyping();
                return;
            }
            letterPanel.SetActive(false);
            var cb = letterClosed;
            letterClosed = null;
            cb?.Invoke();
        }

        // ---------------- Karne ----------------

        public void ShowResult(LevelData level, Outcome o, string fromText, string toText)
        {
            resultPanel.SetActive(true);
            resultSummary.text = level.metricLabel + ":  hiçbir şey yapılmasa " + fromText + ",  senin 2050'nde " + toText + "\n" +
                                 "Hedefe ulaştın  •  " +
                                 (o.usedTrap ? "Tuzağa düştün" : "Tuzaklardan kaçındın") + "  •  " +
                                 (o.combos.Count > 0 ? "Kombo yaptın" : "Kombo bulamadın");
            resultLetter.text = level.winLetter;
            resultFact.text = level.fact;
            nextButton.gameObject.SetActive(!string.IsNullOrEmpty(level.nextSceneName));
            var nextLabel = nextButton.GetComponentInChildren<TMP_Text>();
            if (nextLabel) nextLabel.text = level.nextSceneName == "Final" ? "Finale Geç" : "Sonraki Bölüm";
            StartCoroutine(AnimateStars(o.stars));
        }

        public void HideResult() => resultPanel.SetActive(false);

        IEnumerator AnimateStars(int count)
        {
            foreach (var s in resultStars) { s.color = starOff; s.transform.localScale = Vector3.one; }
            yield return new WaitForSeconds(0.4f);
            for (int i = 0; i < resultStars.Length && i < count; i++)
            {
                var s = resultStars[i];
                s.color = starOn;
                float t = 0f;
                while (t < 0.25f)
                {
                    t += Time.deltaTime;
                    float k = t / 0.25f;
                    s.transform.localScale = Vector3.one * (1f + 0.4f * Mathf.Sin(k * Mathf.PI));
                    yield return null;
                }
                s.transform.localScale = Vector3.one;
                gm.Play(gm.starClip ? gm.starClip : gm.comboClip);
                yield return new WaitForSeconds(0.15f);
            }
        }
    }
}
