namespace RealTime.UI
{
    using System;
    using System.Collections.Generic;
    using ColossalFramework;
    using ColossalFramework.Globalization;
    using ColossalFramework.UI;
    using ICities;
    using RealTime.Events;
    using RealTime.Events.Containers;
    using RealTime.Events.Storage;
    using RealTime.Simulation;
    using RealTime.Utils;
    using RealTime.Utils.UIUtils;
    using SkyTools.Localization;
    using SkyTools.Tools;
    using UnityEngine;

    internal class UserEventCreationWindow : UIPanel
    {
        protected CityEventTemplate template = null;
        protected UIHelper _helper = null;
        protected UITitleBar _titleBar = null;
        protected UILabel _informationLabel = null;
        protected UISlider _ticketSlider = null;
        protected UIPanel _totalPanel = null;
        protected UILabel _totalAmountLabel = null;
        protected UILabel _costLabel = null;
        protected UILabel _totalIncomeLabel = null;
        protected UILabel _incomeLabel = null;
        protected UIFastList _incentiveList = null;
        protected UIButton _createButton = null;
        protected UISlider _startTimeSlider = null;
        protected UISlider _startDaySlider = null;

        public static ILocalizationProvider localizationProvider;
        public static ITimeInfo TimeInfo;

        protected float totalCost = 0f;
        protected float maxIncome = 0f;
        protected ushort eventBuildingID = 0;

        private const float one_over_twelve = 0.08333333333333333f; // This is just 1/12 because * is (usually) faster than /

        public string title
        {
            set
            {
                if (_titleBar)
                {
                    _titleBar.title = value;
                }
            }

            get => _titleBar ? _titleBar.title : "";
        }

        public override void Awake()
        {
            Initialise();

            base.Awake();
        }

