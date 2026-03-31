namespace RealTime.Events.Containers
{
    using System;
    using ColossalFramework.UI;
    using RealTime.Localization;
    using SkyTools.Localization;
    using UnityEngine;

    // =========================================================================
    // UpcomingEventRow  —  460×48 row with date, name, tickets, cost + remove btn
    // =========================================================================

    internal sealed class UpcomingEventRow
    {
        public UIPanel Root { get; }
        public event Action OnRemoveClicked;

        private UpcomingEventRow(UIPanel root) { Root = root; }

        public static UpcomingEventRow Create(UIScrollablePanel parent, RealTimeCityEvent ev, ILocalizationProvider loc)
        {
            var row = parent.AddUIComponent<UIPanel>();
            row.name = "UpcomingEventRow";
            row.size = new Vector2(460f, 48f);
            row.backgroundSprite = "GenericPanelDark";

            var lblDate = row.AddUIComponent<UILabel>();
            lblDate.name = "LabelDate";
            lblDate.textColor = Color.white;
            lblDate.textScale = 0.75f;
            lblDate.relativePosition = new Vector3(8f, 4f);
            lblDate.text = ev.StartTime.ToString("MMM dd\nHH:mm");

            var lblName = row.AddUIComponent<UILabel>();
            lblName.name = "LabelEventName";
            lblName.textColor = Color.white;
            lblName.textScale = 0.8f;
            lblName.relativePosition = new Vector3(90f, 4f);
            lblName.text = ev.UserEventName ?? ev.EventName ?? "Event";

            var lblTickets = row.AddUIComponent<UILabel>();
            lblTickets.name = "LabelTickets";
            lblTickets.textColor = new Color32(180, 180, 180, 255);
            lblTickets.textScale = 0.7f;
            lblTickets.relativePosition = new Vector3(8f, 28f);
            lblTickets.text = loc.Translate(TranslationKeys.VanillaEventTicketSliderLabel) + ": " + ev.UserTicketCount;

            var lblCost = row.AddUIComponent<UILabel>();
            lblCost.name = "LabelCost";
            lblCost.textColor = new Color32(255, 180, 0, 255);
            lblCost.textScale = 0.7f;
            lblCost.relativePosition = new Vector3(160f, 28f);

            var btnRemove = row.AddUIComponent<UIButton>();
            btnRemove.name = "ButtonRemove";
            btnRemove.text = loc.Translate(TranslationKeys.VanillaEventUpcomingHideBtn);
            btnRemove.size = new Vector2(70f, 22f);
            btnRemove.relativePosition = new Vector3(382f, 13f);
            btnRemove.normalBgSprite = "ButtonMenu";
            btnRemove.hoveredBgSprite = "ButtonMenuHovered";
            btnRemove.pressedBgSprite = "ButtonMenuPressed";
            btnRemove.textColor = Color.white;
            btnRemove.textScale = 0.8f;

            var result = new UpcomingEventRow(row);
            btnRemove.eventClicked += (c, e) => result.OnRemoveClicked?.Invoke();
            return result;
        }
    }
}
