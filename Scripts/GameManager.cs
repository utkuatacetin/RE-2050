using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZamaninMektubu
{
    public struct Outcome
    {
        public float futureValue;
        public bool usedTrap;
        public List<ComboRule> combos;
        public bool goalMet;
        public int stars;
    }

    /// <summary>
    /// Oyunun beyni: zaman, bütçe, kart yerleştirme, 2050 hesabı ve bölüm akışı.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Bölüm")]
        public LevelData level;
        public GameUI ui;

        [Header("Sahne")]
        public SpriteRenderer heatOverlay;
        [Range(0f, 1f)] public float heatOverlayMaxAlpha = 0.45f;

        [Header("Ses (opsiyonel, boş bırakılabilir)")]
        public AudioSource musicSource;
        public AudioLowPassFilter musicFilter;
        public AudioSource sfxSource;
        public AudioClip music;
        public AudioClip placeClip, removeClip, whooshClip, comboClip, trapClip, winClip, errorClip;
        public AudioClip paperClip, starClip;

        public float Era { get; private set; } = 1f;      // 0 = 2026, 1 = 2050
        public int BudgetLeft { get; private set; }
        public bool InPlay { get; private set; }
        public bool IsInPresent => Era <= 0.05f;
        public Outcome Current => outcome;
        public float DisplayedValue => Mathf.Lerp(level.todayValue, outcome.futureValue, Era);

        EraVisual[] eraVisuals;
        RiskVisual[] riskVisuals;
        Outcome outcome;
        readonly HashSet<InterventionData> revealed = new HashSet<InterventionData>();
        readonly HashSet<ComboRule> revealedCombos = new HashSet<ComboRule>();
        int stateVersion, shownVersion = -1;
        bool visitedPresent;
        bool lastSideFuture = true;

        void Awake() { Instance = this; }

        void Start()
        {
            eraVisuals = FindObjectsByType<EraVisual>(FindObjectsSortMode.None);
            riskVisuals = FindObjectsByType<RiskVisual>(FindObjectsSortMode.None);
            if (heatOverlay)
            {
                var oc = level.overlayColor; oc.a = 0f; heatOverlay.color = oc;
                if (level.overlayMaxAlpha >= 0f) heatOverlayMaxAlpha = level.overlayMaxAlpha;
            }
            BudgetLeft = level.budget;
            ui.Init(this);
            Recalculate();
            ApplyEra(1f, true);

            if (music) MusicManager.Play(music);   // sahneler arasında kesilmeden çalar
            MusicManager.SetMuffle(0f);
            ui.ShowLetter(level.levelTitle, level.introLetter, "Geleceği Gör", OnIntroClosed);
        }

        void OnIntroClosed()
        {
            InPlay = true;
            shownVersion = stateVersion;
            ui.SetSliderInteractable(true);
            ui.Toast("Hedef: " + level.goalText, 3.5f);
            ui.Toast("Zaman kaydırıcısını 2026'ya çek ve bugünü değiştir.", 3.5f);
        }

        // ---------------- ZAMAN ----------------

        public void OnSliderChanged(float t) => ApplyEra(t, false);

        void ApplyEra(float t, bool updateSlider)
        {
            Era = Mathf.Clamp01(t);
            foreach (var v in eraVisuals) if (v) v.Apply(Era);
            foreach (var s in Slot.All) s.ShowEra(Era);

            float shown = DisplayedValue;
            float heat = Mathf.InverseLerp(level.targetValue, level.untreatedFutureValue, shown);
            if (heatOverlay)
            {
                var c = heatOverlay.color;
                c.a = heat * heatOverlayMaxAlpha;
                heatOverlay.color = c;
            }
            float futureRisk = Mathf.InverseLerp(level.targetValue, level.untreatedFutureValue, outcome.futureValue);
            if (riskVisuals != null) foreach (var rv in riskVisuals) if (rv) rv.Apply(Era, futureRisk);
            MusicManager.SetMuffle(heat * Era * 0.9f);

            ui.UpdateEra(Era, shown, updateSlider);

            bool sideFuture = Era >= 0.5f;
            if (sideFuture != lastSideFuture) { lastSideFuture = sideFuture; Play(whooshClip); }

            if (!InPlay) { ui.ShowConfirm(false); return; }

            if (IsInPresent && !visitedPresent)
            {
                visitedPresent = true;
                ui.ClearToasts();
                ui.Toast("Bugündesin! Kartları parlayan yuvalara sürükle. Geri almak için objeye dokun.", 4f);
            }
            if (Era >= 0.98f && shownVersion != stateVersion) RevealFuture();
            ui.ShowConfirm(Era >= 0.98f && outcome.goalMet);
        }

        void RevealFuture()
        {
            shownVersion = stateVersion;
            ui.ClearToasts();

            foreach (var d in PlacedInterventions())
            {
                if (revealed.Add(d) && !string.IsNullOrEmpty(d.futureNote))
                {
                    ui.Toast(d.futureNote, 3.5f);
                    if (d.isTrap) Play(trapClip);
                }
            }
            foreach (var c in outcome.combos)
            {
                if (revealedCombos.Add(c))
                {
                    ui.Toast("KOMBO: " + c.comboName + "\n" + c.note, 4f);
                    Play(comboClip);
                }
            }

            string val = Format(outcome.futureValue);
            if (outcome.goalMet)
                ui.Toast("2050: " + val + ". Hedefe ulaştın! Hazırsan geleceği mühürle.", 4f);
            else
                ui.Toast("2050: " + val + ". Hedef " + Format(level.targetValue) + ". Biraz daha çabalaman gerek.", 4f);
        }

        // ---------------- KARTLAR ----------------

        public bool TryPlace(InterventionData d, Slot slot, out string reason)
        {
            reason = null;
            if (!InPlay || PauseMenu.IsPaused) { reason = ""; return false; }

            if (!IsInPresent) reason = "Değişiklik yapmak için önce kaydırıcıyı 2026'ya çek.";
            else if (slot.Current != null) reason = "Bu yuva dolu. Boşaltmak için üstüne dokun.";
            else if (!d.CanGoTo(slot.slotTag)) reason = d.displayName + " buraya konamaz.";
            else if (d.cost > BudgetLeft) reason = "Yeterli yaprağın yok. Bir şeyi geri alabilirsin.";

            if (reason != null) { Play(errorClip); return false; }

            slot.Place(d);
            BudgetLeft -= d.cost;
            Play(placeClip);
            OnChanged();
            return true;
        }

        public void TryClear(Slot slot)
        {
            if (!InPlay || PauseMenu.IsPaused || slot.Current == null) return;
            if (!IsInPresent) { ui.Toast("Geçmişi değiştirmek için 2026'ya dön.", 2.5f); return; }
            if (slot.Current.cost < 0 && BudgetLeft + slot.Current.cost < 0)
            {
                ui.Toast("Bu karttan gelen yapraklar harcandı. Önce başka bir kartı geri al.", 3f);
                Play(errorClip);
                return;
            }
            var d = slot.Clear();
            BudgetLeft += d.cost;
            Play(removeClip);
            OnChanged();
        }

        void OnChanged()
        {
            stateVersion++;
            Recalculate();
            ui.UpdateBudget(BudgetLeft);
            ApplyEra(Era, false);
        }

        List<InterventionData> PlacedInterventions()
        {
            var list = new List<InterventionData>();
            foreach (var s in Slot.All) if (s.Current != null) list.Add(s.Current);
            return list;
        }

        void Recalculate()
        {
            var placed = PlacedInterventions();
            var o = new Outcome { combos = new List<ComboRule>() };
            float v = level.untreatedFutureValue;

            foreach (var d in placed)
            {
                v -= d.impact;
                if (d.isTrap) o.usedTrap = true;
            }

            if (level.combos != null)
            {
                foreach (var c in level.combos)
                {
                    if (c == null || c.a == null || c.b == null) continue;
                    int na = placed.Count(x => x == c.a);
                    int nb = placed.Count(x => x == c.b);
                    bool ok = c.a == c.b ? na >= 2 : (na > 0 && nb > 0);
                    if (ok) { v -= c.bonus; o.combos.Add(c); }
                }
            }

            o.futureValue = v;
            o.goalMet = v <= level.targetValue + 0.001f;
            o.stars = o.goalMet ? 1 + (o.usedTrap ? 0 : 1) + (o.combos.Count > 0 ? 1 : 0) : 0;
            outcome = o;

            revealed.IntersectWith(placed);
            revealedCombos.IntersectWith(o.combos);
        }

        // ---------------- BÖLÜM SONU ----------------

        public void ConfirmFuture()
        {
            if (!InPlay || !outcome.goalMet) return;
            InPlay = false;
            ui.ShowConfirm(false);
            ui.ClearToasts();
            Play(winClip);
            GameProgress.Record(SceneManager.GetActiveScene().name, outcome.stars);
            ui.ShowResult(level, outcome, Format(level.untreatedFutureValue), Format(outcome.futureValue));
        }

        public void ContinuePlaying()
        {
            ui.HideResult();
            InPlay = true;
            ApplyEra(Era, false);
        }

        public void LoadNext()
        {
            if (!string.IsNullOrEmpty(level.nextSceneName)) SceneManager.LoadScene(level.nextSceneName);
        }

        public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // ---------------- YARDIMCI ----------------

        public string Format(float v) => level.unitBefore ? level.unit + Mathf.RoundToInt(v) : Mathf.RoundToInt(v) + level.unit;

        public void Play(AudioClip c)
        {
            if (c && sfxSource) sfxSource.PlayOneShot(c);
        }
    }
}