        public override void Start()
        {
            base.Start();

            Initialise();

            atlas = TextureUtils.GetAtlas("Ingame");
            backgroundSprite = "MenuPanel2";

            _titleBar.name = "TitleBar";
            _titleBar.relativePosition = new Vector3(0, 0);
            _titleBar.width = width;

            _informationLabel.width = width;
            _informationLabel.padding = new RectOffset(5, 5, 5, 5);
            _informationLabel.autoHeight = true;
            _informationLabel.processMarkup = true;
            _informationLabel.wordWrap = true;
            _informationLabel.textScale = 0.7f;

            _ticketSlider.eventValueChanged += TicketSlider_eventValueChanged;

            var sliderPanel = _ticketSlider.parent as UIPanel;
            sliderPanel.name = "TicketSliderPanel";

            var sliderLabel = sliderPanel.Find<UILabel>("Label");
            sliderLabel.textScale = 0.8f;

            _totalPanel.atlas = TextureUtils.GetAtlas("Ingame");
            _totalPanel.backgroundSprite = "GenericPanel";
            _totalPanel.color = new Color32(91, 97, 106, 255);
            _totalPanel.name = "Totals";

            _totalAmountLabel.autoSize = false;
            _totalAmountLabel.autoHeight = false;
            _totalAmountLabel.name = "TotalLabel";
            _totalAmountLabel.padding = new RectOffset(4, 4, 4, 4);
            _totalAmountLabel.textAlignment = UIHorizontalAlignment.Left;
            _totalAmountLabel.verticalAlignment = UIVerticalAlignment.Middle;
            _totalAmountLabel.textColor = new Color32(255, 100, 100, 255);
            _totalAmountLabel.color = new Color32(91, 97, 106, 255);
            _totalAmountLabel.textScale = 0.7f;

            _totalIncomeLabel.autoSize = false;
            _totalIncomeLabel.autoHeight = false;
            _totalIncomeLabel.name = "TotalIncome";
            _totalIncomeLabel.padding = new RectOffset(4, 4, 4, 4);
            _totalIncomeLabel.textAlignment = UIHorizontalAlignment.Left;
            _totalIncomeLabel.verticalAlignment = UIVerticalAlignment.Middle;
            _totalIncomeLabel.textColor = new Color32(206, 248, 0, 255);
            _totalIncomeLabel.color = new Color32(91, 97, 106, 255);
            _totalIncomeLabel.textScale = 0.7f;

            _costLabel.autoSize = false;
            _costLabel.autoHeight = false;
            _costLabel.atlas = TextureUtils.GetAtlas("Ingame");
            _costLabel.backgroundSprite = "TextFieldPanel";
            _costLabel.name = "Cost";
            _costLabel.padding = new RectOffset(4, 4, 2, 2);
            _costLabel.textAlignment = UIHorizontalAlignment.Right;
            _costLabel.verticalAlignment = UIVerticalAlignment.Middle;
            _costLabel.textColor = new Color32(238, 95, 0, 255);
            _costLabel.color = new Color32(45, 52, 61, 255);
            _costLabel.textScale = 0.7f;

            _incomeLabel.autoSize = false;
            _incomeLabel.autoHeight = false;
            _incomeLabel.atlas = TextureUtils.GetAtlas("Ingame");
            _incomeLabel.backgroundSprite = "TextFieldPanel";
            _incomeLabel.name = "Income";
            _incomeLabel.padding = new RectOffset(4, 4, 2, 2);
            _incomeLabel.textAlignment = UIHorizontalAlignment.Right;
            _incomeLabel.verticalAlignment = UIVerticalAlignment.Middle;
            _incomeLabel.textColor = new Color32(151, 238, 0, 255);
            _incomeLabel.color = new Color32(45, 52, 61, 255);
            _incomeLabel.textScale = 0.7f;

            _incentiveList.canSelect = false;
            _incentiveList.name = "IncentiveSelectionList";
            _incentiveList.backgroundSprite = "UnlockingPanel";
            _incentiveList.rowHeight = 76f;
            _incentiveList.rowsData.Clear();
            _incentiveList.selectedIndex = -1;

            sliderPanel = _startTimeSlider.parent as UIPanel;
            sliderLabel = sliderPanel.Find<UILabel>("Label");
            sliderLabel.textScale = 0.8f;

            sliderPanel = _startDaySlider.parent as UIPanel;
            sliderLabel = sliderPanel.Find<UILabel>("Label");
            sliderLabel.textScale = 0.8f;

            _createButton.textScale = 0.9f;
            _createButton.anchor = UIAnchorStyle.Right | UIAnchorStyle.Bottom;

            TranslationOnLanguageChanged();
            PerformLayout();
        }

        private void Initialise()
        {
            if (_helper == null)
            {
                width = 400;
                height = 500;
                isInteractive = true;
                enabled = true;

                _helper = new UIHelper(this);
                _titleBar = AddUIComponent<UITitleBar>();
                _informationLabel = AddUIComponent<UILabel>();
                _totalPanel = AddUIComponent<UIPanel>();
                _totalAmountLabel = _totalPanel.AddUIComponent<UILabel>();
                _totalIncomeLabel = _totalPanel.AddUIComponent<UILabel>();
                _costLabel = _totalPanel.AddUIComponent<UILabel>();
                _incomeLabel = _totalPanel.AddUIComponent<UILabel>();
                _incentiveList = UIFastList.Create<UIFastListIncentives>(this);

                _ticketSlider = _helper.AddSlider("Tickets", 100, 9000, 10, 500, delegate (float value)
                {
                    if (_incentiveList != null)
                    {
                        var optionItems = _incentiveList.rowsData;

                        foreach (IncentiveOptionItem optionItemObject in optionItems)
                        {
                            optionItemObject.ticketCount = value;
                            optionItemObject.UpdateTicketSize();
                        }
                    }
                }) as UISlider;

                _startDaySlider = _helper.AddSlider("Days", 0, 7, 1, 0, delegate (float value)
                {
                    UpdateTotalCost();
                }) as UISlider;

                _startTimeSlider = _helper.AddSlider("Start Hour", 0f, 24f, 0.25f, 12f, delegate (float value)
                {
                    UpdateTotalCost();
                }) as UISlider;

                _startTimeSlider.eventValueChanged += Slider_eventValueChanged;

                _createButton = _helper.AddButton("Create", new OnButtonClicked(CreateEvent)) as UIButton;

            }
        }

