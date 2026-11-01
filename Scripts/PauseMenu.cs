using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ZamaninMektubu
{
    /// <summary>
    /// Duraklatma: oyunu dondurur; Devam Et, Bölümü Baştan Başlat ve Ana Menü seçenekleri.
    /// Telefon arka plana alınınca da kendiliğinden açılır.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public Button pauseButton;
        public GameObject panel;
        public Button resumeButton, restartButton, menuButton;
        public string menuScene = "Menu";

        public static bool IsPaused { get; private set; }

        void Start()
        {
            panel.SetActive(false);
            pauseButton.onClick.AddListener(Pause);
            resumeButton.onClick.AddListener(Resume);
            restartButton.onClick.AddListener(() => Leave(SceneManager.GetActiveScene().name));
            menuButton.onClick.AddListener(() => Leave(menuScene));
        }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) { if (IsPaused) Resume(); else Pause(); } // Android geri tuşu
#endif
        }

        void OnApplicationPause(bool paused) { if (paused && !IsPaused) Pause(); }

        public void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            MusicManager.Duck(true);
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            panel.SetActive(false);
            MusicManager.Duck(false);
        }

        void Leave(string scene)
        {
            IsPaused = false;
            Time.timeScale = 1f;
            MusicManager.Duck(false);
            SceneManager.LoadScene(scene);
        }

        void OnDestroy()
        {
            if (IsPaused) { IsPaused = false; Time.timeScale = 1f; }
        }
    }
}
