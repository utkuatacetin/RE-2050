using System.Collections;
using UnityEngine;

namespace ZamaninMektubu
{
    /// <summary>
    /// Sahneler arasında yaşayan müzik çalar. Aynı parça devam ediyorsa kesmez,
    /// farklı parçaya yumuşakça geçer. 2050'deki "boğuk müzik" efekti ve buton sesleri de buradan.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        static MusicManager instance;
        public static float Volume = 0.55f;

        AudioSource[] src = new AudioSource[2];
        AudioLowPassFilter[] filt = new AudioLowPassFilter[2];
        AudioSource ui;
        int active;
        float duck = 1f;
        Coroutine fadeRoutine;

        static MusicManager Get()
        {
            if (instance) return instance;
            var go = new GameObject("MusicManager");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<MusicManager>();
            for (int i = 0; i < 2; i++)
            {
                var ch = new GameObject("Kanal" + i);
                ch.transform.SetParent(go.transform, false);
                var s = ch.AddComponent<AudioSource>();
                s.loop = true; s.playOnAwake = false; s.volume = 0f; s.ignoreListenerPause = true;
                instance.src[i] = s;
                var f = ch.AddComponent<AudioLowPassFilter>();
                f.cutoffFrequency = 22000f;
                instance.filt[i] = f;
            }
            instance.ui = go.AddComponent<AudioSource>();
            instance.ui.playOnAwake = false;
            instance.ui.ignoreListenerPause = true;
            return instance;
        }

        /// <summary>Parçayı çal. Zaten çalıyorsa dokunma.</summary>
        public static void Play(AudioClip clip, float fade = 1.2f)
        {
            if (clip == null) return;
            var m = Get();
            var cur = m.src[m.active];
            if (cur.clip == clip && cur.isPlaying) return;
            if (m.fadeRoutine != null) m.StopCoroutine(m.fadeRoutine);
            m.fadeRoutine = m.StartCoroutine(m.CrossFade(clip, fade));
        }

        IEnumerator CrossFade(AudioClip clip, float dur)
        {
            var from = src[active];
            active = 1 - active;
            var to = src[active];
            to.clip = clip; to.volume = 0f; to.Play();
            filt[active].cutoffFrequency = 22000f;
            float v0 = from.volume, t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = t / dur;
                to.volume = Volume * duck * k;
                from.volume = v0 * (1f - k);
                yield return null;
            }
            to.volume = Volume * duck;
            from.Stop();
            fadeRoutine = null;
        }

        /// <summary>0 = net, 1 = en boğuk.</summary>
        public static void SetMuffle(float amount)
        {
            if (!instance) return;
            float c = Mathf.Lerp(22000f, 900f, Mathf.Clamp01(amount));
            instance.filt[instance.active].cutoffFrequency = c;
        }

        /// <summary>Duraklatma menüsünde müziği kıs.</summary>
        public static void Duck(bool on)
        {
            if (!instance) return;
            instance.duck = on ? 0.35f : 1f;
            if (instance.fadeRoutine == null) instance.src[instance.active].volume = Volume * instance.duck;
        }

        public static void PlayUi(AudioClip clip, float vol = 0.8f)
        {
            if (clip == null) return;
            Get().ui.PlayOneShot(clip, vol);
        }
    }
}