        private void Slider_eventValueChanged(UIComponent component, float value)
        {
            var slider = (UISlider)component;

            slider.value = value;
            slider.tooltip = getTimeFromFloatingValue(slider.value);

            try
            {
                slider.tooltipBox.Show();
                slider.RefreshTooltip();
            }
            catch
            {
                //This is just here because it'll error out when the game fist starts otherwise as the tooltip doesn't exist.
            }
        }

        public static string getTimeFromFloatingValue(float value)
        {
            float displayedValue = value % 12; // Wrap military time into civilian time
            if (displayedValue < 1f)
            {
                displayedValue += 12f; // Instead of 0 let's show 12 even for am
            }
            int hours = (int)displayedValue;
            string minutes = string.Format("{0:00}", (int)(displayedValue % 1f * 60f));
            string suffix = (value * one_over_twelve > 1) ? "pm" : "am";

            return hours.ToString() + ':' + minutes.ToString() + ' ' + suffix;
        }

        public void SetUp(LabelOptionItem selectedData, ushort buildingID)
        {
            template = selectedData.linkedTemplate;

            if (template != null && buildingID != 0 && _ticketSlider != null)
            {
                eventBuildingID = buildingID;
                _ticketSlider.maxValue = template.Capacity;
                _ticketSlider.minValue = Mathf.Min(template.Capacity, 100);
                _ticketSlider.value = _ticketSlider.minValue;

                title = template.UserEventName;

                var incentives = template.Incentives;
                _incentiveList.rowsData.Clear();

                if( incentives != null )
                {
                    foreach (var incentive in incentives)
                    {
                        var optionItem = new IncentiveOptionItem()
                        {
                            cost = incentive.Cost,
                            description = incentive.Description,
                            negativeEffect = incentive.NegativeEffect,
                            positiveEffect = incentive.PositiveEffect,
                            returnCost = incentive.ReturnCost,
                            title = incentive.Name,
                            ticketCount = _ticketSlider.value
                        };
                        optionItem.OnOptionItemChanged += OptionItem_OnOptionItemChanged;

                        _incentiveList.rowsData.Add(optionItem);
                    }

                    try
                    {
                        _incentiveList.DisplayAt(0);
                        _incentiveList.selectedIndex = 0;
                        _incentiveList.Show();
                    }
                    catch
                    {
                        Log.Error("IncentiveList DisplayAt hit an error. Probably too few items in the list.");
                    }

                    _incentiveList.Refresh();
                }

                string tooltip = $"Males: {template.Attendees.Males}%\n" + $"Females: {template.Attendees.Females}%\n" + $"Young: {template.Attendees.YoungAdults}%";

                _informationLabel.tooltip = localizationProvider.Translate("RUSH_EVENT_DEMOS") + "\n" + tooltip;

                UpdateTotalCost();
                PerformLayout();

                Log.Info($"Event capacity: {template.Capacity}");
            }
        }

