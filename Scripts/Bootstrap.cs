using UnityEngine;

namespace ZamaninMektubu
{
    /// <summary>Oyun açılırken bir kez çalışır: mobilde akıcı 60 FPS ve ekranın kararmaması.</summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            QualitySettings.vSyncCount = 0;          // mobilde vSync yerine targetFrameRate geçerli
            Application.targetFrameRate = 60;        // Android varsayılanı 30; 60'a sabitle
            Screen.sleepTimeout = SleepTimeout.NeverSleep; // sunumda ekran kararmasın
#if ENABLE_LEGACY_INPUT_MANAGER
            Input.multiTouchEnabled = false;         // sürüklerken ikinci parmak karışmasın
#endif
        }
    }
}
