using UnityEngine;
using UnityEngine.UI;

namespace ZamaninMektubu
{
    /// <summary>
    /// Her sahnede bir tane: sahnenin müziğini başlatır ve bütün butonlara tık sesi ekler.
    /// </summary>
    public class SceneAudio : MonoBehaviour
    {
        public AudioClip music;
        public AudioClip clickClip;

        void Start()
        {
            if (music) MusicManager.Play(music);
            MusicManager.SetMuffle(0f);
            if (clickClip)
                foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    b.onClick.AddListener(() => MusicManager.PlayUi(clickClip, 0.7f));
        }
    }
}
