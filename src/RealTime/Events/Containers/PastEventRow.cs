namespace RealTime.Events.Containers
{
    using ColossalFramework.Globalization;
    using ColossalFramework.UI;
    using SkyTools.Localization;
    using UnityEngine;

    // =========================================================================
    // PastEventRow  —  460×48 row with date, name, tickets sold, revenue, profit
    // =========================================================================

    internal sealed class PastEventRow
    {
        public UIPanel Root { get; }

        private PastEventRow(UIPanel root) { Root = root; }

        public static PastEventRow Create(UIScrollablePanel parent, RealTimeCityEvent ev, ILocalizationProvider loc)
        {
            var row = parent.AddUIComponent<UIPanel>();
            row.name = "PastEventRow";
            row.size = new Vector2(460f, 48f);
            row.backgroundSprite = "GenericPanelDark";

            var lblDate = row.AddUIComponent<UILabel>();
            lblDate.name = "LabelDate";
            lblDate.textColor = new Color32(180, 180, 180, 255);
            lblDate.textScale = 0.7f;
            lblDate.relativePosition = new Vector3(8f, 4f);
            lblDate.text = ev.StartTime.ToString("dd/MM/yyyy");

            var lblName = row.AddUIComponent<UILabel>();
            lblName.name = "LabelEventName";
            lblName.textColor = Color.white;
            lblName.textScale = 0.8f;
            lblName.relativePosition = new Vector3(90f, 4f);
            lblName.text = ev.EventName;

            var lblTicketsSold = row.AddUIComponent<UILabel>();
            lblTicketsSold.name = "LabelTicketsSold";
            lblTicketsSold.textColor = new Color32(180, 180, 180, 255);
            lblTicketsSold.textScale = 0.7f;
            lblTicketsSold.relativePosition = new Vector3(8f, 26f);
            lblTicketsSold.text = $"{ev.AttendeesCount}/{ev.UserTicketCount}";

            var lblRevenue = row.AddUIComponent<UILabel>();
            lblRevenue.name = "LabelRevenue";
            lblRevenue.textColor = Color.white;
            lblRevenue.textScale = 0.7f;
            lblRevenue.relativePosition = new Vector3(100f, 26f);
            lblRevenue.text = ev.Revenue.ToString(Settings.moneyFormat, LocaleManager.cultureInfo);

            bool inProfit = ev.Profit >= 0;
            var lblProfit = row.AddUIComponent<UILabel>();
            lblProfit.name = "LabelProfit";
            lblProfit.textColor = inProfit
                ? new Color32(100, 220, 100, 255)
                : new Color32(220, 80, 80, 255);
            lblProfit.textScale = 0.7f;
            lblProfit.relativePosition = new Vector3(310f, 26f);
            lblProfit.text = (inProfit ? "+" : "") + ev.Profit.ToString(Settings.moneyFormat, LocaleManager.cultureInfo);

            return new PastEventRow(row);
        }
    }
}
