using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZamaninMektubu
{
    /// <summary>
    /// Eldeki bir kart. Sürüklenince "hayalet" bir kopya parmağı takip eder,
    /// bırakılınca altındaki yuva bulunur.
    /// </summary>
    public class CardUI : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public InterventionData data;
        GameUI ui;
        CanvasGroup group;
        RectTransform ghost;

        public static CardUI Create(GameUI ui, InterventionData d, Transform parent)
        {
            var root = UIFactory.MakeRect("Kart_" + d.id, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190, 230));
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 190;
            le.preferredHeight = 230;
            UIFactory.Skin(root, ui.cardSprite, ui.cardSpriteMultiplier, ui.roundedSprite, new Color(1f, 0.97f, 0.9f), true);

            // Görsel alanı
            var iconRt = UIFactory.MakeRect("Ikon", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0, -22), new Vector2(150, 110));
            var icon = UIFactory.Img(iconRt, d.icon ? d.icon : ui.roundedSprite, d.icon ? Color.white : d.placeholderColor);
            icon.preserveAspect = d.icon != null;

            // İsim
            var nameRt = UIFactory.MakeRect("Isim", root, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(-34, 66));
            var nameTxt = UIFactory.Txt(nameRt, d.displayName, 28, UIFactory.Hex("#3E2A17"), TextAlignmentOptions.Center, true);
            UIFactory.AutoSize(nameTxt, 18, 28);

            // Maliyet rozeti
            var badge = UIFactory.MakeRect("Maliyet", root, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(-10, -10), new Vector2(64, 64));
            UIFactory.Skin(badge, ui.badgeSprite, 1f, ui.circleSprite, UIFactory.Hex("#2E9E5B"), false, true);
            var costTxt = UIFactory.Txt(UIFactory.Fill("Sayi", badge), d.cost < 0 ? "+" + (-d.cost) : d.cost.ToString(), d.cost < 0 ? 28 : 34,
                ui.badgeSprite ? UIFactory.Hex("#1F3D14") : Color.white, TextAlignmentOptions.Center, true);

            var card = root.gameObject.AddComponent<CardUI>();
            card.data = d;
            card.ui = ui;
            card.group = root.gameObject.AddComponent<CanvasGroup>();
            return card;
        }

        public void Refresh(int budget)
        {
            if (group) group.alpha = data.cost <= budget ? 1f : 0.45f;
        }

        public void OnPointerDown(PointerEventData e)
        {
            string price = data.cost < 0 ? (-data.cost) + " yaprak kazandırır" : data.cost + " yaprak";
            ui.ShowInfo("<b>" + data.displayName + "</b>  (" + price + ")  " + data.description);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.InPlay) return;

            ghost = UIFactory.MakeRect("Hayalet", ui.dragLayer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 130));
            var img = UIFactory.Img(ghost, data.icon ? data.icon : ui.roundedSprite, data.icon ? Color.white : data.placeholderColor);
            img.preserveAspect = data.icon != null;
            var cg = ghost.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.alpha = 0.9f;
            MoveGhost(e);

            foreach (var s in Slot.All)
            {
                bool valid = gm.IsInPresent && s.Current == null && data.CanGoTo(s.slotTag);
                s.SetHighlight(valid ? Slot.Highlight.Valid : Slot.Highlight.None);
            }
        }

        public void OnDrag(PointerEventData e)
        {
            if (ghost) MoveGhost(e);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!ghost) return;
            Destroy(ghost.gameObject);
            ghost = null;
            foreach (var s in Slot.All) s.SetHighlight(Slot.Highlight.None);

            var gm = GameManager.Instance;
            var cam = Camera.main;
            if (gm == null || cam == null) return;

            Vector3 world = cam.ScreenToWorldPoint(new Vector3(e.position.x, e.position.y, -cam.transform.position.z));
            var col = Physics2D.OverlapPoint(world);
            var slot = col ? col.GetComponentInParent<Slot>() : null;

            if (slot == null)
            {
                if (!gm.IsInPresent) ui.Toast("Değişiklik yapmak için önce kaydırıcıyı 2026'ya çek.", 2.5f);
                return;
            }
            if (!gm.TryPlace(data, slot, out var reason) && !string.IsNullOrEmpty(reason))
                ui.Toast(reason, 2.5f);
        }

        void MoveGhost(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(ui.dragLayer, e.position, null, out var local);
            ghost.localPosition = local;
        }
    }
}
