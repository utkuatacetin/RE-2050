using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ZamaninMektubu
{
    /// <summary>
    /// Açılış ekranı: "RE: 2050" yazılır, imleç yanıp söner, gelen mesaj kartı belirip nabız gibi atar.
    /// Karta dokununca ilk bölüm açılır.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        public TMP_Text titleText;
        public CanvasGroup subtitleGroup;
        public CanvasGroup mailGroup;
        public RectTransform mailCard;
        public Button mailButton;
        public RectTransform logo;
        public string firstScene = "Bolum1";
        public string title = "RE: 2050";
        public AudioSource sfx;
        public AudioClip clickClip;

        bool cursorOn = true;
        bool typingDone;
        bool leaving;

        void Start()
        {
            GameProgress.Reset();
            subtitleGroup.alpha = 0f;
            mailGroup.alpha = 0f;
            mailGroup.interactable = false;
            mailButton.onClick.AddListener(Open);
            StartCoroutine(Intro());
            StartCoroutine(Blink());
        }

        IEnumerator Intro()
        {
            if (logo) StartCoroutine(Pop(logo, 0.5f));
            titleText.text = Cursor("");
            yield return new WaitForSeconds(0.6f);
            for (int i = 1; i <= title.Length; i++)
            {
                titleText.text = Cursor(title.Substring(0, i));
                yield return new WaitForSeconds(0.11f);
            }
            typingDone = true;
            yield return Fade(subtitleGroup, 0f, 1f, 0.6f);
            yield return new WaitForSeconds(0.3f);
            yield return Fade(mailGroup, 0f, 1f, 0.6f);
            mailGroup.interactable = true;
            StartCoroutine(Pulse());
        }

        string Cursor(string s) => s + (cursorOn ? "<color=#D6542080>|</color>" : "<alpha=#00>|");

        IEnumerator Blink()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                cursorOn = !cursorOn;
                if (typingDone) titleText.text = Cursor(title);
            }
        }

        IEnumerator Pulse()
        {
            while (!leaving)
            {
                float s = 1f + 0.025f * Mathf.Sin(Time.time * 3.2f);
                mailCard.localScale = Vector3.one * s;
                yield return null;
            }
        }

        IEnumerator Pop(RectTransform rt, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float s = k < 0.7f ? Mathf.Lerp(0.6f, 1.06f, k / 0.7f) : Mathf.Lerp(1.06f, 1f, (k - 0.7f) / 0.3f);
                rt.localScale = Vector3.one * s;
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        static IEnumerator Fade(CanvasGroup g, float a, float b, float dur)
        {
            float t = 0f;
            while (t < dur) { t += Time.deltaTime; g.alpha = Mathf.Lerp(a, b, t / dur); yield return null; }
            g.alpha = b;
        }

        void Open()
        {
            if (leaving) return;
            leaving = true;
            if (sfx && clickClip) sfx.PlayOneShot(clickClip);
            SceneManager.LoadScene(firstScene);
        }
    }
}
