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

        internal static RealTimeConfig RealTimeConfig { get => field ?? throw new InvalidOperationException("RealTimeConfig not set"); private set; }

        internal static ILocalizationProvider LocalizationProvider { get => field ?? throw new InvalidOperationException("LocalizationProvider not set"); private set; }

        internal static void Configure(RealTimeConfig config, ILocalizationProvider provider)
        {
            RealTimeConfig = config;
            LocalizationProvider = provider;
        }

        protected float totalCost = 0f;
        protected float maxIncome = 0f;
        protected ushort eventBuildingID = 0;
        private bool _updatingSchedule;

        protected UIFastList _upcomingEventsList = null;
        protected UIButton _upcomingToggleBtn = null;  // Tab button to show/hide
        private bool _upcomingEventsVisible = false;

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
            dropdownLabel.textScale = 0.875f;

            dropdownPanel = _startMonthDropDown.parent as UIPanel;
            dropdownLabel = dropdownPanel.Find<UILabel>("Label");
            dropdownLabel.textScale = 0.875f;

            dropdownPanel = _startHourDropDown.parent as UIPanel;
            dropdownLabel = dropdownPanel.Find<UILabel>("Label");
            dropdownLabel.textScale = 0.875f;

            dropdownPanel = _startMinuteDropDown.parent as UIPanel;
            dropdownLabel = dropdownPanel.Find<UILabel>("Label");
            dropdownLabel.textScale = 0.875f;

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

                _upcomingToggleBtn = _helper.AddButton("Upcoming Events", OnUpcomingToggleClicked) as UIButton;
                _upcomingToggleBtn.size = new Vector2(120, 28);
                _upcomingToggleBtn.textScale = 0.85f;

                _upcomingEventsList = UIFastList.Create<UpcomingEventRow>(this);
                _upcomingEventsList.rowHeight = 30f;
                _upcomingEventsList.backgroundSprite = "UnlockingPanel";
                _upcomingEventsList.isVisible = false;

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

                _createButton = _helper.AddButton("Create", new OnButtonClicked(CreateEvent)) as UIButton;

                int defaultDaySelection = Singleton<SimulationManager>.instance.m_currentGameTime.Day - 1;
                int defaultMonthSelection = Singleton<SimulationManager>.instance.m_currentGameTime.Month - 1;
                int defaultHourSelection = AdjustEventStartTime(Singleton<SimulationManager>.instance.m_currentGameTime).Hour;
                int defaultMinuteSelection = Singleton<SimulationManager>.instance.m_currentGameTime.Minute;

                _startDayDropDown = (UIDropDown)_helper.AddDropdown("Day", [.. Enumerable.Range(1, 31).Select(i => i.ToString("D2"))], defaultDaySelection, delegate (int value) {});
                _startMonthDropDown = (UIDropDown)_helper.AddDropdown("Month", [.. Enumerable.Range(1, 12).Select(i => i.ToString("D2"))], defaultMonthSelection, delegate (int value) { });
                _startHourDropDown = (UIDropDown)_helper.AddDropdown("Hour", [.. Enumerable.Range(0, 24).Select(i => i.ToString("D2"))], defaultHourSelection, delegate (int value) { });
                _startMinuteDropDown = (UIDropDown)_helper.AddDropdown("Minute", [.. Enumerable.Range(0, 60).Select(i => i.ToString("D2"))], defaultMinuteSelection, delegate (int value) { });

                _startDayDropDown.size = new Vector3(50f, 27f);
                _startMonthDropDown.size = new Vector3(50f, 27f);
                _startHourDropDown.size = new Vector3(50f, 27f);
                _startMinuteDropDown.size = new Vector3(50f, 27f);

                _startDayDropDown.textScale = 0.875f;
                _startMonthDropDown.textScale = 0.875f;
                _startHourDropDown.textScale = 0.875f;
                _startMinuteDropDown.textScale = 0.875f;

                _startDayDropDown.textFieldPadding.right = 10;
                _startMonthDropDown.textFieldPadding.right = 10;
                _startHourDropDown.textFieldPadding.right = 10;
                _startMinuteDropDown.textFieldPadding.right = 10;

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
            }
        }

        private void OnScheduleDayChanged(UIComponent uiComponent, int value)
        {
            if (_updatingSchedule)
            {
                return;
            }
            _updatingSchedule = true;
            try
            {
                int year = Singleton<SimulationManager>.instance.m_currentGameTime.Year;

                var dropdown = (UIDropDown)uiComponent;
                int startMonth = int.Parse(_startMonthDropDown.selectedValue);

                string dayText = dropdown.items[value];
                int day = byte.Parse(dayText);

                int num = DateTime.DaysInMonth(2, startMonth);
                bool flag = day > num;
                if (flag)
                {
                    day = num;
                }

                int startHour = byte.Parse(_startHourDropDown.selectedValue);
                int startMinute = byte.Parse(_startMinuteDropDown.selectedValue);
                var startDateTime = new DateTime(year, startMonth, day, startHour, startMinute, 0);
                var dateTime = AdjustEventStartTime(startDateTime);
                _startHourDropDown.selectedIndex = dateTime.Hour;
                _startMinuteDropDown.selectedIndex = dateTime.Minute;
            }
            finally
            {
                _updatingSchedule = false;
            }
        }

        private void OnScheduleMonthChanged(UIComponent uiComponent, int value)
        {
            if (_updatingSchedule)
            {
                return;
            }
            _updatingSchedule = true;
            try
            {
                int year = Singleton<SimulationManager>.instance.m_currentGameTime.Year;
                var dropdown = (UIDropDown)uiComponent;
                string monthText = dropdown.items[value];
                int month = int.Parse(monthText);

                int startDay = int.Parse(_startDayDropDown.selectedValue);

                int num = DateTime.DaysInMonth(2, month);
                if (startDay > num)
                {
                    startDay = num;
                }
                _startDayDropDown.selectedIndex = startDay - 1;

                int startHour = byte.Parse(_startHourDropDown.selectedValue);
                int startMinute = byte.Parse(_startMinuteDropDown.selectedValue);
                var startDateTime = new DateTime(year, month, startDay, startHour, startMinute, 0);
                var dateTime = AdjustEventStartTime(startDateTime);
                _startHourDropDown.selectedIndex = dateTime.Hour;
                _startMinuteDropDown.selectedIndex = dateTime.Minute;
            }
            finally
            {
                _updatingSchedule = false;
            }
        }

        private void OnScheduleHourChanged(UIComponent uiComponent, int value)
        {
            if (_updatingSchedule)
            {
                return;
            }
            _updatingSchedule = true;
            try
            {
                int year = Singleton<SimulationManager>.instance.m_currentGameTime.Year;
                var dropdown = (UIDropDown)uiComponent;

                int startDay = int.Parse(_startDayDropDown.selectedValue);
                int startMonth = int.Parse(_startMonthDropDown.selectedValue);
                int startMinute = int.Parse(_startMinuteDropDown.selectedValue);

                
                string hourText = dropdown.items[value];
                int hour = int.Parse(hourText);

                var startDateTime = new DateTime(year, startMonth, startDay, hour, startMinute, 0);
                var dateTime = AdjustEventStartTime(startDateTime);
                _startHourDropDown.selectedIndex = dateTime.Hour;
                _startMinuteDropDown.selectedIndex = dateTime.Minute;
            }
            finally
            {
                _updatingSchedule = false;
            }
        }

        private void OnScheduleMinuteChanged(UIComponent uiComponent, int value)
        {
            if (_updatingSchedule)
            {
                return;
            }
            _updatingSchedule = true;
            try
            {
                int year = Singleton<SimulationManager>.instance.m_currentGameTime.Year;
                var dropdown = (UIDropDown)uiComponent;

                int startDay = int.Parse(_startDayDropDown.selectedValue);
                int startMonth = int.Parse(_startMonthDropDown.selectedValue);
                int startHour = int.Parse(_startHourDropDown.selectedValue);

                
                string minuteText = dropdown.items[value];
                int minute = int.Parse(minuteText);

                var startDateTime = new DateTime(year, startMonth, startDay, startHour, minute, 0);
                var dateTime = AdjustEventStartTime(startDateTime);
                _startHourDropDown.selectedIndex = dateTime.Hour;
                _startMinuteDropDown.selectedIndex = dateTime.Minute;
            }
            finally
            {
                _updatingSchedule = false;
            }
        }

        private static DateTime AdjustEventStartTime(DateTime eventStartTime)
        {
            var result = eventStartTime;

            float earliestHour;
            float latestHour;
            float hour = result.Hour;
            float minute = result.Minute;

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

            if(result.Hour >= latestHour)
            {
                hour = latestHour;
                minute = 0;
            }
            else if(result.Hour < earliestHour)
            {
                hour = earliestHour;
            }

            return new DateTime(eventStartTime.Year, eventStartTime.Month, eventStartTime.Day, (int)hour, (int)minute, 0);
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

                LoadUpcomingEvents();
                _upcomingToggleBtn.isVisible = true;

                title = template.UserEventName;

                var incentives = template.Incentives;
                _incentiveList.rowsData.Clear();

                if(incentives != null)
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

                    _incentiveList.Refresh();
                }

                string tooltip = $"Males: {template.Attendees.Males}%\n" + $"Females: {template.Attendees.Females}%\n" + $"Young: {template.Attendees.YoungAdults}%";

                _informationLabel.tooltip = LocalizationProvider.Translate("RUSH_EVENT_DEMOS") + "\n" + tooltip;

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

            _upcomingEventsList.width = width - 20;
            _upcomingEventsList.height = _upcomingEventsVisible ? 120 : 0;
            _upcomingToggleBtn.relativePosition = new Vector3(10, _incentiveList.relativePosition.y + _incentiveList.height + 5);
            _upcomingEventsList.relativePosition = _incentiveList.relativePosition + new Vector3(0, _upcomingToggleBtn.height + 5);

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
            startMonthDropDownPanel.relativePosition = new Vector3(startDayDropDownPanel.relativePosition.x + startDayDropDownPanel.width + 10, _createButton.relativePosition.y - 7f);

            dropdownLabel = startMonthDropDownPanel.Find<UILabel>("Label");
            dropdownLabel.width = _startMonthDropDown.width;

            var startHourDropDownPanel = _startHourDropDown.parent as UIPanel;
            startHourDropDownPanel.width = _startHourDropDown.width;
            startHourDropDownPanel.relativePosition = new Vector3(startMonthDropDownPanel.relativePosition.x + startMonthDropDownPanel.width + 10, _createButton.relativePosition.y - 7f);

            dropdownLabel = startHourDropDownPanel.Find<UILabel>("Label");
            dropdownLabel.width = _startHourDropDown.width;

            var startMinuteDropDownPanel = _startMinuteDropDown.parent as UIPanel;
            startMinuteDropDownPanel.width = _startMinuteDropDown.width;
            startMinuteDropDownPanel.relativePosition = new Vector3(startHourDropDownPanel.relativePosition.x + startHourDropDownPanel.width + 10, _createButton.relativePosition.y - 7f);

            dropdownLabel = startMinuteDropDownPanel.Find<UILabel>("Label");
            dropdownLabel.width = _startMinuteDropDown.width;

            _incentiveList.DisplayAt(0);
        }

        private void OptionItem_OnOptionItemChanged() => UpdateTotalCost();

        private void TicketSlider_eventValueChanged(UIComponent component, float value) => UpdateTotalCost();

        private void UpdateTotalCost()
        {
            if (_ticketSlider != null)
            {
                var sliderPanel = _ticketSlider.parent as UIPanel;
                var sliderLabel = sliderPanel.Find<UILabel>("Label");
                string ticketsText = LocalizationProvider.Translate(TranslationKeys.VanillaEventTicketSliderLabel);
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
                int month = int.Parse(_startMonthDropDown.selectedValue);
                int day = int.Parse(_startDayDropDown.selectedValue);
                int hour = int.Parse(_startHourDropDown.selectedValue);
                int minute = int.Parse(_startMinuteDropDown.selectedValue);

                // Clamp day to valid range (CS ignores leap years so year 2 = always non-leap)
                int safeDay = Math.Min(day, DateTime.DaysInMonth(2, month));

                var now = Singleton<SimulationManager>.instance.m_currentGameTime;
                var startTime = new DateTime(year, month, safeDay, hour, minute, 0);

                // Auto-fix past dates to next year
                if (startTime < now)
                {
                    int nextYear = now.Year + 1;
                    startTime = new DateTime(nextYear, month, day, hour, minute, 0);
                }

                // Real Time event
                var rtEvent = new RealTimeCityEvent(template);

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
            _totalAmountLabel.text = LocalizationProvider.Translate(TranslationKeys.VanillaEventTotalAmountLabel);
            _totalAmountLabel.tooltip = LocalizationProvider.Translate(TranslationKeys.VanillaEventTotalAmountLabelTooltip);
            _totalIncomeLabel.text = LocalizationProvider.Translate(TranslationKeys.VanillaEventTotalIncomeLabel);
            _totalIncomeLabel.tooltip = LocalizationProvider.Translate(TranslationKeys.VanillaEventTotalIncomeLabelTooltip);
            _createButton.text = LocalizationProvider.Translate(TranslationKeys.VanillaEventCreateButton);
            _createButton.tooltip = LocalizationProvider.Translate(TranslationKeys.VanillaEventCreateButtonTooltip);

            _startDayDropDown.text = LocalizationProvider.Translate(TranslationKeys.VanillaEventDayDropDownLabel);
            _startDayDropDown.tooltip = LocalizationProvider.Translate(TranslationKeys.VanillaEventDayDropDownLabelTooltip);
            _startMonthDropDown.text = LocalizationProvider.Translate(TranslationKeys.VanillaEventMonthDropDownLabel);
            _startMonthDropDown.tooltip = LocalizationProvider.Translate(TranslationKeys.VanillaEventMonthDropDownLabelTooltip);
            _startHourDropDown.text = LocalizationProvider.Translate(TranslationKeys.VanillaEventHourDropDownLabel);
            _startHourDropDown.tooltip = LocalizationProvider.Translate(TranslationKeys.VanillaEventHourDropDownLabelTooltip);
            _startMinuteDropDown.text = LocalizationProvider.Translate(TranslationKeys.VanillaEventMinuteDropDownLabel);
            _startMinuteDropDown.tooltip = LocalizationProvider.Translate(TranslationKeys.VanillaEventMinuteDropDownLabelTooltip);

            var sliderPanel = _ticketSlider.parent as UIPanel;
            var sliderLabel = sliderPanel.Find<UILabel>("Label");
            sliderLabel.tooltip = LocalizationProvider.Translate(TranslationKeys.VanillaEventTicketSliderLabelTooltip);

            _upcomingToggleBtn.text = LocalizationProvider.Translate(TranslationKeys.VanillaEventUpcomingShowBtn);

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

        private void LoadUpcomingEvents()
        {
            _upcomingEventsList.rowsData.Clear();
            var now = Singleton<SimulationManager>.instance.m_currentGameTime;

            var events = SimulationHandler.EventManager.GetUpcomingEventsForBuilding(eventBuildingID);

            foreach (var ev in events.Where(e => e.StartTime > now).OrderBy(e => e.StartTime))
            {
                var capturedEvent = ev; // ← fix closure capture bug

                _upcomingEventsList.rowsData.Add(new UpcomingEventItem
                {
                    eventName = capturedEvent.UserEventName ?? capturedEvent.EventName ?? "Event",
                    timeStr = capturedEvent.StartTime.ToString("MMM dd\nHH:mm"),
                    deleteAction = () =>
                    {
                        SimulationHandler.EventManager.RemoveEvent(capturedEvent);
                        LoadUpcomingEvents();
                    }
                });
            }

            _upcomingEventsList.Refresh();
        }

        private void OnUpcomingToggleClicked()
        {
            string show_btn_text = LocalizationProvider.Translate(TranslationKeys.VanillaEventUpcomingShowBtn);
            string hide_btn_text = LocalizationProvider.Translate(TranslationKeys.VanillaEventUpcomingHideBtn);

            _upcomingEventsVisible = !_upcomingEventsVisible;
            _upcomingEventsList.isVisible = _upcomingEventsVisible;
            _upcomingToggleBtn.text = _upcomingEventsVisible ? hide_btn_text : show_btn_text;
            PerformLayout();
        }
    }
}