        public override void PerformLayout()
        {
            base.PerformLayout();

            _titleBar.width = width;

            _informationLabel.width = width;
            _informationLabel.relativePosition = new Vector3(0, _titleBar.height + 10);

            _ticketSlider.width = width - width * 60f / 100f;

            var sliderPanel = _ticketSlider.parent as UIPanel;
            sliderPanel.width = _ticketSlider.width;
            sliderPanel.relativePosition = new Vector3(10, _informationLabel.relativePosition.y + _informationLabel.height + 5);

            var sliderLabel = sliderPanel.Find<UILabel>("Label");
            sliderLabel.width = _ticketSlider.width;

            _totalPanel.relativePosition = sliderPanel.relativePosition + new Vector3(sliderPanel.width + 10, 0);
            _totalPanel.width = width - sliderPanel.width - 30;
            _totalPanel.height = sliderPanel.height;

            _totalAmountLabel.relativePosition = Vector3.zero;
            _totalAmountLabel.width = 110;
            _totalAmountLabel.height = _totalPanel.height / 2f;

            _totalIncomeLabel.relativePosition = _totalAmountLabel.relativePosition + new Vector3(0, _totalAmountLabel.height);
            _totalIncomeLabel.width = 110;
            _totalIncomeLabel.height = _totalPanel.height / 2f;

            _costLabel.relativePosition = _totalAmountLabel.relativePosition + new Vector3(_totalAmountLabel.width, 4);
            _costLabel.width = _totalPanel.width - _totalAmountLabel.width - 4;
            _costLabel.height = _totalAmountLabel.height - 8;

            _incomeLabel.relativePosition = _totalIncomeLabel.relativePosition + new Vector3(_totalIncomeLabel.width, 4);
            _incomeLabel.width = _totalPanel.width - _totalIncomeLabel.width - 4;
            _incomeLabel.height = _totalIncomeLabel.height - 8;

            _incentiveList.relativePosition = sliderPanel.relativePosition + new Vector3(0, sliderPanel.height + 10);
            _incentiveList.width = width - 20;

            _createButton.height = sliderPanel.height;

            _incentiveList.height = height - _incentiveList.relativePosition.y - 20 - _createButton.height;

            float availableWidth = width - _createButton.width;

            _createButton.relativePosition = new Vector3(width - _createButton.width - 10, height - _createButton.height - 10);

            _startTimeSlider.width = availableWidth / 2f - 10;

            var startTimeSliderPanel = _startTimeSlider.parent as UIPanel;
            startTimeSliderPanel.width = _startTimeSlider.width;
            startTimeSliderPanel.relativePosition = new Vector3(10, _createButton.relativePosition.y - 7f);

            sliderLabel = startTimeSliderPanel.Find<UILabel>("Label");
            sliderLabel.width = _startTimeSlider.width;

            _startDaySlider.width = availableWidth / 2f - 35;

            var startDaySliderPanel = _startDaySlider.parent as UIPanel;
            startDaySliderPanel.width = _startDaySlider.width;
            startDaySliderPanel.relativePosition = new Vector3(startTimeSliderPanel.relativePosition.x + startTimeSliderPanel.width + 5, _createButton.relativePosition.y - 7f);

            sliderLabel = startTimeSliderPanel.Find<UILabel>("Label");
            sliderLabel.width = _startDaySlider.width;

            _incentiveList.DisplayAt(0);
        }

        public override void Update()
        {
            if (_startTimeSlider != null)
            {
                if (_startTimeSlider.value <= TimeInfo.Now.Hour)
                {
                    _startDaySlider.value = Mathf.Max(1f, _startDaySlider.value);
                }
            }
        }

        private void OptionItem_OnOptionItemChanged() => UpdateTotalCost();

        private void TicketSlider_eventValueChanged(UIComponent component, float value) => UpdateTotalCost();

