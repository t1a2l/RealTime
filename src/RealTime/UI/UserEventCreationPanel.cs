namespace RealTime.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ColossalFramework;
    using ColossalFramework.Globalization;
    using ColossalFramework.UI;
    using ICities;
    using RealTime.Config;
    using RealTime.Events;
    using RealTime.Events.Containers;
    using RealTime.Events.Storage;
    using RealTime.Localization;
    using RealTime.Simulation;
    using RealTime.Utils;
    using RealTime.Utils.UIUtils;
    using SkyTools.Localization;
    using SkyTools.Tools;
    using UnityEngine;

    internal class UserEventCreationPanel : UIPanel
    {
        internal CityEventTemplate template = null;
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

        protected UIDropDown _startDayDropDown = null;
        protected UIDropDown _startMonthDropDown = null;
        protected UIDropDown _startHourDropDown = null;
        protected UIDropDown _startMinuteDropDown = null;

        public static RealTimeConfig RealTimeConfig;
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

            var dropdownPanel = _startDayDropDown.parent as UIPanel;
            var dropdownLabel = dropdownPanel.Find<UILabel>("Label");
            dropdownLabel.textScale = 0.8f;

            dropdownPanel = _startMonthDropDown.parent as UIPanel;
            dropdownLabel = sliderPanel.Find<UILabel>("Label");
            dropdownLabel.textScale = 0.8f;

            dropdownPanel = _startHourDropDown.parent as UIPanel;
            dropdownLabel = sliderPanel.Find<UILabel>("Label");
            dropdownLabel.textScale = 0.8f;

            dropdownPanel = _startMinuteDropDown.parent as UIPanel;
            dropdownLabel = sliderPanel.Find<UILabel>("Label");
            dropdownLabel.textScale = 0.8f;

            _createButton.textScale = 0.9f;
            _createButton.anchor = UIAnchorStyle.Right | UIAnchorStyle.Bottom;

            TranslationOnLanguageChanged();
            PerformLayout();

            isVisible = false;
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

                _ticketSlider = (UISlider)_helper.AddSlider("Tickets", 100, 9000, 10, 500, delegate (float value)
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
                });

                _startDayDropDown = (UIDropDown)_helper.AddDropdown("Day", [], 0, null);
                _startMonthDropDown = (UIDropDown)_helper.AddDropdown("Month", [], 0, null);
                _startHourDropDown = (UIDropDown)_helper.AddDropdown("Hour", [], 0, null);
                _startMinuteDropDown = (UIDropDown)_helper.AddDropdown("Minute", [], 0, null);

                _startDayDropDown.items = [.. Enumerable.Range(0, 32).Select(i => i.ToString("D2"))];
                _startMonthDropDown.items = [.. Enumerable.Range(0, 13).Select(i => i.ToString("D2"))];
                _startHourDropDown.items = [.. Enumerable.Range(0, 24).Select(i => i.ToString("D2"))];
                _startMinuteDropDown.items = [.. Enumerable.Range(0, 60).Select(i => i.ToString("D2"))];

                _startDayDropDown.selectedIndex = Singleton<SimulationManager>.instance.m_currentGameTime.Day - 1;
                _startMonthDropDown.selectedIndex = Singleton<SimulationManager>.instance.m_currentGameTime.Month - 1;
                _startHourDropDown.selectedIndex = Singleton<SimulationManager>.instance.m_currentGameTime.Hour;
                _startMinuteDropDown.selectedIndex = Singleton<SimulationManager>.instance.m_currentGameTime.Minute;

                _startDayDropDown.eventSelectedIndexChanged += delegate (UIComponent uiComponent, int value)
                {
                    OnScheduleDayChanged(uiComponent, value);
                    UpdateTotalCost();
                };

                _startMonthDropDown.eventSelectedIndexChanged += delegate (UIComponent uiComponent, int value)
                {
                    OnScheduleMonthChanged(uiComponent, value);
                    UpdateTotalCost();
                };

                _startHourDropDown.eventSelectedIndexChanged += delegate (UIComponent uiComponent, int value)
                {
                    OnScheduleHourChanged(uiComponent, value);
                    UpdateTotalCost();
                };

                _startMinuteDropDown.eventSelectedIndexChanged += delegate (UIComponent uiComponent, int value)
                {
                    OnScheduleMinuteChanged(uiComponent, value);
                    UpdateTotalCost();
                };

                _createButton = _helper.AddButton("Create", new OnButtonClicked(CreateEvent)) as UIButton;

            }
        }

        private void OnScheduleDayChanged(UIComponent uiComponent, int value)
        {
            var dropdown = (UIDropDown)uiComponent;

            if(dropdown == null && dropdown.selectedIndex != value)
            {
                int startMonth = byte.Parse(_startMonthDropDown.selectedValue);
                int num = DateTime.DaysInMonth(2, startMonth + 1);
                bool flag = value + 1 > num;
                if (flag)
                {
                    value = (byte)(num - 1);
                }
                if (flag)
                {
                    dropdown.selectedIndex = value;
                }
            }
        }

        private void OnScheduleMonthChanged(UIComponent uiComponent, int value)
        {
            var dropdown = (UIDropDown)uiComponent;

            if (dropdown == null && dropdown.selectedIndex != value)
            {
                int startDay = byte.Parse(_startDayDropDown.selectedValue);
                int num = DateTime.DaysInMonth(2, value + 1);
                if (startDay > num)
                {
                    dropdown.selectedIndex = num - 1;
                }
            }
        }

        private void OnScheduleHourChanged(UIComponent uiComponent, int value)
        {
            var dropdown = (UIDropDown)uiComponent;

            int startDay = byte.Parse(_startDayDropDown.selectedValue) + 1;
            int startMonth = byte.Parse(_startMonthDropDown.selectedValue) + 1;
            int startMinute = byte.Parse(_startMinuteDropDown.selectedValue) + 1;

            int year = Singleton<SimulationManager>.instance.m_currentGameTime.Year;

            if (dropdown == null && dropdown.selectedIndex != value)
            {
                string hourText = dropdown.items[value];
                int Hour = byte.Parse(hourText) + 1;

                var startDateTime = new DateTime(year, startMonth, startDay, Hour, startMinute, 0);
                dropdown.selectedIndex = (byte)AdjustEventStartTime(startDateTime) - 1;
            }
        }

        private void OnScheduleMinuteChanged(UIComponent uiComponent, int value)
        {
            var dropdown = (UIDropDown)uiComponent;
            if (dropdown == null && dropdown.selectedIndex != value)
            {
                dropdown.selectedIndex = value;
            }
        }

        private static string getTimeFromFloatingValue(float value)
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

        private static float AdjustEventStartTime(DateTime eventStartTime)
        {
            var result = eventStartTime;

            float earliestHour;
            float latestHour;
            if (RealTimeConfig.IsWeekendEnabled && result.IsWeekend())
            {
                earliestHour = RealTimeConfig.EarliestHourEventStartWeekend;
                latestHour = RealTimeConfig.LatestHourEventStartWeekend;
            }
            else
            {
                earliestHour = RealTimeConfig.EarliestHourEventStartWeekday;
                latestHour = RealTimeConfig.LatestHourEventStartWeekday;
            }

            return result.Hour >= latestHour ? latestHour : result.Hour < earliestHour ? earliestHour : result.Hour;
        }

        public void SetUp(CityEventTemplate selectedTemplate, ushort buildingID)
        {
            template = selectedTemplate;

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

            _createButton.relativePosition = new Vector3(width - _createButton.width - 10, height - _createButton.height - 10);

            _startDayDropDown.width = 60f;
            _startMonthDropDown.width = 60f;
            _startHourDropDown.width = 60f;
            _startMinuteDropDown.width = 60f;

            var startDayDropDownPanel = _startDayDropDown.parent as UIPanel;
            startDayDropDownPanel.width = _startDayDropDown.width;
            startDayDropDownPanel.relativePosition = new Vector3(10, _createButton.relativePosition.y - 7f);

            var dropdownLabel = startDayDropDownPanel.Find<UILabel>("Label");
            dropdownLabel.width = _startDayDropDown.width;

            var startMonthDropDownPanel = _startMonthDropDown.parent as UIPanel;
            startMonthDropDownPanel.width = _startMonthDropDown.width;
            startMonthDropDownPanel.relativePosition = new Vector3(startDayDropDownPanel.relativePosition.x + startDayDropDownPanel.width + 5, _createButton.relativePosition.y - 7f);

            dropdownLabel = startMonthDropDownPanel.Find<UILabel>("Label");
            dropdownLabel.width = _startMonthDropDown.width;

            var startHourDropDownPanel = _startHourDropDown.parent as UIPanel;
            startHourDropDownPanel.width = _startHourDropDown.width;
            startHourDropDownPanel.relativePosition = new Vector3(startMonthDropDownPanel.relativePosition.x + startMonthDropDownPanel.width + 5, _createButton.relativePosition.y - 7f);

            dropdownLabel = startHourDropDownPanel.Find<UILabel>("Label");
            dropdownLabel.width = _startHourDropDown.width;

            var startMinuteDropDownPanel = _startMinuteDropDown.parent as UIPanel;
            startMinuteDropDownPanel.width = _startMinuteDropDown.width;
            startMinuteDropDownPanel.relativePosition = new Vector3(startHourDropDownPanel.relativePosition.x + startHourDropDownPanel.width + 5, _createButton.relativePosition.y - 7f);

            dropdownLabel = startMinuteDropDownPanel.Find<UILabel>("Label");
            dropdownLabel.width = _startMinuteDropDown.width;

            _incentiveList.DisplayAt(0);
        }

        public override void Update()
        {
            if (_startHourDropDown != null)
            {
                if (_startHourDropDown.selectedIndex + 1 <= TimeInfo.Now.Hour)
                {
                    _startHourDropDown.selectedIndex = (int)Mathf.Max(1, _startHourDropDown.selectedIndex + 1);
                }
            }
        }

        private void OptionItem_OnOptionItemChanged() => UpdateTotalCost();

        private void TicketSlider_eventValueChanged(UIComponent component, float value) => UpdateTotalCost();

        private void UpdateTotalCost()
        {
            if (_ticketSlider != null)
            {
                var sliderPanel = _ticketSlider.parent as UIPanel;
                var sliderLabel = sliderPanel.Find<UILabel>("Label");
                string ticketsText = localizationProvider.Translate(TranslationKeys.VanillaEventTicketSliderLabel);
                sliderLabel.text = string.Format("{0} {1}", _ticketSlider.value, ticketsText);
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
                int month = byte.Parse(_startMonthDropDown.selectedValue);
                int day = byte.Parse(_startDayDropDown.selectedValue);

                int hour = byte.Parse(_startHourDropDown.selectedValue);
                int minute = byte.Parse(_startMinuteDropDown.selectedValue);

                var startTime = new DateTime(year, month, day, hour, minute, 0);

                // Real Time event
                var rtEvent = new RealTimeCityEvent(template, Mathf.RoundToInt(_ticketSlider.value));

                // Copy incentives (optional—Real Time skips now)
                foreach (var item in optionItems)
                {
                    rtEvent.AddIncentive(item.title, item.sliderValue, item.cost);  // If needed
                }

                // Add (Real Time manager)
                SimulationHandler.EventManager.AddEvent(rtEvent, eventBuildingID, startTime);  // ← No clash check
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
            _totalAmountLabel.text = localizationProvider.Translate(TranslationKeys.VanillaEventTotalAmountLabel);
            _totalAmountLabel.tooltip = localizationProvider.Translate(TranslationKeys.VanillaEventTotalAmountLabelTooltip);
            _totalIncomeLabel.text = localizationProvider.Translate(TranslationKeys.VanillaEventTotalIncomeLabel);
            _totalIncomeLabel.tooltip = localizationProvider.Translate(TranslationKeys.VanillaEventTotalIncomeLabelTooltip);
            _createButton.text = localizationProvider.Translate(TranslationKeys.VanillaEventCreateButton);
            _createButton.tooltip = localizationProvider.Translate(TranslationKeys.VanillaEventCreateButtonTooltip);

            _startDayDropDown.text = localizationProvider.Translate(TranslationKeys.VanillaEventDayDropDownLabel);
            _startDayDropDown.tooltip = localizationProvider.Translate(TranslationKeys.VanillaEventDayDropDownLabelTooltip);
            _startMonthDropDown.text = localizationProvider.Translate(TranslationKeys.VanillaEventMonthDropDownLabel);
            _startMonthDropDown.tooltip = localizationProvider.Translate(TranslationKeys.VanillaEventMonthDropDownLabelTooltip);
            _startHourDropDown.text = localizationProvider.Translate(TranslationKeys.VanillaEventHourDropDownLabel);
            _startHourDropDown.tooltip = localizationProvider.Translate(TranslationKeys.VanillaEventHourDropDownLabelTooltip);
            _startMinuteDropDown.text = localizationProvider.Translate(TranslationKeys.VanillaEventMinuteDropDownLabel);
            _startMinuteDropDown.tooltip = localizationProvider.Translate(TranslationKeys.VanillaEventMinuteDropDownLabelTooltip);

            var sliderPanel = _ticketSlider.parent as UIPanel;
            var sliderLabel = sliderPanel.Find<UILabel>("Label");
            sliderLabel.tooltip = localizationProvider.Translate(TranslationKeys.VanillaEventTicketSliderLabelTooltip);

            _totalAmountLabel.Invalidate();
            _totalIncomeLabel.Invalidate();
            _startDayDropDown.Invalidate();
            _startMonthDropDown.Invalidate();
            _startHourDropDown.Invalidate();
            _startMinuteDropDown.Invalidate();
            _createButton.Invalidate();

            UpdateTotalCost();
            PerformLayout();
        }
    }
}
