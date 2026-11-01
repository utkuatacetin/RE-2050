using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ZamaninMektubu
{
    /// <summary>
    /// Final: kurtarılan üç yerin 2050 hali art arda geçer, Deniz'in son mektubu gelir,
    /// "Devam Et" ile "Devam edecek..." ekranı açılır.
    /// </summary>
    public class FinalController : MonoBehaviour
    {
        [System.Serializable]
        public class Slide
        {
            public string sceneName;      // yıldızların okunacağı bölüm sahnesi
            public string caption;        // "Sokak · 2050"
            public Sprite image;
        }

        [Header("Sahneler")]
        public Slide[] slides;
        public Image slideA, slideB;      // çapraz geçiş için iki katman
        public TMP_Text captionText;
        public Image[] captionStars;
        public CanvasGroup captionGroup;
        public float slideSeconds = 4.2f;
        public float fadeSeconds = 1.1f;

        [Header("Mektup")]
        public CanvasGroup letterGroup;
        public TMP_Text letterTitle;
        public TMP_Text letterBody;
        public TMP_Text totalText;
        public Button continueButton;
        [TextArea(6, 20)] public string finalLetter;

        [Header("Devam edecek")]
        public CanvasGroup endGroup;
        public TMP_Text endLine;
        public TMP_Text endTitle;
        public CanvasGroup endExtras;
        public Button replayButton;
        public string menuScene = "Menu";

        [Header("Renkler")]
        public Color starOn = Color.white;
        public Color starOff = new Color(1f, 1f, 1f, 0.25f);

        void Start()
        {
            letterGroup.alpha = 0f; letterGroup.blocksRaycasts = false;
            endGroup.alpha = 0f; endGroup.blocksRaycasts = false;
            endExtras.alpha = 0f;
            captionGroup.alpha = 0f;
            continueButton.onClick.AddListener(() => StartCoroutine(ToBeContinued()));
            replayButton.onClick.AddListener(() => SceneManager.LoadScene(menuScene));
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            Color clear = new Color(1, 1, 1, 0);
            slideA.color = clear; slideB.color = clear;
            Image front = slideA, back = slideB;

            for (int i = 0; i < slides.Length; i++)
            {
                var s = slides[i];
                back.sprite = s.image;
                back.transform.SetAsLastSibling();
                captionGroup.transform.SetAsLastSibling();
                StartCoroutine(KenBurns(back.rectTransform, slideSeconds + fadeSeconds * 2f, i % 2 == 0));

                // yeni sahne belirir, eskisi kaybolur
                yield return CrossFade(front, back, fadeSeconds);
                ShowCaption(s);
                yield return Fade(captionGroup, 0f, 1f, 0.4f);
                yield return new WaitForSeconds(slideSeconds);
                yield return Fade(captionGroup, 1f, 0f, 0.4f);
                var t = front; front = back; back = t;
            }

            // mektup
            int total = GameProgress.Total;
            totalText.text = total > 0 ? "Toplam yıldız: " + total + " / " + (slides.Length * 3) : "";
            letterBody.text = finalLetter;
            letterGroup.transform.SetAsLastSibling();
            yield return Fade(letterGroup, 0f, 1f, 0.8f);
            letterGroup.blocksRaycasts = true;
        }

        void ShowCaption(Slide s)
        {
            captionText.text = s.caption;
            int n = GameProgress.Get(s.sceneName);
            for (int i = 0; i < captionStars.Length; i++)
            {
                captionStars[i].gameObject.SetActive(n >= 0);
                captionStars[i].color = i < n ? starOn : starOff;
            }
        }

        IEnumerator ToBeContinued()
        {
            continueButton.interactable = false;
            endGroup.transform.SetAsLastSibling();
            endGroup.blocksRaycasts = true;
            endLine.maxVisibleCharacters = 0;
            endTitle.alpha = 0f;
            yield return Fade(endGroup, 0f, 1f, 1.0f);

            endLine.ForceMeshUpdate();
            int n = endLine.textInfo.characterCount;
            for (int i = 1; i <= n; i++) { endLine.maxVisibleCharacters = i; yield return new WaitForSeconds(0.045f); }
            yield return new WaitForSeconds(0.6f);

            float t = 0f;
            while (t < 1.4f)
            {
                t += Time.deltaTime;
                float k = t / 1.4f;
                endTitle.alpha = k;
                endTitle.characterSpacing = Mathf.Lerp(40f, 12f, k);
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);
            yield return Fade(endExtras, 0f, 1f, 0.8f);
        }

        IEnumerator KenBurns(RectTransform rt, float dur, bool zoomIn)
        {
            float t = 0f;
            Vector2 from = zoomIn ? new Vector2(-30f, 0f) : new Vector2(30f, 0f);
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                float s = zoomIn ? Mathf.Lerp(1.02f, 1.1f, k) : Mathf.Lerp(1.1f, 1.02f, k);
                rt.localScale = Vector3.one * s;
                rt.anchoredPosition = Vector2.Lerp(from, -from, k);
                yield return null;
            }
        }

        static IEnumerator CrossFade(Image a, Image b, float dur)
        {
            float t = 0f;
            float a0 = a.color.a;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                b.color = new Color(1, 1, 1, k);
                a.color = new Color(1, 1, 1, a0 * (1f - k));
                yield return null;
            }
            b.color = Color.white;
            a.color = new Color(1, 1, 1, 0);
        }

        static IEnumerator Fade(CanvasGroup g, float a, float b, float dur)
        {
            float t = 0f;
            while (t < dur) { t += Time.deltaTime; g.alpha = Mathf.Lerp(a, b, t / dur); yield return null; }
            g.alpha = b;
        }
    }
}