        private void UpdateTotalCost()
        {
            if (_startTimeSlider != null)
            {
                var sliderPanel = _startTimeSlider.parent as UIPanel;
                var sliderLabel = sliderPanel.Find<UILabel>("Label");
                sliderLabel.text = string.Format("Start at {0}", getTimeFromFloatingValue(_startTimeSlider.value));
            }

            if (_startDaySlider != null)
            {
                var sliderPanel = _startDaySlider.parent as UIPanel;
                var sliderLabel = sliderPanel.Find<UILabel>("Label");
                sliderLabel.text = string.Format("Starts in {0} day(s)", _startDaySlider.value);
            }

            if (_ticketSlider != null)
            {
                var sliderPanel = _ticketSlider.parent as UIPanel;
                var sliderLabel = sliderPanel.Find<UILabel>("Label");
                sliderLabel.text = string.Format("{0} tickets", _ticketSlider.value);
            }

            if (template != null && template.Costs != null)
            {
                totalCost = 0f;
                maxIncome = 0f;

                totalCost += template.Costs.Creation;
                totalCost += _ticketSlider.value * template.Costs.PerHead;

                maxIncome += _ticketSlider.value * template.Costs.Entry;

                foreach (IncentiveOptionItem item in _incentiveList.rowsData)
                {
                    totalCost += item.cost * item.sliderValue;
                    maxIncome += item.returnCost * item.sliderValue;
                }
            }

            if (_costLabel != null)
            {
                _costLabel.text = totalCost.ToString(Settings.moneyFormat, LocaleManager.cultureInfo);
                _incomeLabel.text = maxIncome.ToString(Settings.moneyFormat, LocaleManager.cultureInfo);
            }

            if (_createButton != null)
            {
                int adjustedCost = Mathf.RoundToInt(totalCost * 100f);
                if (Singleton<EconomyManager>.instance.PeekResource(EconomyManager.Resource.Construction, adjustedCost) != adjustedCost)
                {
                    _createButton.Disable();
                }
                else
                {
                    _createButton.Enable();
                }
            }
        }

        private void CreateEvent()
        {
            if (template != null)
            {
                // Incentives
                var optionItems = GetIncentiveItems();

                int year = SimulationManager.instance.m_currentGameTime.Year;
                int month = SimulationManager.instance.m_currentGameTime.Month;
                int day = SimulationManager.instance.m_currentGameTime.Day;

                var startTime = new DateTime(year, month, day, 0, 0, 0);

                startTime = startTime.AddDays((int)_startDaySlider.value).AddHours(_startTimeSlider.value);

                // Real Time event
                var rtEvent = new RealTimeCityEvent(template, Mathf.RoundToInt(_ticketSlider.value));

                // Copy incentives (optional—Real Time skips now)
                foreach (var item in optionItems)
                {
                    rtEvent.AddIncentive(item.title, item.sliderValue, item.cost);  // If needed
                }

                // Add (Real Time manager)
                SimulationHandler.EventManager.AddEvent(rtEvent, startTime);  // ← No clash check
                Hide();

                Log.Info($"Created {template.EventName} for {eventBuildingID}");
            }
        }

        private List<IncentiveOptionItem> GetIncentiveItems()
        {
            var optionItems = new List<IncentiveOptionItem>();
            for (int i = 0; i < _incentiveList.rowsData.m_size; i++)
            {
                if (_incentiveList.rowsData.m_buffer[i] is IncentiveOptionItem item)
                {
                    optionItems.Add(item);
                }
            }
            return optionItems;
        }

        private void TranslationOnLanguageChanged()
        {
            _totalAmountLabel.text = "Event cost";
            _totalAmountLabel.tooltip = "The total cost to create this event.";
            _totalIncomeLabel.text = "Max profit";
            _totalIncomeLabel.tooltip = "The maximum profits you can receive if the event is 100% successful and all incentives are used.";
            _createButton.text = "Create";
            _createButton.tooltip = "Create the event.";

            var sliderPanel = _ticketSlider.parent as UIPanel;
            var sliderLabel = sliderPanel.Find<UILabel>("Label");
            sliderLabel.tooltip = "Increase or decrease the number of tickets available for this venue.";

            _totalAmountLabel.Invalidate();
            _totalIncomeLabel.Invalidate();
            _createButton.Invalidate();

            UpdateTotalCost();
            PerformLayout();
        }
    }
}
