namespace RealTime.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ColossalFramework;
    using ColossalFramework.Globalization;
    using ColossalFramework.UI;
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

    /// <summary>
    /// A WorldInfoPanel-style UI for creating and managing user city events.
    /// Mirrors the structure of RaceEventWorldInfoPanel with custom naming.
    /// Panel: 492×742, Tabstrip at y=40, TabContainer below.
    /// </summary>
    internal class UserEventCreationPanel : UIPanel
    {
        // ── Static configuration (set before panel is shown) ─────────────────
        internal static RealTimeConfig RealTimeConfig { get => field ?? throw new InvalidOperationException("RealTimeConfig not set"); private set; }

        internal static ILocalizationProvider LocalizationProvider { get => field ?? throw new InvalidOperationException("LocalizationProvider not set"); private set; }

        internal static void Configure(RealTimeConfig config, ILocalizationProvider provider)
        {
            RealTimeConfig = config ?? throw new ArgumentNullException(nameof(config));
            LocalizationProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        // ── Tab indices ───────────────────────────────────────────────────────
        private const int TAB_SCHEDULE = 0;
        private const int TAB_UPCOMING = 1;
        private const int TAB_PAST = 2;

        // ── Root layout ───────────────────────────────────────────────────────
        private UITabstrip _tabstrip;
        private UITabContainer _tabContainer;

        // ── Schedule tab ──────────────────────────────────────────────────────
        private UIPanel _containerSchedule;
        private UIPanel _eventConfigPanel;
        private UIScrollablePanel _eventConfigContainer;
        private UIDropDown _eventTypeDropdown;
        private UIPanel _incentiveListPanel;
        private UIFastList _incentiveList;
        private UIPanel _scheduleFooter;
        private UIButton _createEventButton;
        private UILabel _labelEventCost;
        private UILabel _labelMaxProfit;
        private UILabel _labelTickets;
        private UILabel _labelScheduledDate;
        private UISlider _ticketSlider;
        private UIDropDown _dropDay;
        private UIDropDown _dropMonth;
        private UIDropDown _dropHour;
        private UIDropDown _dropMinute;

        // ── Upcoming tab ──────────────────────────────────────────────────────
        private UIPanel _containerUpcoming;
        private UIScrollablePanel _upcomingEventList;

        // ── Past events tab ───────────────────────────────────────────────────
        private UIPanel _containerPast;
        private UIScrollablePanel _pastEventList;

        // ── Upcoming / past event rows ────────────────────────────────────────
        private readonly List<UpcomingEventRow> _upcomingRows = [];
        private readonly List<PastEventRow> _pastRows = [];

        // ── State ─────────────────────────────────────────────────────────────
        private CityEventTemplate _currentEventData;
        private ushort _buildingId;
        private bool _updatingSchedule;
        private float _totalCost;
        private float _maxIncome;

        // ─────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────────

        public override void Start()
        {
            base.Start();
            BuildPanel();
            TranslationOnLanguageChanged();
            isVisible = false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        public void Show(ushort buildingId, List<CityEventTemplate> availableEvents)
        {
            _buildingId = buildingId;           // ← must be first
            PopulateEventTypeDropdown(availableEvents);
            RefreshUpcomingTab();
            RefreshPastTab();
            _tabstrip.selectedIndex = TAB_SCHEDULE;
            Show();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Panel construction
        // ─────────────────────────────────────────────────────────────────────

        private void BuildPanel()
        {
            width = 492f;
            height = 742f;
            backgroundSprite = "MenuPanel2";
            atlas = TextureUtils.GetAtlas("Ingame");

            BuildCaption();
            BuildTabstrip();
            BuildTabContainer();
        }

        private void BuildCaption()
        {
            var caption = AddUIComponent<UIPanel>();
            caption.name = "Caption";
            caption.width = width;
            caption.height = 40f;
            caption.relativePosition = Vector3.zero;

            var title = caption.AddUIComponent<UILabel>();
            title.name = "BuildingName";
            title.text = LocalizationProvider.Translate(TranslationKeys.VanillaEventSelectionLabelTitle);
            title.textAlignment = UIHorizontalAlignment.Center;
            title.verticalAlignment = UIVerticalAlignment.Middle;
            title.size = new Vector2(width - 40f, 40f);
            title.relativePosition = new Vector3(0f, 0f);
            title.textScale = 0.9f;

            var closeBtn = caption.AddUIComponent<UIButton>();
            closeBtn.name = "Close";
            closeBtn.size = new Vector2(32f, 32f);
            closeBtn.relativePosition = new Vector3(width - 36f, 4f);
            closeBtn.normalBgSprite = "buttonclose";
            closeBtn.hoveredBgSprite = "buttonclosehover";
            closeBtn.pressedBgSprite = "buttonclosepressed";
            closeBtn.eventClicked += (c, e) => Hide();
        }

        private void BuildTabstrip()
        {
            _tabstrip = AddUIComponent<UITabstrip>();
            _tabstrip.name = "Tabstrip";
            _tabstrip.size = new Vector2(492f, 30f);
            _tabstrip.relativePosition = new Vector3(0f, 40f);

            AddTab(_tabstrip, "TabSchedule", LocalizationProvider.Translate("TranslationKeys.TabSchedule"));
            AddTab(_tabstrip, "TabUpcoming", LocalizationProvider.Translate(TranslationKeys.VanillaEventUpcomingShowBtn));
            AddTab(_tabstrip, "TabPast", LocalizationProvider.Translate("TranslationKeys.TabPast"));

            _tabstrip.selectedIndex = TAB_SCHEDULE;
            _tabstrip.eventSelectedIndexChanged += OnTabChanged;
        }

        private static void AddTab(UITabstrip strip, string tabName, string tabText)
        {
            var btn = strip.AddTab(tabText);
            btn.name = tabName;
            btn.size = new Vector2(96f, 31.5f);
            btn.normalBgSprite = "GenericTab";
            btn.focusedBgSprite = "GenericTabFocused";
            btn.hoveredBgSprite = "GenericTabHovered";
            btn.pressedBgSprite = "GenericTabPressed";
            btn.textColor = Color.white;
            btn.focusedTextColor = Color.white;
            btn.hoveredTextColor = Color.white;
            btn.textScale = 0.8f;
        }

        private void BuildTabContainer()
        {
            _tabContainer = AddUIComponent<UITabContainer>();
            _tabContainer.name = "TabContainer";
            _tabContainer.size = new Vector2(492f, 672f);
            _tabContainer.relativePosition = new Vector3(0f, 70f);
            _tabContainer.padding = new RectOffset(0, 0, 0, 0);

            _tabstrip.tabPages = _tabContainer;

            BuildScheduleTab();
            BuildUpcomingTab();
            BuildPastTab();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Schedule tab
        // ─────────────────────────────────────────────────────────────────────

        private void BuildScheduleTab()
        {
            _containerSchedule = _tabContainer.AddUIComponent<UIPanel>();
            _containerSchedule.name = "ContainerSchedule";
            _containerSchedule.size = new Vector2(492f, 672f);

            _eventConfigPanel = _containerSchedule.AddUIComponent<UIPanel>();
            _eventConfigPanel.name = "EventConfigPanel";
            _eventConfigPanel.size = new Vector2(492f, 658f);
            _eventConfigPanel.relativePosition = new Vector3(0f, 0f);
            _eventConfigPanel.backgroundSprite = "GenericPanel";
            _eventConfigPanel.atlas = TextureUtils.GetAtlas("Ingame");
            _eventConfigPanel.color = new Color32(91, 97, 106, 255);

            _eventConfigContainer = _eventConfigPanel.AddUIComponent<UIScrollablePanel>();
            _eventConfigContainer.name = "EventConfigContainer";
            _eventConfigContainer.size = new Vector2(456f, 580f);
            _eventConfigContainer.relativePosition = new Vector3(8f, 8f);
            _eventConfigContainer.autoLayout = true;
            _eventConfigContainer.autoLayoutDirection = LayoutDirection.Vertical;
            _eventConfigContainer.autoLayoutPadding = new RectOffset(0, 0, 0, 4);
            _eventConfigContainer.clipChildren = true;
            _eventConfigContainer.scrollWheelDirection = UIOrientation.Vertical;

            BuildEventTypeDropdown();
            BuildTicketSliderRow();
            BuildCostInfoRow();
            BuildIncentiveListArea();
            BuildScheduleDateRow();
            BuildScheduleFooter();
        }

        private void BuildEventTypeDropdown()
        {
            var row = _eventConfigContainer.AddUIComponent<UIPanel>();
            row.name = "PanelEventType";
            row.size = new Vector2(444f, 30f);

            _eventTypeDropdown = row.AddUIComponent<UIDropDown>();
            _eventTypeDropdown.name = "EventTypeDropdown";
            _eventTypeDropdown.size = new Vector2(440f, 28f);
            _eventTypeDropdown.relativePosition = new Vector3(2f, 1f);
            _eventTypeDropdown.textFieldPadding = new RectOffset(8, 0, 4, 0);
            _eventTypeDropdown.triggerButton = _eventTypeDropdown; // self-trigger
            _eventTypeDropdown.listBackground = "GenericPanelDark";
            _eventTypeDropdown.itemHover = "ListItemHover";
            _eventTypeDropdown.itemHighlight = "ListItemHighlight";
            _eventTypeDropdown.listHeight = 160;
            _eventTypeDropdown.itemHeight = 26;
            _eventTypeDropdown.textColor = Color.white;
            _eventTypeDropdown.eventSelectedIndexChanged += OnEventTypeChanged;
        }

        private void BuildTicketSliderRow()
        {
            var row = _eventConfigContainer.AddUIComponent<UIPanel>();
            row.name = "PanelTickets";
            row.size = new Vector2(456f, 40f);

            var lblTicketTitle = row.AddUIComponent<UILabel>();
            lblTicketTitle.name = "LabelTicketTitle";
            lblTicketTitle.textScale = 0.8f;
            lblTicketTitle.relativePosition = new Vector3(5f, 4f);

            _ticketSlider = row.AddUIComponent<UISlider>();
            _ticketSlider.name = "TicketSlider";
            _ticketSlider.size = new Vector2(260f, 16f);
            _ticketSlider.relativePosition = new Vector3(5f, 20f);
            _ticketSlider.minValue = 100f;
            _ticketSlider.maxValue = 9000f;
            _ticketSlider.stepSize = 10f;
            _ticketSlider.value = 500f;
            _ticketSlider.backgroundSprite = "ScrollbarTrack";
            _ticketSlider.atlas = TextureUtils.GetAtlas("Ingame");

            var thumb = _ticketSlider.AddUIComponent<UISprite>();
            thumb.spriteName = "ScrollbarThumb";
            thumb.size = new Vector2(16f, 16f);
            _ticketSlider.thumbObject = thumb;

            _labelTickets = row.AddUIComponent<UILabel>();
            _labelTickets.name = "LabelTickets";
            _labelTickets.textColor = Color.white;
            _labelTickets.textScale = 0.75f;
            _labelTickets.relativePosition = new Vector3(275f, 20f);

            _ticketSlider.eventValueChanged += (c, v) =>
            {
                foreach (IncentiveOptionItem optionItemObject in _incentiveList?.rowsData ?? new FastList<object>())
                {
                    optionItemObject.ticketCount = v;
                    optionItemObject.UpdateTicketSize();
                }
                UpdateCostDisplay();
            };
        }

        private void BuildCostInfoRow()
        {
            var row = _eventConfigContainer.AddUIComponent<UIPanel>();
            row.name = "PanelCostInfo";
            row.size = new Vector2(456f, 26f);
            row.backgroundSprite = "GenericPanel";
            row.atlas = TextureUtils.GetAtlas("Ingame");
            row.color = new Color32(45, 52, 61, 255);

            var costTitle = row.AddUIComponent<UILabel>();
            costTitle.name = "LabelEventCostTitle";
            costTitle.textColor = new Color32(255, 100, 100, 255);
            costTitle.textScale = 0.7f;
            costTitle.relativePosition = new Vector3(4f, 5f);

            _labelEventCost = row.AddUIComponent<UILabel>();
            _labelEventCost.name = "LabelEventCostValue";
            _labelEventCost.textColor = new Color32(238, 95, 0, 255);
            _labelEventCost.textScale = 0.7f;
            _labelEventCost.textAlignment = UIHorizontalAlignment.Right;
            _labelEventCost.relativePosition = new Vector3(120f, 5f);
            _labelEventCost.text = "0";

            var profitTitle = row.AddUIComponent<UILabel>();
            profitTitle.name = "LabelMaxProfitTitle";
            profitTitle.textColor = new Color32(206, 248, 0, 255);
            profitTitle.textScale = 0.7f;
            profitTitle.relativePosition = new Vector3(240f, 5f);

            _labelMaxProfit = row.AddUIComponent<UILabel>();
            _labelMaxProfit.name = "LabelMaxProfitValue";
            _labelMaxProfit.textColor = new Color32(151, 238, 0, 255);
            _labelMaxProfit.textScale = 0.7f;
            _labelMaxProfit.textAlignment = UIHorizontalAlignment.Right;
            _labelMaxProfit.relativePosition = new Vector3(360f, 5f);
            _labelMaxProfit.text = "0";
        }

        private void BuildIncentiveListArea()
        {
            _incentiveListPanel = _eventConfigContainer.AddUIComponent<UIPanel>();
            _incentiveListPanel.name = "IncentiveListPanel";
            _incentiveListPanel.size = new Vector2(456f, 415f);

            _incentiveList = UIFastList.Create<UIFastListIncentives>(_incentiveListPanel);
            _incentiveList.name = "IncentiveList";
            _incentiveList.size = new Vector2(452f, 415f);
            _incentiveList.relativePosition = Vector3.zero;
            _incentiveList.rowHeight = 76f;
            _incentiveList.canSelect = false;
            _incentiveList.backgroundSprite = "UnlockingPanel";
            _incentiveList.rowsData.Clear();
            _incentiveList.selectedIndex = -1;
        }

        private void BuildScheduleDateRow()
        {
            var panel = _eventConfigContainer.AddUIComponent<UIPanel>();
            panel.name = "PanelScheduleDate";
            panel.size = new Vector2(456f, 60f);

            var lblStartDate = panel.AddUIComponent<UILabel>();
            lblStartDate.name = "LabelStartDate";
            lblStartDate.textColor = Color.white;
            lblStartDate.textScale = 0.8f;
            lblStartDate.relativePosition = new Vector3(8f, 4f);

            _dropDay = CreateDropdown(panel, "DropDay", 50f, new Vector3(8f, 24f));
            _dropMonth = CreateDropdown(panel, "DropMonth", 50f, new Vector3(68f, 24f));
            _dropHour = CreateDropdown(panel, "DropHour", 50f, new Vector3(138f, 24f));
            _dropMinute = CreateDropdown(panel, "DropMinute", 50f, new Vector3(198f, 24f));

            _dropDay.textScale = 0.875f;
            _dropMonth.textScale = 0.875f;
            _dropHour.textScale = 0.875f;
            _dropMinute.textScale = 0.875f;

            _dropDay.eventSelectedIndexChanged += (c, v) =>
            {
                OnScheduleDayChanged(c, v);
                UpdateCostDisplay();
            };
            _dropMonth.eventSelectedIndexChanged += (c, v) =>
            {
                OnScheduleMonthChanged(c, v);
                UpdateCostDisplay();
            };
            _dropHour.eventSelectedIndexChanged += (c, v) =>
            {
                OnScheduleHourChanged(c, v);
                UpdateCostDisplay();
            };
            _dropMinute.eventSelectedIndexChanged += (c, v) =>
            {
                OnScheduleMinuteChanged(c, v);
                UpdateCostDisplay();
            };

            var panelDatePreview = _eventConfigContainer.AddUIComponent<UIPanel>();
            panelDatePreview.name = "PanelDatePreview";
            panelDatePreview.size = new Vector2(456f, 22f);

            _labelScheduledDate = panelDatePreview.AddUIComponent<UILabel>();
            _labelScheduledDate.name = "LabelScheduledDate";
            _labelScheduledDate.textColor = new Color32(255, 180, 0, 255);
            _labelScheduledDate.textScale = 0.75f;
            _labelScheduledDate.relativePosition = new Vector3(8f, 2f);
            _labelScheduledDate.text = "";
        }

        private void BuildScheduleFooter()
        {
            _scheduleFooter = _eventConfigPanel.AddUIComponent<UIPanel>();
            _scheduleFooter.name = "PanelCreateEvent";
            _scheduleFooter.size = new Vector2(476f, 40f);
            _scheduleFooter.relativePosition = new Vector3(8f, 610f);
            _scheduleFooter.backgroundSprite = "GenericPanel";
            _scheduleFooter.atlas = TextureUtils.GetAtlas("Ingame");
            _scheduleFooter.color = new Color32(91, 97, 106, 255);

            _createEventButton = _scheduleFooter.AddUIComponent<UIButton>();
            _createEventButton.name = "ButtonCreateEvent";
            _createEventButton.size = new Vector2(120f, 32f);
            _createEventButton.relativePosition = new Vector3(348f, 4f);
            _createEventButton.normalBgSprite = "ButtonMenu";
            _createEventButton.hoveredBgSprite = "ButtonMenuHovered";
            _createEventButton.pressedBgSprite = "ButtonMenuPressed";
            _createEventButton.disabledBgSprite = "ButtonMenuDisabled";
            _createEventButton.textColor = Color.white;
            _createEventButton.textScale = 0.9f;
            _createEventButton.anchor = UIAnchorStyle.Right | UIAnchorStyle.Bottom;
            _createEventButton.eventClicked += (c, e) => CreateEvent();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Upcoming tab
        // ─────────────────────────────────────────────────────────────────────

        private void BuildUpcomingTab()
        {
            _containerUpcoming = _tabContainer.AddUIComponent<UIPanel>();
            _containerUpcoming.name = "ContainerUpcoming";
            _containerUpcoming.size = new Vector2(492f, 672f);

            var heading = _containerUpcoming.AddUIComponent<UILabel>();
            heading.name = "LabelUpcomingHeading";
            heading.textAlignment = UIHorizontalAlignment.Center;
            heading.textColor = Color.white;
            heading.textScale = 0.85f;
            heading.size = new Vector2(492f, 24f);
            heading.relativePosition = new Vector3(0f, 8f);

            var panelUpcoming = _containerUpcoming.AddUIComponent<UIPanel>();
            panelUpcoming.name = "PanelUpcomingEvents";
            panelUpcoming.size = new Vector2(492f, 630f);
            panelUpcoming.relativePosition = new Vector3(0f, 40f);

            _upcomingEventList = panelUpcoming.AddUIComponent<UIScrollablePanel>();
            _upcomingEventList.name = "UpcomingEventList";
            _upcomingEventList.size = new Vector2(460f, 614f);
            _upcomingEventList.relativePosition = new Vector3(16f, 8f);
            _upcomingEventList.autoLayout = true;
            _upcomingEventList.autoLayoutDirection = LayoutDirection.Vertical;
            _upcomingEventList.autoLayoutPadding = new RectOffset(0, 0, 0, 2);
            _upcomingEventList.clipChildren = true;
            _upcomingEventList.scrollWheelDirection = UIOrientation.Vertical;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Past events tab
        // ─────────────────────────────────────────────────────────────────────

        private void BuildPastTab()
        {
            _containerPast = _tabContainer.AddUIComponent<UIPanel>();
            _containerPast.name = "ContainerPast";
            _containerPast.size = new Vector2(492f, 672f);

            var heading = _containerPast.AddUIComponent<UILabel>();
            heading.name = "LabelPastHeading";
            heading.textAlignment = UIHorizontalAlignment.Center;
            heading.textColor = Color.white;
            heading.textScale = 0.85f;
            heading.size = new Vector2(492f, 24f);
            heading.relativePosition = new Vector3(0f, 8f);

            var panelPast = _containerPast.AddUIComponent<UIPanel>();
            panelPast.name = "PanelPastEvents";
            panelPast.size = new Vector2(492f, 630f);
            panelPast.relativePosition = new Vector3(0f, 40f);

            _pastEventList = panelPast.AddUIComponent<UIScrollablePanel>();
            _pastEventList.name = "PastEventList";
            _pastEventList.size = new Vector2(460f, 614f);
            _pastEventList.relativePosition = new Vector3(16f, 8f);
            _pastEventList.autoLayout = true;
            _pastEventList.autoLayoutDirection = LayoutDirection.Vertical;
            _pastEventList.autoLayoutPadding = new RectOffset(0, 0, 0, 2);
            _pastEventList.clipChildren = true;
            _pastEventList.scrollWheelDirection = UIOrientation.Vertical;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Tab switching
        // ─────────────────────────────────────────────────────────────────────

        private void OnTabChanged(UIComponent c, int index)
        {
            if (index == TAB_UPCOMING)
            {
                RefreshUpcomingTab();
            }
            else if (index == TAB_PAST)
            {
                RefreshPastTab();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Event type dropdown
        // ─────────────────────────────────────────────────────────────────────

        private void PopulateEventTypeDropdown(List<CityEventTemplate> templates)
        {
            var buildingMgr = Singleton<BuildingManager>.instance;
            var building = buildingMgr.m_buildings.m_buffer[_buildingId]; // ← use the field, not WorldInfoPanel
            string buildingName = building.Info.name;

            var items = new List<string>();
            foreach (var e in templates)
            {
                items.Add(e.EventName);
            }

            _eventTypeDropdown.items = [.. items];
            if (items.Count > 0)
            {
                _eventTypeDropdown.selectedIndex = 0;
                var template = CityEventsLoader.Instance.GetEventTemplate(items[0], buildingName);
                LoadEventData(template);
            }
        }

        private void OnEventTypeChanged(UIComponent c, int index)
        {
            if (index < 0 || index >= _eventTypeDropdown.items.Length)
            {
                return;
            }

            var buildingMgr = Singleton<BuildingManager>.instance;
            var building = buildingMgr.m_buildings.m_buffer[_buildingId]; // ← use field
            string buildingName = building.Info.name;

            var data = CityEventsLoader.Instance.GetEventTemplate(_eventTypeDropdown.items[index], buildingName);
            if (data != null)
            {
                LoadEventData(data);
            }
        }

        private void LoadEventData(CityEventTemplate eventData)
        {
            _currentEventData = eventData;
            PopulateIncentiveList(eventData);
            UpdateCostDisplay();
            UpdateDatePreview();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Incentive list population (mirrors SetUp from UserEventCreationPanel)
        // ─────────────────────────────────────────────────────────────────────

        private void PopulateIncentiveList(CityEventTemplate eventData)
        {
            _incentiveList.rowsData.Clear();

            if (eventData?.Incentives == null)
            {
                _incentiveList.Refresh();
                return;
            }

            foreach (var incentive in eventData.Incentives)
            {
                var optionItem = new IncentiveOptionItem()
                {
                    cost = incentive.Cost,
                    description = incentive.Description,
                    negativeEffect = incentive.NegativeEffect,
                    positiveEffect = incentive.PositiveEffect,
                    returnCost = incentive.ReturnCost,
                    title = incentive.Name,
                    ticketCount = _ticketSlider != null ? _ticketSlider.value : 500f
                };

                optionItem.OnOptionItemChanged += UpdateCostDisplay;
                _incentiveList.rowsData.Add(optionItem);
            }

            _incentiveList.Refresh();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Cost display  (ported from UserEventCreationPanel.UpdateTotalCost)
        // ─────────────────────────────────────────────────────────────────────

        private void UpdateCostDisplay()
        {
            if (_ticketSlider != null)
            {
                string ticketsText = LocalizationProvider.Translate(TranslationKeys.VanillaEventTicketSliderLabel);
                _labelTickets?.text = string.Format("{0} {1}", _ticketSlider.value, ticketsText);
            }

            if (_currentEventData != null && _currentEventData.Costs != null)
            {
                _totalCost = 0f;
                _maxIncome = 0f;

                _totalCost += _currentEventData.Costs.Creation;
                _totalCost += (_ticketSlider?.value ?? 0f) * _currentEventData.Costs.PerHead;
                _maxIncome += (_ticketSlider?.value ?? 0f) * _currentEventData.Costs.Entry;

                foreach (IncentiveOptionItem item in _incentiveList.rowsData)
                {
                    _totalCost += item.cost * item.sliderValue;
                    _maxIncome += item.returnCost * item.sliderValue;
                }
            }

            if (_labelEventCost != null)
            {
                _labelEventCost.text = _totalCost.ToString(Settings.moneyFormat, LocaleManager.cultureInfo);
                _labelMaxProfit.text = _maxIncome.ToString(Settings.moneyFormat, LocaleManager.cultureInfo);
            }

            UpdateDatePreview();

            if (_createEventButton != null)
            {
                int adjustedCost = Mathf.RoundToInt(_totalCost * 100f);
                if (Singleton<EconomyManager>.instance.PeekResource(EconomyManager.Resource.Construction, adjustedCost) != adjustedCost)
                {
                    _createEventButton.Disable();
                }
                else
                {
                    _createEventButton.Enable();
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Schedule date dropdowns  (ported from UserEventCreationPanel)
        // ─────────────────────────────────────────────────────────────────────

        private void PopulateDateDropdowns()
        {
            var now = Singleton<SimulationManager>.instance.m_currentGameTime;

            _dropDay.items = [.. Enumerable.Range(1, 31).Select(i => i.ToString("D2"))];
            _dropDay.selectedIndex = now.Day - 1;

            _dropMonth.items = [.. Enumerable.Range(1, 12).Select(i => i.ToString("D2"))];
            _dropMonth.selectedIndex = now.Month - 1;

            _dropHour.items = [.. Enumerable.Range(0, 24).Select(i => i.ToString("D2"))];
            _dropMinute.items = [.. Enumerable.Range(0, 60).Select(i => i.ToString("D2"))];

            var adjusted = AdjustEventStartTime(now);
            _dropHour.selectedIndex = adjusted.Hour;
            _dropMinute.selectedIndex = adjusted.Minute;
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
                int startMonth = int.Parse(_dropMonth.selectedValue);

                string dayText = dropdown.items[value];
                int day = byte.Parse(dayText);

                int maxDay = DateTime.DaysInMonth(2, startMonth);
                if (day > maxDay)
                {
                    day = maxDay;
                }

                int startHour = byte.Parse(_dropHour.selectedValue);
                int startMinute = byte.Parse(_dropMinute.selectedValue);
                var startDateTime = new DateTime(year, startMonth, day, startHour, startMinute, 0);
                var dateTime = AdjustEventStartTime(startDateTime);
                _dropHour.selectedIndex = dateTime.Hour;
                _dropMinute.selectedIndex = dateTime.Minute;
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

                int startDay = int.Parse(_dropDay.selectedValue);

                int maxDay = DateTime.DaysInMonth(2, month);
                if (startDay > maxDay)
                {
                    startDay = maxDay;
                }

                _dropDay.selectedIndex = startDay - 1;

                int startHour = byte.Parse(_dropHour.selectedValue);
                int startMinute = byte.Parse(_dropMinute.selectedValue);
                var startDateTime = new DateTime(year, month, startDay, startHour, startMinute, 0);
                var dateTime = AdjustEventStartTime(startDateTime);
                _dropHour.selectedIndex = dateTime.Hour;
                _dropMinute.selectedIndex = dateTime.Minute;
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

                int startDay = int.Parse(_dropDay.selectedValue);
                int startMonth = int.Parse(_dropMonth.selectedValue);
                int startMinute = int.Parse(_dropMinute.selectedValue);

                string hourText = dropdown.items[value];
                int hour = int.Parse(hourText);

                var startDateTime = new DateTime(year, startMonth, startDay, hour, startMinute, 0);
                var dateTime = AdjustEventStartTime(startDateTime);
                _dropHour.selectedIndex = dateTime.Hour;
                _dropMinute.selectedIndex = dateTime.Minute;
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

                int startDay = int.Parse(_dropDay.selectedValue);
                int startMonth = int.Parse(_dropMonth.selectedValue);
                int startHour = int.Parse(_dropHour.selectedValue);

                string minuteText = dropdown.items[value];
                int minute = int.Parse(minuteText);

                var startDateTime = new DateTime(year, startMonth, startDay, startHour, minute, 0);
                var dateTime = AdjustEventStartTime(startDateTime);
                _dropHour.selectedIndex = dateTime.Hour;
                _dropMinute.selectedIndex = dateTime.Minute;
            }
            finally
            {
                _updatingSchedule = false;
            }
        }

        /// <summary>Clamps a proposed event start time to the configured earliest/latest hours.</summary>
        private static DateTime AdjustEventStartTime(DateTime eventStartTime)
        {
            float earliestHour;
            float latestHour;
            float hour = eventStartTime.Hour;
            float minute = eventStartTime.Minute;

            if (RealTimeConfig.IsWeekendEnabled && eventStartTime.IsWeekend())
            {
                earliestHour = RealTimeConfig.EarliestHourEventStartWeekend;
                latestHour = RealTimeConfig.LatestHourEventStartWeekend;
            }
            else
            {
                earliestHour = RealTimeConfig.EarliestHourEventStartWeekday;
                latestHour = RealTimeConfig.LatestHourEventStartWeekday;
            }

            if (eventStartTime.Hour >= latestHour)
            {
                hour = latestHour;
                minute = 0;
            }
            else if (eventStartTime.Hour < earliestHour)
            {
                hour = earliestHour;
            }

            return new DateTime(eventStartTime.Year, eventStartTime.Month, eventStartTime.Day, (int)hour, (int)minute, 0);
        }

        private void UpdateDatePreview()
        {
            if (_dropDay == null || _dropDay.items.Length == 0)
            {
                return;
            }

            int day = _dropDay.selectedIndex + 1;
            int month = _dropMonth.selectedIndex + 1;

            // Guard: items array bounds
            if (_dropHour.items.Length == 0 || _dropMinute.items.Length == 0)
            {
                return;
            }

            int hour = int.Parse(_dropHour.selectedValue);
            int minute = int.Parse(_dropMinute.selectedValue);

            // CS ignores leap years; year 2 is a stable non-leap reference
            int maxDay = DateTime.DaysInMonth(2, month);
            if (day > maxDay)
            {
                _dropDay.selectedIndex = maxDay - 1;
                day = maxDay;
            }

            _labelScheduledDate?.text = $"{day:D2}/{month:D2} {hour:D2}:{minute:D2}";
        }

        private DateTime BuildScheduledDateTime()
        {
            var now = Singleton<SimulationManager>.instance.m_currentGameTime;
            int day = int.Parse(_dropDay.selectedValue);
            int month = int.Parse(_dropMonth.selectedValue);
            int hour = int.Parse(_dropHour.selectedValue);
            int minute = int.Parse(_dropMinute.selectedValue);

            // CS ignores leap years — year 2 is always non-leap
            int safeDay = Math.Min(day, DateTime.DaysInMonth(2, month));

            var startTime = new DateTime(now.Year, month, safeDay, hour, minute, 0);

            // Auto-fix past dates to next year
            if (startTime <= now)
            {
                startTime = new DateTime(now.Year + 1, month, safeDay, hour, minute, 0);
            }

            return startTime;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Create event  (ported from UserEventCreationPanel.CreateEvent)
        // ─────────────────────────────────────────────────────────────────────

        private void CreateEvent()
        {
            if (_currentEventData == null || _buildingId == 0)
            {
                return;
            }

            var startTime = BuildScheduledDateTime();

            // Collect incentive selections
            var optionItems = new List<IncentiveOptionItem>();
            for (int i = 0; i < _incentiveList.rowsData.m_size; i++)
            {
                if (_incentiveList.rowsData.m_buffer[i] is IncentiveOptionItem item)
                {
                    optionItems.Add(item);
                }
            }

            var rtEvent = new RealTimeCityEvent(_currentEventData);
            rtEvent.SetUserConfiguration((int)(_ticketSlider?.value ?? 0f), _currentEventData.Costs?.Entry ?? 0f);

            foreach (var item in optionItems)
            {
                rtEvent.AddIncentive(item.title, item.sliderValue, item.cost, item.returnCost);
            }

            SimulationHandler.EventManager.AddEvent(rtEvent, _buildingId, startTime);
            RefreshUpcomingTab();
            _tabstrip.selectedIndex = TAB_UPCOMING;

            Log.Info($"Created {_currentEventData.EventName} for building {_buildingId} at {startTime}");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Refresh upcoming tab  (ported from LoadUpcomingEvents)
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshUpcomingTab()
        {
            // Destroy old row gameobjects
            foreach (var row in _upcomingRows)
            {
                if (row?.Root != null)
                {
                    Destroy(row.Root.gameObject);
                }
            }

            _upcomingRows.Clear();

            if (_upcomingEventList == null)
            {
                return;
            }

            var now = Singleton<SimulationManager>.instance.m_currentGameTime;
            var events = SimulationHandler.EventManager.GetUpcomingEventsForBuilding(_buildingId);

            if (events == null)
            {
                return;
            }

            foreach (var ev in events.Where(e => e.StartTime > now).OrderBy(e => e.StartTime))
            {
                var capturedEvent = ev;
                var row = UpcomingEventRow.Create(_upcomingEventList, ev, LocalizationProvider);
                row.OnRemoveClicked += () =>
                {
                    SimulationHandler.EventManager.RemoveEvent(capturedEvent);
                    RefreshUpcomingTab();
                };
                _upcomingRows.Add(row);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Refresh past tab
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshPastTab()
        {
            foreach (var row in _pastRows)
            {
                if (row?.Root != null)
                {
                    Destroy(row.Root.gameObject);
                }
            }

            _pastRows.Clear();

            if (_pastEventList == null)
            {
                return;
            }

            var events = SimulationHandler.EventManager.GetPastEventsForBuilding(_buildingId);
            if (events == null)
            {
                return;
            }

            foreach (var ev in events)
            {
                var row = PastEventRow.Create(_pastEventList, ev, LocalizationProvider);
                _pastRows.Add(row);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Translations  (ported from TranslationOnLanguageChanged)
        // ─────────────────────────────────────────────────────────────────────

        private void TranslationOnLanguageChanged()
        {
            if (_createEventButton != null)
            {
                _createEventButton.text = LocalizationProvider.Translate(TranslationKeys.VanillaEventCreateButton);
                _createEventButton.tooltip = LocalizationProvider.Translate(TranslationKeys.VanillaEventCreateButtonTooltip);
            }

            if (_dropDay != null)
            {
                _dropDay.tooltip = LocalizationProvider.Translate(TranslationKeys.VanillaEventDayDropDownLabelTooltip);
                _dropMonth.tooltip = LocalizationProvider.Translate(TranslationKeys.VanillaEventMonthDropDownLabelTooltip);
                _dropHour.tooltip = LocalizationProvider.Translate(TranslationKeys.VanillaEventHourDropDownLabelTooltip);
                _dropMinute.tooltip = LocalizationProvider.Translate(TranslationKeys.VanillaEventMinuteDropDownLabelTooltip);
            }

            // Update cost row title labels
            var costTitle = _eventConfigPanel?.Find<UILabel>("LabelEventCostTitle");
            if (costTitle != null)
            {
                costTitle.text = LocalizationProvider.Translate(TranslationKeys.VanillaEventTotalAmountLabel);
                costTitle.tooltip = LocalizationProvider.Translate(TranslationKeys.VanillaEventTotalAmountLabelTooltip);
            }

            var profitTitle = _eventConfigPanel?.Find<UILabel>("LabelMaxProfitTitle");
            if (profitTitle != null)
            {
                profitTitle.text = LocalizationProvider.Translate(TranslationKeys.VanillaEventTotalIncomeLabel);
                profitTitle.tooltip = LocalizationProvider.Translate(TranslationKeys.VanillaEventTotalIncomeLabelTooltip);
            }

            // Update tab headings
            var upcomingHeading = _containerUpcoming?.Find<UILabel>("LabelUpcomingHeading");
            upcomingHeading?.text = LocalizationProvider.Translate(TranslationKeys.VanillaEventUpcomingShowBtn);

            // Update tabstrip button labels
            if (_tabstrip?.tabs != null && _tabstrip.tabs.Count >= 3)
            {
                // Tab buttons are UIButton children
            }

            UpdateCostDisplay();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: create a small dropdown
        // ─────────────────────────────────────────────────────────────────────

        private static UIDropDown CreateDropdown(UIPanel parent, string name, float width, Vector3 pos)
        {
            var dd = parent.AddUIComponent<UIDropDown>();
            dd.name = name;
            dd.size = new Vector2(width, 24f);
            dd.relativePosition = pos;
            dd.listBackground = "GenericPanelDark";
            dd.itemHover = "ListItemHover";
            dd.itemHighlight = "ListItemHighlight";
            dd.textFieldPadding = new RectOffset(4, 0, 4, 0);
            dd.listHeight = 200;
            dd.itemHeight = 22;
            dd.textColor = Color.white;
            dd.atlas = TextureUtils.GetAtlas("Ingame");
            return dd;
        }
    }   
}
