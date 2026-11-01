using UnityEngine;

namespace ZamaninMektubu
{
    /// <summary>
    /// Ekran oranı ne olursa olsun (telefon, tablet, 16:9 monitör) sokağın
    /// tamamının yatayda görünmesini sağlar. Main Camera'ya ekle.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CameraFit : MonoBehaviour
    {
        [Tooltip("Her zaman görünmesi gereken sahne genişliği (dünya birimi).")]
        public float requiredWidth = 21.6f;
        [Tooltip("Kameranın en küçük boyutu. Geniş telefonlarda bu kullanılır.")]
        public float minSize = 5f;

        Camera cam;

        void OnEnable() { cam = GetComponent<Camera>(); Fit(); }
        void Update() { Fit(); }

        void Fit()
        {
            if (!cam || cam.aspect <= 0f) return;
            float size = Mathf.Max(minSize, requiredWidth / cam.aspect * 0.5f);
            if (!Mathf.Approximately(cam.orthographicSize, size)) cam.orthographicSize = size;
        }
    }
}
