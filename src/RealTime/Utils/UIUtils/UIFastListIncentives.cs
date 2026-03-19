namespace RealTime.Utils.UIUtils
{
    using ColossalFramework.Globalization;
    using ColossalFramework.UI;
    using RealTime.Events.Containers;
    using RealTime.UI;
    using RealTime.Utils;
    using UnityEngine;

    public class UIFastListIncentives : UIPanel, IUIFastListRow
    {
        private UIPanel background;
        //private UILabel title;
        private UILabel effects;
        private UISlider amount;
        private UIHelper mainHelper;
        private UIPanel totalsPanel;
        private UILabel costsLabel;
        private UILabel returnsLabel;
        private UILabel costsReadout;
        private UILabel returnsReadout;
        private IncentiveOptionItem currentOption = null;

        public override void Start()
        {
            base.Start();

            Initialise();
        }

        public void Display(object data, bool isRowOdd)
        {
            if (data != null)
            {
                Initialise();

                if (data is IncentiveOptionItem option && option.title != null && option.title != "" && background != null)
                {
                    currentOption = option;

                    background.width = width;
                    background.height = height;
                    background.relativePosition = Vector2.zero;
                    background.zOrder = 0;

                    amount.isInteractive = true;
                    amount.value = option.sliderValue;
                    amount.maxValue = option.ticketCount;
                    amount.width = width - width * 40f / 100f;

                    var sliderPanel = amount.parent as UIPanel;
                    sliderPanel.relativePosition = new Vector2(5, 5);
                    sliderPanel.width = amount.width;

                    var sliderLabel = sliderPanel.Find<UILabel>("Label");
                    sliderLabel.tooltip = option.description;
                    sliderLabel.textScale = 0.8f;
                    sliderLabel.processMarkup = true;

                    totalsPanel.relativePosition = new Vector3(sliderPanel.relativePosition.x + sliderPanel.width + 5, 5);
                    totalsPanel.width = width - totalsPanel.relativePosition.x - 15;
                    totalsPanel.height = height - 10;
                    totalsPanel.atlas = TextureUtils.GetAtlas("Ingame");
                    totalsPanel.backgroundSprite = "GenericPanel";
                    totalsPanel.color = new Color32(91, 97, 106, 255);

                    effects.autoSize = false;
                    effects.autoHeight = false;
                    effects.width = totalsPanel.width;
                    effects.height = 20;
                    effects.relativePosition = new Vector2(0, totalsPanel.height - effects.height);
                    effects.textScale = 0.6f;
                    effects.padding = new RectOffset(5, 5, 5, 5);
                    effects.name = "Effects";
                    effects.processMarkup = true;
                    effects.text = string.Format("<sprite NotificationIconHappy> {0}%         <sprite NotificationIconNotHappy> {1}%", option.positiveEffect, option.negativeEffect);
                    effects.textAlignment = UIHorizontalAlignment.Center;
                    effects.verticalAlignment = UIVerticalAlignment.Middle;

                    costsLabel.relativePosition = Vector3.zero;
                    costsLabel.autoSize = false;
                    costsLabel.autoHeight = false;
                    costsLabel.width = 40;
                    costsLabel.height = totalsPanel.height / 2f - effects.height / 2f;
                    costsLabel.name = "CostsLabel";
                    costsLabel.textScale = 0.6f;
                    costsLabel.padding = new RectOffset(4, 4, 4, 4);
                    costsLabel.textAlignment = UIHorizontalAlignment.Left;
                    costsLabel.verticalAlignment = UIVerticalAlignment.Middle;
                    costsLabel.textColor = new Color32(255, 100, 100, 255);
                    costsLabel.color = new Color32(91, 97, 106, 255);

                    returnsLabel.relativePosition = new Vector3(0, totalsPanel.height / 2f - effects.height / 2f);
                    returnsLabel.autoSize = false;
                    returnsLabel.autoHeight = false;
                    returnsLabel.width = 40;
                    returnsLabel.height = totalsPanel.height / 2f - effects.height / 2f;
                    returnsLabel.name = "ReturnsLabel";
                    returnsLabel.textScale = 0.6f;
                    returnsLabel.padding = new RectOffset(4, 4, 4, 4);
                    returnsLabel.textAlignment = UIHorizontalAlignment.Left;
                    returnsLabel.verticalAlignment = UIVerticalAlignment.Middle;
                    returnsLabel.textColor = new Color32(206, 248, 0, 255);
                    returnsLabel.color = new Color32(91, 97, 106, 255);

                    costsReadout.relativePosition = costsLabel.relativePosition + new Vector3(costsLabel.width + 5, 1);
                    costsReadout.autoSize = false;
                    costsReadout.autoHeight = false;
                    costsReadout.width = totalsPanel.width - costsReadout.relativePosition.x - 5;
                    costsReadout.height = costsLabel.height - 2;
                    costsReadout.atlas = TextureUtils.GetAtlas("Ingame");
                    costsReadout.backgroundSprite = "TextFieldPanel";
                    costsReadout.name = "Cost";
                    costsReadout.textScale = 0.6f;
                    costsReadout.textAlignment = UIHorizontalAlignment.Right;
                    costsReadout.verticalAlignment = UIVerticalAlignment.Middle;
                    costsReadout.textColor = new Color32(238, 95, 0, 255);
                    costsReadout.color = new Color32(45, 52, 61, 255);

                    returnsReadout.relativePosition = returnsLabel.relativePosition + new Vector3(returnsLabel.width + 5, 1);
                    returnsReadout.autoSize = false;
                    returnsReadout.autoHeight = false;
                    returnsReadout.width = totalsPanel.width - returnsReadout.relativePosition.x - 5;
                    returnsReadout.height = returnsLabel.height - 2;
                    returnsReadout.atlas = TextureUtils.GetAtlas("Ingame");
                    returnsReadout.backgroundSprite = "TextFieldPanel";
                    returnsReadout.name = "Returns";
                    returnsReadout.textScale = 0.6f;
                    returnsReadout.textAlignment = UIHorizontalAlignment.Right;
                    returnsReadout.verticalAlignment = UIVerticalAlignment.Middle;
                    returnsReadout.textColor = new Color32(151, 238, 0, 255);
                    returnsReadout.color = new Color32(45, 52, 61, 255);

                    option.OnTicketSizeChanged += Option_OnTicketSizeChanged;

                    /*title.name = option.title;
                    title.text = option.title;
                    title.autoSize = false;
                    title.relativePosition = new Vector2(0, 0);
                    title.width = width - ((width * 40) / 100);
                    title.height = sliderPanel.relativePosition.y;
                    title.textScale = 1f;
                    title.padding = new RectOffset(5, 5, 5, 5);
                    title.tooltip = option.description;*/

                    Deselect(isRowOdd);
                    UpdateTotals();
                    UpdateVariableStrings();
                    TranslationOnLanguageChanged();
                }
            }
        }

        private void Initialise()
        {
            if (mainHelper == null)
            {
                isVisible = true;
                canFocus = true;
                width = parent.width;
                height = 76;

                mainHelper = new UIHelper(this);
                background = AddUIComponent<UIPanel>();
                totalsPanel = AddUIComponent<UIPanel>();
                effects = totalsPanel.AddUIComponent<UILabel>();
                costsLabel = totalsPanel.AddUIComponent<UILabel>();
                returnsLabel = totalsPanel.AddUIComponent<UILabel>();
                costsReadout = totalsPanel.AddUIComponent<UILabel>();
                returnsReadout = totalsPanel.AddUIComponent<UILabel>();
                //title = AddUIComponent<UILabel>();

                amount = mainHelper.AddSlider(" ", 0, 100, 10, 0, delegate (float val)
                {
                    if (currentOption != null)
                    {
                        currentOption.sliderValue = val;

                        UpdateTotals();
                        currentOption.UpdateItemChanged();
                    }
                }) as UISlider;
                TranslationOnLanguageChanged();
            }
        }

        public void Select(bool isRowOdd)
        {
            if (background != null)
            {
                /*background.backgroundSprite = "ListItemHighlight";
                background.color = new Color32(255, 255, 255, 255);*/
            }
        }

        public void Deselect(bool isRowOdd)
        {
            if (background != null)
            {
                background.backgroundSprite = "GenericPanel";
                background.color = new Color32(255, 255, 255, 128);
            }
        }

        private void UpdateTotals()
        {
            if (currentOption != null)
            {
                float totalCosts = currentOption.sliderValue * currentOption.cost;
                float toralReturns = currentOption.sliderValue * currentOption.returnCost;

                var sliderPanel = amount.parent as UIPanel;
                var sliderLabel = sliderPanel.Find<UILabel>("Label");

                costsReadout.text = totalCosts.ToString(Settings.moneyFormat, LocaleManager.cultureInfo);
                returnsReadout.text = toralReturns.ToString(Settings.moneyFormat, LocaleManager.cultureInfo);
                sliderLabel.text = "<color#90a0b0>" + currentOption.sliderValue + "</color>   " + currentOption.title;

                currentOption.UpdateItemChanged();
            }
        }

        private void UpdateVariableStrings()
        {
            if (currentOption != null && effects != null)
            {
                effects.tooltip = string.Format("TranslationKeys.EventTooltipItemImpact", currentOption.positiveEffect, currentOption.negativeEffect);
            }
        }

        protected override void OnClick(UIMouseEventParameter p) => base.OnClick(p);

        private void Option_OnTicketSizeChanged()
        {
            if (currentOption != null)
            {
                float newValue = amount.value + 10;
                amount.value = newValue;
                amount.maxValue = currentOption.ticketCount;
                amount.value = Mathf.Min(newValue - 10, amount.maxValue);
                currentOption.sliderValue = amount.value;

                amount.Invalidate();

                UpdateTotals();
            }
        }

        private void TranslationOnLanguageChanged()
        {
            var lp = UserEventCreationWindow.localizationProvider; // or UserEventCreationWindow.localizationProvider

            if (lp == null)
            {
                return; // safe no-op if localization not ready
            }

            if (costsLabel != null)
            {
                costsLabel.text = "BUY";      // new key
                costsLabel.tooltip = "BUY_TT";
                costsLabel.Invalidate();   // instead of .Update()
            }

            if (returnsLabel != null)
            {
                returnsLabel.text = "SELL";
                returnsLabel.tooltip = "SELL_TT";
                returnsLabel.Invalidate();
            }
        }
    }
}
