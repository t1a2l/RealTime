// WorldInfoPanelPatch.cs

namespace RealTime.Patches
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using ColossalFramework;
    using ColossalFramework.Threading;
    using ColossalFramework.UI;
    using HarmonyLib;
    using RealTime.Config;
    using RealTime.Core;
    using RealTime.CustomAI;
    using RealTime.Events;
    using RealTime.GameConnection;
    using RealTime.Localization;
    using RealTime.Managers;
    using RealTime.Simulation;
    using RealTime.UI;
    using RealTime.Utils;
    using RealTime.Utils.UIUtils;
    using SkyTools.Localization;
    using SkyTools.Tools;
    using UnityEngine;

    /// <summary>
    /// A static class that provides the patch objects for the world info panel game methods.
    /// </summary>
    [HarmonyPatch]
    internal static class WorldInfoPanelPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeResidentAI<ResidentAI, Citizen> RealTimeResidentAI { get; set; }

        /// <summary>Gets or sets the customized citizen information panel.</summary>
        public static CustomCitizenInfoPanel CitizenInfoPanel { get; set; }

        /// <summary>Gets or sets the customized vehicle information panel.</summary>
        public static CustomVehicleInfoPanel VehicleInfoPanel { get; set; }

        /// <summary>Gets or sets the customized campus information panel.</summary>
        public static CustomCampusWorldInfoPanel CampusWorldInfoPanel { get; set; }

        /// <summary>Gets or sets the timeInfo.</summary>
        public static TimeInfo TimeInfo { get; set; }

        /// <summary>Gets or sets the game events data.</summary>
        public static RealTimeEventManager RealTimeEventManager { get; set; }

        /// <summary>Gets or sets the mod configuration.</summary>
        public static RealTimeConfig RealTimeConfig { get; set; }

        /// <summary>Gets or sets the mod localization.</summary>
        public static ILocalizationProvider LocalizationProvider { get; set; }

        /// <summary>Gets or sets the time adjustment simulation class instance.</summary>
        internal static TimeAdjustment TimeAdjustment { get; set; }

        /// <summary>car parking buildings.</summary>
        private static readonly string[] CarParkingBuildings = ["parking", "garage", "car park", "Parking", "Car Port", "Garage", "Car Park"];

        [HarmonyPatch]
        private sealed class WorldInfoPanel_UpdateBindings
        {
            [HarmonyPatch(typeof(WorldInfoPanel), "UpdateBindings")]
            [HarmonyPostfix]
            private static void Postfix(WorldInfoPanel __instance, ref InstanceID ___m_InstanceID)
            {
                switch (__instance)
                {
                    case CitizenWorldInfoPanel _:
                        CitizenInfoPanel?.UpdateCustomInfo(ref ___m_InstanceID, RealTimeConfig.DebugMode);
                        break;

                    case VehicleWorldInfoPanel _:
                        VehicleInfoPanel?.UpdateCustomInfo(ref ___m_InstanceID, RealTimeConfig.DebugMode);
                        break;

                    case CampusWorldInfoPanel _:
                        CampusWorldInfoPanel?.UpdateCustomInfo(ref ___m_InstanceID, RealTimeConfig.DebugMode);
                        break;
                }
            }
        }

        [HarmonyPatch]
        private sealed class TouristWorldInfoPanel_UpdateBindings
        {
            [HarmonyPatch(typeof(TouristWorldInfoPanel), "UpdateBindings")]
            [HarmonyPostfix]
            private static void UpdateBindings(TouristWorldInfoPanel __instance, ref InstanceID ___m_InstanceID, ref UILabel ___m_AgeWealth)
            {
                if (!Singleton<CitizenManager>.exists)
                {
                    return;
                }
                if (RealTimeConfig.DebugMode && ___m_InstanceID.Type == InstanceType.Citizen && ___m_InstanceID.Citizen != 0)
                {
                    var CurrentLocation = Singleton<CitizenManager>.instance.m_citizens.m_buffer[___m_InstanceID.Citizen].CurrentLocation;

                    var info = new StringBuilder(100);
                    float labelHeight = 0;
                    info.Append("CitizenId").Append(": ").Append(___m_InstanceID.Citizen);
                    info.AppendLine();
                    labelHeight += 14f;
                    info.Append("CurrentLocation").Append(": ").Append(CurrentLocation.ToString());
                    info.AppendLine();
                    labelHeight += 14f;

                    ___m_AgeWealth.text += Environment.NewLine + info;
                    ___m_AgeWealth.height = labelHeight;
                    __instance.component.height = 180f;
                }
            }
        }

        [HarmonyPatch]
        private sealed class FootballPanel_RefreshMatchInfo
        {
            [HarmonyPatch(typeof(FootballPanel), "RefreshMatchInfo")]
            [HarmonyPostfix]
            private static void Postfix(FootballPanel __instance, ref InstanceID ___m_InstanceID, ref UILabel ___m_nextMatchDate, ref UIPanel ___m_panelPastMatches)
            {
                int eventIndex = Singleton<BuildingManager>.instance.m_buildings.m_buffer[___m_InstanceID.Building].m_eventIndex;
                var eventData = Singleton<EventManager>.instance.m_events.m_buffer[eventIndex];
                var data = eventData;

                if (__instance.isCityServiceEnabled && (eventData.m_flags & EventData.Flags.Cancelled) == 0)
                {
                    if ((eventData.m_flags & EventData.Flags.Active) == 0 && (eventData.m_flags & EventData.Flags.Completed) == 0)
                    {
                        ___m_nextMatchDate.text = data.StartTime.ToString("dd/MM/yyyy HH:mm");
                    }
                }

                for (int i = 1; i <= 6; i++)
                {
                    var uISlicedSprite = ___m_panelPastMatches.Find<UISlicedSprite>("PastMatch " + i);
                    var uILabel2 = uISlicedSprite.Find<UILabel>("PastMatchDate");
                    ushort num4 = data.m_nextBuildingEvent;
                    if (i == 1 && (eventData.m_flags & EventData.Flags.Cancelled) != 0)
                    {
                        num4 = (ushort)eventIndex;
                    }
                    if (num4 != 0)
                    {
                        data = Singleton<EventManager>.instance.m_events.m_buffer[num4];
                        uILabel2.text = data.StartTime.ToString("dd/MM/yyyy HH:mm");
                    }
                }
            }
        }

        [HarmonyPatch]
        private static class VarsitySportsArenaPanelPatch
        {
            [HarmonyPatch(typeof(VarsitySportsArenaPanel), "RefreshPastMatches")]
            [HarmonyPostfix]
            private static void RefreshPastMatches(int eventIndex, EventData upcomingEvent, EventData currentEvent, ref UIPanel ___m_panelPastMatches)
            {
                var originalTime = new DateTime(currentEvent.m_startFrame * SimulationManager.instance.m_timePerFrame.Ticks + SimulationManager.instance.m_timeOffsetTicks);
                currentEvent.m_startFrame = SimulationManager.instance.TimeToFrame(originalTime);

                for (int i = 1; i <= 6; i++)
                {
                    var uISlicedSprite = ___m_panelPastMatches.Find<UISlicedSprite>("PastMatch " + i);
                    var uILabel2 = uISlicedSprite.Find<UILabel>("PastMatchDate");
                    ushort num4 = currentEvent.m_nextBuildingEvent;
                    if (i == 1 && (upcomingEvent.m_flags & EventData.Flags.Cancelled) != 0)
                    {
                        num4 = (ushort)eventIndex;
                    }
                    if (num4 != 0)
                    {
                        currentEvent = Singleton<EventManager>.instance.m_events.m_buffer[num4];
                        uILabel2.text = currentEvent.StartTime.ToString("dd/MM/yyyy HH:mm");
                    }
                }
            }

            [HarmonyPatch(typeof(VarsitySportsArenaPanel), "RefreshNextMatchDates")]
            [HarmonyPostfix]
            private static void RefreshNextMatchDates(VarsitySportsArenaPanel __instance, EventData upcomingEvent, EventData currentEvent, ref UILabel ___m_nextMatchDate)
            {
                if (__instance.isCityServiceEnabled && (upcomingEvent.m_flags & EventData.Flags.Cancelled) == 0)
                {
                    if ((upcomingEvent.m_flags & EventData.Flags.Active) == 0 && (upcomingEvent.m_flags & EventData.Flags.Completed) == 0)
                    {
                        ___m_nextMatchDate.text = currentEvent.StartTime.ToString("dd/MM/yyyy HH:mm");
                    }
                }
            }
        }

        [HarmonyPatch]
        private static class HotelWorldInfoPanelPatch
        {
            [HarmonyPatch(typeof(HotelWorldInfoPanel), "UpdateBindings")]
            [HarmonyPostfix]
            private static void Postfix(HotelWorldInfoPanel __instance, ref InstanceID ___m_InstanceID, ref UILabel ___m_labelEventTimeLeft, ref UIPanel ___m_panelEventInactive)
            {
                ushort building = ___m_InstanceID.Building;
                var hotel_event = RealTimeEventManager.GetCityEvent(building);
                var event_state = RealTimeEventManager.GetEventState(building, DateTime.MaxValue);

                if(hotel_event != null)
                {
                    if (event_state == CityEventState.Upcoming)
                    {
                        if (hotel_event.StartTime.Date < TimeInfo.Now.Date)
                        {
                            string event_start = hotel_event.StartTime.ToString("dd/MM/yyyy HH:mm");
                            ___m_labelEventTimeLeft.text = "Event starts at " + event_start;
                        }
                        else
                        {
                            string event_start = hotel_event.StartTime.ToString("HH:mm");
                            ___m_labelEventTimeLeft.text = "Event starts at " + event_start;
                        }
                    }
                    else if (event_state == CityEventState.Ongoing)
                    {
                        if (TimeInfo.Now.Date < hotel_event.EndTime.Date)
                        {
                            string event_end = hotel_event.EndTime.ToString("dd/MM/yyyy HH:mm");
                            ___m_labelEventTimeLeft.text = "Event ends at " + event_end;
                        }
                        else
                        {
                            string event_end = hotel_event.EndTime.ToString("HH:mm");
                            ___m_labelEventTimeLeft.text = "Event ends at " + event_end;
                        }
                    }
                    else if (event_state == CityEventState.Finished)
                    {
                        ___m_labelEventTimeLeft.text = "Event ended";
                    }
                }
                var buttonStartEvent = ___m_panelEventInactive.Find<UIButton>("ButtonStartEvent");
                buttonStartEvent?.text = "Schedule";
            }

            [HarmonyPatch(typeof(HotelWorldInfoPanel), "SelectEvent")]
            [HarmonyPostfix]
            private static void SelectEvent(HotelWorldInfoPanel __instance, int index, ref UILabel ___m_labelEventDuration)
            {
                if (___m_labelEventDuration && ___m_labelEventDuration.text.Contains("days"))
                {
                    ___m_labelEventDuration.text = ___m_labelEventDuration.text.Replace("days", "hours");
                }
            }

            private static bool IsHotelEventActiveOrUpcoming(ushort buildingID, ref Building buildingData) => buildingData.m_eventIndex != 0 || RealTimeEventManager.GetCityEvent(buildingID) != null;
        }

        [HarmonyPatch]
        private static class FestivalPanelPatch
        {
            [HarmonyPatch(typeof(FestivalPanel), "RefreshCurrentConcert")]
            [HarmonyPostfix]
            private static void RefreshCurrentConcert(UIPanel panel, EventData concert)
            {
                var current_concert = RealTimeEventManager.GetCityEvent(concert.m_building);
                if (current_concert != null)
                {
                    panel.Find<UILabel>("Date").text = current_concert.StartTime.ToString("dd/MM/yyyy HH:mm");
                }
            }

            [HarmonyPatch(typeof(FestivalPanel), "RefreshFutureConcert")]
            [HarmonyPostfix]
            private static void RefreshFutureConcert(UIPanel panel, EventManager.FutureEvent concert) => panel.Find<UILabel>("Date").text = concert.m_startTime.ToString("dd/MM/yyyy HH:mm");
        }

        [HarmonyPatch]
        private sealed class LivingCreatureWorldInfoPanelPatch
        {
            private static UIButton m_clearScheduleButton;

            [HarmonyPatch(typeof(LivingCreatureWorldInfoPanel), "OnSetTarget")]
            [HarmonyPostfix]
            private static void OnSetTarget(ref InstanceID ___m_InstanceID)
            {
                if (___m_InstanceID.Citizen != 0)
                {
                    if (m_clearScheduleButton == null)
                    {
                        CreateClearScheduleButton();
                    }
                    if (RealTimeConfig.DebugMode)
                    {
                        m_clearScheduleButton.Show();
                    }
                    else
                    {
                        m_clearScheduleButton.Hide();
                    }
                }
            }

            private static void CreateClearScheduleButton()
            {
                var citizenInfoPanel = GameObject.Find("(Library) CitizenWorldInfoPanel").GetComponent<CitizenWorldInfoPanel>();
                m_clearScheduleButton = UIButtons.CreateButton(citizenInfoPanel.component, -10f, 90f, "ClearSchedule", "", "Clear the citizen schedule", 30, 30);
                m_clearScheduleButton.AlignTo(citizenInfoPanel.component, UIAlignAnchor.TopRight);
                m_clearScheduleButton.relativePosition += new Vector3(-10f, 90f);

                m_clearScheduleButton.atlas = TextureUtils.GetAtlas("ClearScheduleButton");
                m_clearScheduleButton.normalFgSprite = "ClearSchedule";
                m_clearScheduleButton.disabledFgSprite = "ClearSchedule";
                m_clearScheduleButton.focusedFgSprite = "ClearSchedule";
                m_clearScheduleButton.hoveredFgSprite = "ClearSchedule";
                m_clearScheduleButton.pressedFgSprite = "ClearSchedule";
                m_clearScheduleButton.eventClicked += ClearSchedule;
                citizenInfoPanel.component.AttachUIComponent(m_clearScheduleButton.gameObject);
            }

            public static void ClearSchedule(UIComponent c, UIMouseEventParameter eventParameter)
            {
                uint citizenID = WorldInfoPanel.GetCurrentInstanceID().Citizen;
                var citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].GetCitizenInfo(citizenID);
                if (citizen.GetAI() is ResidentAI)
                {
                    RealTimeResidentAI.ClearCitizenSchedule(citizenID);
                }
            }
        }

        [HarmonyPatch]
        private sealed class BuildingWorldInfoPanelPatch
        {
            private static BuildingOperationHoursPanel CityServiceOperationHoursPanel;

            private static BuildingOperationHoursPanel UniqueFactoryOperationHoursPanel;

            private static BuildingOperationHoursPanel WarehouseOperationHoursPanel;

            private static BuildingOperationHoursPanel ZonedBuildingOperationHoursPanel;

            private static OperationHoursSettingsCheckBoxPanel CityServiceOperationHoursCheckBoxPanel;

            private static OperationHoursSettingsCheckBoxPanel UniqueFactoryOperationHoursCheckBoxPanel;

            private static OperationHoursSettingsCheckBoxPanel WarehouseOperationHoursCheckBoxPanel;

            private static OperationHoursSettingsCheckBoxPanel ZonedBuildingOperationHoursCheckBoxPanel;

            [HarmonyPatch(typeof(BuildingWorldInfoPanel), "OnSetTarget")]
            [HarmonyPostfix]
            private static void OnSetTarget(BuildingWorldInfoPanel __instance, InstanceID ___m_InstanceID) => OperationHoursUIUpdate(__instance, ___m_InstanceID.Building);

            private static void OperationHoursUIUpdate(BuildingWorldInfoPanel instance, ushort buildingID)
            {
                float checkBoxXposition = 340f;
                float checkBoxYposition = 16f;
                float panelHeight = 0f;

                var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];
                var buildingAI = building.Info.GetAI();

                if (instance is CityServiceWorldInfoPanel)
                {
                    var m_cityServiceWorldInfoPanel = GameObject.Find("(Library) CityServiceWorldInfoPanel").GetComponent<CityServiceWorldInfoPanel>();
                    var wrapper = m_cityServiceWorldInfoPanel?.Find("Wrapper");
                    var mainSectionPanel = wrapper?.Find("MainSectionPanel");
                    var mainBottom = mainSectionPanel?.Find("MainBottom");
                    var buttonPanels = mainBottom?.Find("ButtonPanels").GetComponent<UIPanel>();

                    if (buttonPanels == null)
                    {
                        return;
                    }

                    if (CityServiceOperationHoursPanel == null)
                    {
                        CityServiceOperationHoursPanel = m_cityServiceWorldInfoPanel.component.AddUIComponent<BuildingOperationHoursPanel>();
                    }

                    if (CityServiceOperationHoursCheckBoxPanel == null)
                    {
                        CityServiceOperationHoursCheckBoxPanel = buttonPanels.AddUIComponent<OperationHoursSettingsCheckBoxPanel>();
                    }

                    if (buildingAI is ExtractingFacilityAI || buildingAI is ProcessingFacilityAI)
                    {
                        checkBoxXposition = 360f;
                        checkBoxYposition = 0f;
                        panelHeight = 40f;
                    }

                    CityServiceOperationHoursPanel.UpdateData(panelHeight, LocalizationProvider);
                    CityServiceOperationHoursCheckBoxPanel.UpdateData(checkBoxXposition, checkBoxYposition, LocalizationProvider, CityServiceOperationHoursPanel);

                    OperationHoursUIVisibility(buildingID, CityServiceOperationHoursPanel, CityServiceOperationHoursCheckBoxPanel, checkBoxXposition, checkBoxYposition);
                }
                if (instance is UniqueFactoryWorldInfoPanel)
                {
                    var m_uniqueFactoryWorldInfoPanel = GameObject.Find("(Library) UniqueFactoryWorldInfoPanel").GetComponent<UniqueFactoryWorldInfoPanel>();
                    var IncomeExpensesSection = m_uniqueFactoryWorldInfoPanel?.Find("IncomeExpensesSection").GetComponent<UIPanel>();
                    if (IncomeExpensesSection == null)
                    {
                        return;
                    }

                    if (UniqueFactoryOperationHoursPanel == null)
                    {
                        UniqueFactoryOperationHoursPanel = m_uniqueFactoryWorldInfoPanel.component.AddUIComponent<BuildingOperationHoursPanel>();
                    }

                    if (UniqueFactoryOperationHoursCheckBoxPanel == null)
                    {
                        UniqueFactoryOperationHoursCheckBoxPanel = IncomeExpensesSection.AddUIComponent<OperationHoursSettingsCheckBoxPanel>();
                    }

                    checkBoxXposition = 320f;
                    checkBoxYposition = 0f;
                    panelHeight = 0f;

                    UniqueFactoryOperationHoursPanel.UpdateData(panelHeight, LocalizationProvider);
                    UniqueFactoryOperationHoursCheckBoxPanel.UpdateData(checkBoxXposition, checkBoxYposition, LocalizationProvider, UniqueFactoryOperationHoursPanel);

                    OperationHoursUIVisibility(buildingID, UniqueFactoryOperationHoursPanel, UniqueFactoryOperationHoursCheckBoxPanel, checkBoxXposition, checkBoxYposition);
                }
                if(instance is WarehouseWorldInfoPanel)
                {
                    var m_warehouseWorldInfoPanel = GameObject.Find("(Library) WarehouseWorldInfoPanel").GetComponent<WarehouseWorldInfoPanel>();
                    var WarehousePanel = GameObject.Find("(Library) WarehouseWorldInfoPanel").GetComponent<UIPanel>();
                    if (WarehousePanel == null)
                    {
                        return;
                    }

                    if (WarehouseOperationHoursPanel == null)
                    {
                        WarehouseOperationHoursPanel = m_warehouseWorldInfoPanel.component.AddUIComponent<BuildingOperationHoursPanel>();
                    }

                    if (WarehouseOperationHoursCheckBoxPanel == null)
                    {
                        WarehouseOperationHoursCheckBoxPanel = WarehousePanel.AddUIComponent<OperationHoursSettingsCheckBoxPanel>();
                    }

                    checkBoxXposition = 320f;
                    checkBoxYposition = 500f;
                    panelHeight = 0f;

                    if (buildingAI is WarehouseAI warehouse && warehouse.m_storageType == TransferManager.TransferReason.None)
                    {
                        checkBoxYposition = 550f;
                    }

                    WarehouseOperationHoursPanel.UpdateData(panelHeight, LocalizationProvider);
                    WarehouseOperationHoursCheckBoxPanel.UpdateData(checkBoxXposition, checkBoxYposition, LocalizationProvider, WarehouseOperationHoursPanel);

                    OperationHoursUIVisibility(buildingID, WarehouseOperationHoursPanel, WarehouseOperationHoursCheckBoxPanel, checkBoxXposition, checkBoxYposition);
                }
                if(instance is ZonedBuildingWorldInfoPanel)
                {
                    var m_zonedBuildingWorldInfoPanel = GameObject.Find("(Library) ZonedBuildingWorldInfoPanel").GetComponent<ZonedBuildingWorldInfoPanel>();
                    var makeHistoricalPanel = m_zonedBuildingWorldInfoPanel.Find("MakeHistoricalPanel").GetComponent<UIPanel>();
                    if (makeHistoricalPanel == null)
                    {
                        return;
                    }

                    if (ZonedBuildingOperationHoursPanel == null)
                    {
                        ZonedBuildingOperationHoursPanel = m_zonedBuildingWorldInfoPanel.component.AddUIComponent<BuildingOperationHoursPanel>();
                    }

                    if (ZonedBuildingOperationHoursCheckBoxPanel == null)
                    {
                        ZonedBuildingOperationHoursCheckBoxPanel = makeHistoricalPanel.AddUIComponent<OperationHoursSettingsCheckBoxPanel>();
                    }

                    checkBoxXposition = 340f;
                    checkBoxYposition = 6f;
                    panelHeight = 0f;

                    ZonedBuildingOperationHoursPanel.UpdateData(panelHeight, LocalizationProvider);
                    ZonedBuildingOperationHoursCheckBoxPanel.UpdateData(checkBoxXposition, checkBoxYposition, LocalizationProvider, ZonedBuildingOperationHoursPanel);

                    OperationHoursUIVisibility(buildingID, ZonedBuildingOperationHoursPanel, ZonedBuildingOperationHoursCheckBoxPanel, checkBoxXposition, checkBoxYposition);
                }
            }

            private static void OperationHoursUIVisibility(ushort buildingID, BuildingOperationHoursPanel buildingOperationHoursPanel, OperationHoursSettingsCheckBoxPanel operationHoursSettingsCheckBoxPanel, float checkBoxXposition, float checkBoxYposition)
            {
                var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];
                var buildingAI = building.Info.GetAI();
                var service = building.Info.GetService();
                var sub_service = building.Info.GetSubService();
                var DistrictInstance = Singleton<DistrictManager>.instance;
                bool IsAllowedZonedCommercial = buildingAI is CommercialBuildingAI && service == ItemClass.Service.Commercial && !BuildingManagerConnection.IsHotel(buildingID);
                bool IsAllowedZonedGeneral = buildingAI is IndustrialBuildingAI || buildingAI is IndustrialExtractorAI || buildingAI is OfficeBuildingAI;
                bool isAllowedCityService = buildingAI is BankOfficeAI || buildingAI is PostOfficeAI || buildingAI is SaunaAI || buildingAI is TourBuildingAI || buildingAI is MonumentAI || buildingAI is MarketAI || buildingAI is LibraryAI;
                bool isAllowedParkBuilding = buildingAI is ParkBuildingAI && DistrictInstance.GetPark(building.m_position) == 0 && !CarParkingBuildings.Any(s => building.Info.name.Contains(s));
                bool isAllowedIndustriesBuilding = buildingAI is ExtractingFacilityAI || buildingAI is ProcessingFacilityAI || buildingAI is UniqueFactoryAI || buildingAI is WarehouseAI || buildingAI is WarehouseStationAI;
                bool isPark = buildingAI is ParkAI && !CarParkingBuildings.Any(s => building.Info.name.Contains(s));
                // dont allow hotels
                if (IsAllowedZonedCommercial || IsAllowedZonedGeneral || isAllowedCityService || isAllowedParkBuilding || isPark || isAllowedIndustriesBuilding)
                {
                    var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);
                    buildingOperationHoursPanel.RefreshData(buildingID, buildingWorkTime);
                    operationHoursSettingsCheckBoxPanel.RefreshData(checkBoxXposition, checkBoxYposition, buildingOperationHoursPanel);
                }
                else
                {
                    buildingOperationHoursPanel.Hide();
                    operationHoursSettingsCheckBoxPanel.Hide();
                }
            }

        }

        [HarmonyPatch]
        private sealed class CityServiceWorldInfoPanelPatch
        {
            private static UILabel s_visitorsLabel;

            private static UIButton m_endYearButton;

            // private static UIButton m_openUserEventCreationPanelButton;

            // private static UserEventCreationPanel userEventCreationPanel;

            private static UIDropDown m_commercialBuildingTypeDropdown;

            private static bool s_updatingDropdown;

            [HarmonyPatch(typeof(CityServiceWorldInfoPanel), "OnSetTarget")]
            [HarmonyPostfix]
            public static void OnSetTarget()
            {
                // userEventCreationPanel == null || m_openUserEventCreationPanelButton == null
                if (s_visitorsLabel == null || m_endYearButton == null || m_commercialBuildingTypeDropdown == null)
                {
                    CreateUI();
                }

                // Currently selected building.
                ushort building = WorldInfoPanel.GetCurrentInstanceID().Building;

                // Local references.
                // var buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                // var buildingData = buildingBuffer[building];
                // var buildingInfo = buildingData.Info;

                //if (CityEventsLoader.Instance.GetEventTemplates(buildingInfo.name).Count > 0)
                //{
                //    m_openUserEventCreationPanelButton.Show();
                //}
                //else
                //{
                //    m_openUserEventCreationPanelButton.Hide();
                //}

                // show commercial building type dropdown only for generic commercial buildings that are not hotels
                if (BuildingManagerConnection.IsAllowedCommercialBuildingType(building) && CommercialBuildingTypesManager.CommercialBuildingTypeExist(building))
                {
                    m_commercialBuildingTypeDropdown.Show();
                }
                else
                {
                    m_commercialBuildingTypeDropdown.Hide();
                }

                CommercialBuildingTypesManager.CommercialBuildingTypeDropdownVisibility(building, ref m_commercialBuildingTypeDropdown, ref s_updatingDropdown);
            }

            [HarmonyPatch(typeof(CityServiceWorldInfoPanel), "UpdateBindings")]
            [HarmonyPostfix]
            public static void UpdateBindings()
            {
                // Currently selected building.
                ushort building = WorldInfoPanel.GetCurrentInstanceID().Building;

                // Local references.
                var buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                var buildingData = buildingBuffer[building];
                var buildingInfo = buildingData.Info;

                // Is this a cafeteria or a gymnasium
                if (buildingInfo.GetAI() is CampusBuildingAI campusBuildingAI && (buildingInfo.name.Contains("Cafeteria") || buildingInfo.name.Contains("Gymnasium")))
                {
                    // Show the label
                    s_visitorsLabel.Show();

                    // Get current visitor count.
                    int aliveCount = 0, totalCount = 0;
                    Citizen.BehaviourData behaviour = default;
                    GetVisitBehaviour(campusBuildingAI, building, ref buildingBuffer[building], ref behaviour, ref aliveCount, ref totalCount);

                    // Display visitor count.
                    s_visitorsLabel.text = aliveCount.ToString() + " / 300 visitors";
                }
                else
                {
                    // Not a cafeteria or a gymnasium hide the label
                    s_visitorsLabel.Hide();
                }

                // hide end year button if not in debug mode
                if (RealTimeConfig.DebugMode && buildingInfo.GetAI() is MainCampusBuildingAI)
                {
                    m_endYearButton.width = 133f;
                    m_endYearButton.height = 19.5f;
                    m_endYearButton.Show();
                }
                else
                {
                    m_endYearButton.Hide();
                }

                // Is this a main campus building and the academic year can end and the year has not already ended
                if (buildingInfo.GetAI() is MainCampusBuildingAI && AcademicYearManager.CanAcademicYearEndorBegin(TimeInfo)
                    && AcademicYearManager.MainCampusBuildingExist(building))
                {
                    var academicYearData = AcademicYearManager.GetAcademicYearData(building);
                    if (academicYearData.DidLastYearEnd)
                    {
                        m_endYearButton.Enable();
                    }
                }
                else
                {
                    m_endYearButton.Disable();
                }
            }

            [HarmonyReversePatch]
            [HarmonyPatch(typeof(CommonBuildingAI), "GetVisitBehaviour")]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void GetVisitBehaviour(object instance, ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount)
            {
                string message = "GetVisitBehaviour reverse Harmony patch wasn't applied";
                Debug.LogError(message);
                throw new NotImplementedException(message);
            }

            private static void CreateUI()
            {
                var m_cityServiceWorldInfoPanel = GameObject.Find("(Library) CityServiceWorldInfoPanel").GetComponent<CityServiceWorldInfoPanel>();
                var wrapper = m_cityServiceWorldInfoPanel?.Find("Wrapper");
                var mainSectionPanel = wrapper?.Find("MainSectionPanel");
                var mainBottom = mainSectionPanel?.Find("MainBottom");
                var buttonPanels = mainBottom?.Find("ButtonPanels").GetComponent<UIPanel>();

                if (buttonPanels == null)
                {
                    return;
                }

                //if (userEventCreationPanel == null)
                //{
                //    userEventCreationPanel = m_cityServiceWorldInfoPanel.component.AddUIComponent<UserEventCreationPanel>();
                //}

                if (s_visitorsLabel == null)
                {
                    s_visitorsLabel = UILabels.CreatePositionedLabel(buttonPanels, 65f, 280f, "VisitorsLabel", "Visitors", textScale: 0.75f);
                    s_visitorsLabel.textColor = new Color32(185, 221, 254, 255);
                    s_visitorsLabel.font = Resources.FindObjectsOfTypeAll<UIFont>().FirstOrDefault(f => f.name == "OpenSans-Regular");
                    s_visitorsLabel.relativePosition = new Vector2(200f, 26f);
                }

                if (m_endYearButton == null)
                {
                    string endYearButtonText = LocalizationProvider.Translate(TranslationKeys.AcademicYearEndYearButtonText);
                    string endYearButtonTooltipText = LocalizationProvider.Translate(TranslationKeys.AcademicYearEndYearButtonTooltip);
                    m_endYearButton = UIButtons.CreateButton(buttonPanels, 133f, 19.5f, "EndYear", endYearButtonText, endYearButtonTooltipText);
                    m_endYearButton.textVerticalAlignment = UIVerticalAlignment.Top;
                    m_endYearButton.relativePosition = new Vector2(150f, 22.5f);
                    m_endYearButton.textScale = 0.75f;
                    m_endYearButton.normalBgSprite = "ButtonMenu";
                    m_endYearButton.disabledBgSprite = "ButtonMenuDisabled";
                    m_endYearButton.pressedBgSprite = "ButtonMenuPressed";
                    m_endYearButton.hoveredBgSprite = "ButtonMenuHovered";
                    m_endYearButton.textColor = new Color32(255, 255, 255, 255);
                    m_endYearButton.disabledTextColor = new Color32(142, 142, 142, 255);
                    m_endYearButton.pressedTextColor = new Color32(255, 255, 255, 255);
                    m_endYearButton.hoveredTextColor = new Color32(255, 255, 255, 255);
                    m_endYearButton.focusedTextColor = new Color32(255, 255, 255, 255);
                    m_endYearButton.eventClicked += EndAcademicYear;
                }

                CommercialBuildingTypesManager.CreateUI(buttonPanels, ref m_commercialBuildingTypeDropdown, 220f, 5f);
                m_commercialBuildingTypeDropdown.eventSelectedIndexChanged += delegate (UIComponent uiComponent, int value)
                {
                    ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
                    CommercialBuildingTypesManager.OnCommercialBuildingTypeDropdownIndexChanged(value, buildingID, s_updatingDropdown);
                };

                //if(m_openUserEventCreationPanelButton == null)
                //{
                //    string eventButtonText = LocalizationProvider.Translate(TranslationKeys.VanillaEventSelectEventButton);
                //    string eventButtonTooltipText = LocalizationProvider.Translate(TranslationKeys.VanillaEventSelectEventButtonTooltip);
                //    m_openUserEventCreationPanelButton = UIButtons.CreateButton(buttonPanels, 133f, 19.5f, "OpenUserEventCreationPanelButton", eventButtonText, eventButtonTooltipText, 100f, 20f);
                //    m_openUserEventCreationPanelButton.textVerticalAlignment = UIVerticalAlignment.Top;
                //    m_openUserEventCreationPanelButton.relativePosition = new Vector2(150f, 22.5f);
                //    m_openUserEventCreationPanelButton.textScale = 0.75f;
                //    m_openUserEventCreationPanelButton.normalBgSprite = "ButtonMenu";
                //    m_openUserEventCreationPanelButton.disabledBgSprite = "ButtonMenuDisabled";
                //    m_openUserEventCreationPanelButton.pressedBgSprite = "ButtonMenuPressed";
                //    m_openUserEventCreationPanelButton.hoveredBgSprite = "ButtonMenuHovered";
                //    m_openUserEventCreationPanelButton.textColor = new Color32(255, 255, 255, 255);
                //    m_openUserEventCreationPanelButton.disabledTextColor = new Color32(142, 142, 142, 255);
                //    m_openUserEventCreationPanelButton.pressedTextColor = new Color32(255, 255, 255, 255);
                //    m_openUserEventCreationPanelButton.hoveredTextColor = new Color32(255, 255, 255, 255);
                //    m_openUserEventCreationPanelButton.focusedTextColor = new Color32(255, 255, 255, 255);
                //    m_openUserEventCreationPanelButton.eventClicked += OnOpenUserEventCreationPanelButtonClicked;
                //}
            }

            private static void EndAcademicYear(UIComponent c, UIMouseEventParameter eventParameter)
            {
                ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;

                var buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                var buildingData = buildingBuffer[buildingID];
                ref var eventData = ref Singleton<EventManager>.instance.m_events.m_buffer[buildingData.m_eventIndex];

                if (eventData.Info.GetAI() is AcademicYearAI)
                {
                    var academicYearData = AcademicYearManager.GetAcademicYearData(buildingID);
                    academicYearData.ActualAcademicYearEndFrame = SimulationManager.instance.m_currentFrameIndex;
                    AcademicYearManager.SetAcademicYearData(buildingID, academicYearData);
                    eventData.m_flags = (eventData.m_flags & ~EventData.Flags.Active) | EventData.Flags.Completed | EventData.Flags.Disorganizing;
                    Singleton<EventManager>.instance.m_globalEventDataDirty = true;
                    byte park = Singleton<DistrictManager>.instance.GetPark(Singleton<BuildingManager>.instance.m_buildings.m_buffer[eventData.m_building].m_position);
                    if (park != 0 && Singleton<DistrictManager>.instance.m_parks.m_buffer[park].m_isMainCampus)
                    {
                        OnAcademicYearEnded();
                        m_endYearButton.Disable();
                    }

                }
            }

            private static void OnAcademicYearEnded()
            {
                var instance = DistrictManager.instance;
                var m_activeCampusAreas = (List<byte>)typeof(DistrictManager).GetField("m_activeCampusAreas", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(instance);
                m_activeCampusAreas.Clear();
                for (byte b = 0; b < instance.m_parks.m_buffer.Length; b++)
                {
                    if (instance.m_parks.m_buffer[b].m_flags != 0 && instance.m_parks.m_buffer[b].IsCampus && instance.m_parks.m_buffer[b].m_mainGate != 0)
                    {
                        instance.m_parks.m_buffer[b].OnAcademicYearEnded(b);
                        m_activeCampusAreas.Add(b);
                    }
                }
                var academicYearReportPanel = UIView.library.Get<AcademicYearReportPanel>("AcademicYearReportPanel");
                typeof(DistrictManager).GetField("m_activeCampusAreas", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).SetValue(instance, m_activeCampusAreas);
                academicYearReportPanel.PopupPanel(m_activeCampusAreas, 0, wasTriggeredByButton: true);
                var campusWorldInfoPanel = UIView.library.Get<CampusWorldInfoPanel>("CampusWorldInfoPanel");
                if (campusWorldInfoPanel.component.isVisible)
                {
                    campusWorldInfoPanel.OnAcademicYearEnded();
                }
                ;
            }

            //private static void OnOpenUserEventCreationPanelButtonClicked(UIComponent c, UIMouseEventParameter p)
            //{
            //    ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;

            //    if (buildingID == 0)
            //    {
            //        return;
            //    }

            //    var buildingMgr = Singleton<BuildingManager>.instance;
            //    var building = buildingMgr.m_buildings.m_buffer[buildingID];
            //    string buildingName = building.Info.name;

            //    var templates = CityEventsLoader.Instance.GetEventTemplates(buildingName);
            //    userEventCreationPanel.Show(buildingID, templates);
            //}
        }

        [HarmonyPatch]
        private sealed class ZonedBuildingWorldInfoPanelPatch
        {
            private static UILabel s_hotelLabel;

            private static UIDropDown m_commercialBuildingTypeDropdown;

            private static bool s_updatingDropdown;

            private static UIButton m_realisticPopulationButton;

            [HarmonyPatch(typeof(ZonedBuildingWorldInfoPanel), "OnSetTarget")]
            [HarmonyPostfix]
            public static void OnSetTarget()
            {
                if (s_hotelLabel == null || m_commercialBuildingTypeDropdown == null)
                {
                    CreateUI();
                }

                if (RealTimeCore.ApplyRealisticPopulationButtonPatch && m_realisticPopulationButton != null)
                {
                    m_realisticPopulationButton.relativePosition = new Vector2(280f, 80f);
                }

                // Currently selected building.
                ushort building = WorldInfoPanel.GetCurrentInstanceID().Building;

                if (BuildingManagerConnection.IsHotel(building))
                {
                    // Hotel show the label
                    s_hotelLabel.Show();
                }
                else
                {
                    // Not a hotel hide the label
                    s_hotelLabel.Hide();
                }

                CommercialBuildingTypesManager.CommercialBuildingTypeDropdownVisibility(building, ref m_commercialBuildingTypeDropdown, ref s_updatingDropdown);
            }

            [HarmonyPatch(typeof(ZonedBuildingWorldInfoPanel), "UpdateBindings")]
            [HarmonyPostfix]
            private static void UpdateBindings()
            {
                // Currently selected building.
                ushort building = WorldInfoPanel.GetCurrentInstanceID().Building;

                // Local references.
                var buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                var buildingData = buildingBuffer[building];
                var buildingInfo = buildingData.Info;

                // Is this a hotel building?
                if (buildingInfo.GetAI() is CommercialBuildingAI && BuildingManagerConnection.IsHotel(building))
                {
                    // Display hotel rooms ocuppied count out of max hotel rooms.
                    s_hotelLabel.text = buildingData.m_roomUsed + " / " + buildingData.m_roomMax + " Rooms";
                }
            }

            private static void CreateUI()
            {
                var m_zonedBuildingWorldInfoPanel = UIView.library.Get<ZonedBuildingWorldInfoPanel>("ZonedBuildingWorldInfoPanel");

                if (m_zonedBuildingWorldInfoPanel == null)
                {
                    return;
                }

                if(RealTimeCore.ApplyRealisticPopulationButtonPatch && m_realisticPopulationButton == null)
                {
                    foreach (var child in m_zonedBuildingWorldInfoPanel.component.components)
                    {
                        if (child is UIButton button && button.text.Trim() == "Realistic Population")
                        {
                            m_realisticPopulationButton = button;
                            break;
                        }
                    }
                }

                if (s_hotelLabel == null)
                {
                    // Add current visitor count label.
                    s_hotelLabel = UILabels.CreatePositionedLabel(m_zonedBuildingWorldInfoPanel.component, 65f, 280f, "HotelLabel", "Rooms Ocuppied", textScale: 0.75f);
                    s_hotelLabel.textColor = new Color32(185, 221, 254, 255);
                    s_hotelLabel.font = Resources.FindObjectsOfTypeAll<UIFont>().FirstOrDefault(f => f.name == "OpenSans-Regular");

                    // Position under existing Highly Educated workers count row in line with total workplace count label.
                    var situationLabel = m_zonedBuildingWorldInfoPanel.Find("WorkSituation");
                    var workerLabel = m_zonedBuildingWorldInfoPanel.Find("HighlyEducatedWorkers");
                    if (situationLabel != null && workerLabel != null)
                    {
                        s_hotelLabel.absolutePosition = new Vector2(situationLabel.absolutePosition.x + 200f, workerLabel.absolutePosition.y + 25f);
                    }
                    else
                    {
                        Debug.Log("couldn't find ZonedBuildingWorldInfoPanel components");
                    }
                    s_hotelLabel.Hide();
                }

                var level = m_zonedBuildingWorldInfoPanel.Find("Level");
                if (level != null)
                {
                    CommercialBuildingTypesManager.CreateUI(m_zonedBuildingWorldInfoPanel.component, ref m_commercialBuildingTypeDropdown, level.relativePosition.x + 220f, level.relativePosition.y + 5f);
                    m_commercialBuildingTypeDropdown.eventSelectedIndexChanged += delegate (UIComponent uiComponent, int value)
                    {
                        ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
                        CommercialBuildingTypesManager.OnCommercialBuildingTypeDropdownIndexChanged(value, buildingID, s_updatingDropdown);
                    };
                }
            }
        }

        [HarmonyPatch]
        private sealed class RaceEventWorldInfoPanelPatch
        {
            [HarmonyPatch(typeof(RaceEventWorldInfoPanel), "Start")]
            [HarmonyPostfix]
            private static void Start(RaceEventWorldInfoPanel __instance, ref ushort ___m_eventRouteID, ref UITemplateList<UIPanel> ___m_EventConfigs)
            {
                ushort m_eventRouteID = ___m_eventRouteID;
                var m_EventConfigs = ___m_EventConfigs;

                for (int i = 0; i < 4; i++)
                {
                    // Match the logic you already have
                    var panel = ___m_EventConfigs.items[i];

                    panel.autoLayout = false;

                    panel.parent?.GetComponent<UIPanel>()?.autoLayout = false;

                    panel.height = 415f;

                    var panelStartNow = panel.Find<UIPanel>("PanelStartNow");
                    panelStartNow.relativePosition = new Vector3(266f, 98f);

                    int scheduleIndex = i;

                    // setup dropdowns
                    var dropdownDay = panel.Find<UIDropDown>("DropdownDay");
                    var dropdownMonth = panel.Find<UIDropDown>("DropdownMonth");
                    var dropdownFrequency = panel.Find<UIDropDown>("DropdownFrequency");

                    dropdownFrequency.parent.isVisible = true;
                    dropdownFrequency.parent.enabled = true;
                    dropdownFrequency.parent.isEnabled = true;

                    dropdownFrequency.isVisible = true;
                    dropdownFrequency.enabled = true;
                    dropdownFrequency.zOrder = 10;

                    var dropdownHour = UnityEngine.Object.Instantiate(dropdownDay.gameObject, dropdownDay.parent.transform).GetComponent<UIDropDown>();
                    var dropdownMinute = UnityEngine.Object.Instantiate(dropdownMonth.gameObject, dropdownMonth.parent.transform).GetComponent<UIDropDown>();
                    var dropdownAutoOccur = UnityEngine.Object.Instantiate(dropdownFrequency.gameObject, dropdownFrequency.parent.transform).GetComponent<UIDropDown>();

                    // setup dropdowns labels
                    var labelDay = panel.Find<UILabel>("LabelDay");
                    var labelMonth = panel.Find<UILabel>("LabelMonth");
                    var labelFrequency = panel.Find<UILabel>("LabelFrequency");

                    var labelHour = UnityEngine.Object.Instantiate(labelDay.gameObject, labelDay.parent.transform).GetComponent<UILabel>();
                    var labelMinute = UnityEngine.Object.Instantiate(labelMonth.gameObject, labelMonth.parent.transform).GetComponent<UILabel>();
                    var labelAutoOccur = UnityEngine.Object.Instantiate(labelFrequency.gameObject, labelFrequency.parent.transform).GetComponent<UILabel>();

                    // day of the week label and value
                    var dayOfWeek = UnityEngine.Object.Instantiate(labelDay.gameObject, labelDay.parent.transform).GetComponent<UILabel>();
                    var labelDayOfWeek = UnityEngine.Object.Instantiate(labelDay.gameObject, labelDay.parent.transform).GetComponent<UILabel>();
                    var labelOverlapWarning = UnityEngine.Object.Instantiate(labelDay.gameObject, labelDay.parent.transform).GetComponent<UILabel>();

                    // add disabled missing sprite
                    dropdownFrequency.disabledBgSprite = "OptionsDropboxDisabled";
                    dropdownAutoOccur.disabledBgSprite = "OptionsDropboxDisabled";

                    // update disabled text color to white
                    dropdownFrequency.disabledColor = new Color32(128, 128, 128, 160);
                    dropdownAutoOccur.disabledColor = new Color32(128, 128, 128, 160);

                    // get y positions of dropdown and label
                    float dropdownDayY = dropdownDay.relativePosition.y;
                    float newDropdownY = 65f;
                    float y = labelFrequency.relativePosition.y;

                    float labelDayY = labelDay.relativePosition.y;
                    float newlabelY = 71f;

                    // dropdowns relative positions
                    dropdownDay.relativePosition = new Vector3(40f, dropdownDayY);
                    dropdownMonth.relativePosition = new Vector3(160f, dropdownDayY);
                    dropdownFrequency.relativePosition = new Vector3(250f, dropdownDayY);
                    dropdownHour.relativePosition = new Vector3(40f, newDropdownY);
                    dropdownMinute.relativePosition = new Vector3(160f, newDropdownY);
                    dropdownAutoOccur.relativePosition = new Vector3(250f, newDropdownY);
                    
                    // dropdowns labels relative positions
                    labelDay.relativePosition = new Vector3(-40f, labelDayY);
                    labelMonth.relativePosition = new Vector3(80f, labelDayY);                  
                    labelFrequency.relativePosition = new Vector3(250f, y);
                    labelHour.relativePosition = new Vector3(-40f, newlabelY);
                    labelMinute.relativePosition = new Vector3(80f, newlabelY);                 
                    labelAutoOccur.relativePosition = new Vector3(250f, 50f);

                    // day of the week label and value relative positions
                    dayOfWeek.relativePosition = new Vector3(105f, 105f);
                    labelDayOfWeek.relativePosition = new Vector3(0f, 105f);
                    labelOverlapWarning.relativePosition = new Vector3(200f, 105f);

                    // new dropdowns names
                    dropdownHour.name = "DropdownHour";
                    dropdownMinute.name = "DropdownMinute";
                    dropdownAutoOccur.name = "DropdownAutoOccur";
                    dropdownAutoOccur.cachedName = "DropdownAutoOccur";

                    // new labels names for the new dropdowns
                    labelHour.name = "LabelHour";
                    labelMinute.name = "LabelMinute";
                    labelAutoOccur.name = "LabelAutoOccur";

                    // day of the week label and value names
                    dayOfWeek.name = "DayOfWeek";
                    labelDayOfWeek.name = "LabelDayOfWeek";
                    labelOverlapWarning.name = "LabelOverlapWarning";

                    // new dropdowns labels texts
                    labelHour.text = "";
                    labelMinute.text = ""; 
                    labelAutoOccur.text = "";

                    // day of the week label and value texts
                    dayOfWeek.text = "";
                    dayOfWeek.textAlignment = UIHorizontalAlignment.Left;
                    labelDayOfWeek.text = "";
                    labelDayOfWeek.textAlignment = UIHorizontalAlignment.Left;
                    labelOverlapWarning.text = "";
                    labelOverlapWarning.textAlignment = UIHorizontalAlignment.Left;

                    // day of the week label width
                    dayOfWeek.width = 100f;
                    labelDayOfWeek.width = 130f;

                    // new dropdown items
                    dropdownHour.items = [.. Enumerable.Range(0, 24).Select(i => i.ToString("D2"))];
                    dropdownMinute.items = ["00", "15", "30", "45"];

                    // new dropdown event changes
                    dropdownHour.eventSelectedIndexChanged += delegate (UIComponent uiComponent, int value)
                    {
                        OnScheduleHourChanged(scheduleIndex, value, __instance);
                    };

                    dropdownFrequency.eventSelectedIndexChanged += delegate (UIComponent uiComponent, int value)
                    {
                        OnScheduleFrequencyChanged(scheduleIndex, value, __instance);
                    };

                    dropdownMinute.eventSelectedIndexChanged += delegate (UIComponent uiComponent, int value)
                    {
                        OnScheduleMinuteChanged(scheduleIndex, value, __instance);
                    };

                    dropdownAutoOccur.eventSelectedIndexChanged += delegate (UIComponent uiComponent, int value)
                    {
                        OnScheduleAutoOccurChanged(scheduleIndex, value, __instance);
                    };

                    var sliderLapCount = panel.Find<UISlider>("SliderLapCount");

                    if (sliderLapCount != null)
                    {
                        sliderLapCount.minValue = 1f;
                        sliderLapCount.maxValue = 25f;
                        sliderLapCount.stepSize = 1f;
                    }

                    var existingAction = panel.objectUserData as Action;

                    panel.objectUserData = (Action)delegate  // Chain: original + your logic
                    {
                        existingAction?.Invoke();  // Run game code first

                        ushort routeID = Traverse.Create(__instance).Field("m_eventRouteID").GetValue<ushort>();
                        var eventConfigs = Traverse.Create(__instance).Field("m_EventConfigs").GetValue<UITemplateList<UIPanel>>();

                        var buffer = Singleton<EventManager>.instance.m_eventRoutes.m_buffer;
                        var scheduleData = buffer[routeID].m_scheduleData;
                        bool flag2 = (scheduleData[scheduleIndex].m_flags & EventRouteData.EventScheduleFlags.Suspended) != 0;

                        var eventTimeSchedules = EventRouteTimeManager.GetEventTimeSchedules(routeID);

                        var dropDownFrequency_action = eventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownFrequency");
                        var dropDownAutoOccur_action = eventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownAutoOccur");
                        var dropDownHour_action = eventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownHour");
                        var dropDownMinute_action = eventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownMinute");

                        int year = Singleton<SimulationManager>.instance.m_currentGameTime.Year;
                        int startMonth = scheduleData[scheduleIndex].m_startMonth + 1;
                        int startDay = scheduleData[scheduleIndex].m_startDay + 1;
                        int startHour = eventTimeSchedules[scheduleIndex].StartHour;
                        int startMinute = eventTimeSchedules[scheduleIndex].StartMinute;
                        var startDateTime = new DateTime(year, startMonth, startDay, startHour, startMinute, 0);
                        var newDateTime = AdjustEventStartTime(startDateTime);

                        dropDownHour_action.selectedIndex = newDateTime.Hour;
                        dropDownMinute_action.selectedIndex = GetMinuteIndex(newDateTime.Minute);
                        dropDownFrequency_action.selectedIndex = eventTimeSchedules[scheduleIndex].Frequency;
                        dropDownAutoOccur_action.selectedIndex = eventTimeSchedules[scheduleIndex].AutoOccur ? 1 : 0;

                        dropDownHour_action.isEnabled = flag2;
                        dropDownMinute_action.isEnabled = flag2;
                        dropDownFrequency_action.isEnabled = flag2 && eventTimeSchedules[scheduleIndex].AutoOccur;
                        dropDownAutoOccur_action.isEnabled = flag2;

                        dropDownHour_action.parent?.isEnabled = flag2;
                        dropDownMinute_action.parent?.isEnabled = flag2;
                        dropDownFrequency_action.parent?.isEnabled = flag2;
                        dropDownAutoOccur_action.parent?.isEnabled = flag2;

                        dropDownHour_action.opacity = (!flag2) ? 0.4f : 1f;
                        dropDownMinute_action.opacity = (!flag2) ? 0.4f : 1f;
                        dropDownFrequency_action.opacity = (!flag2) ? 0.4f : 1f;
                        dropDownAutoOccur_action.opacity = (!flag2) ? 0.4f : 1f;
                    }; 
                }
            }

            [HarmonyPatch(typeof(RaceEventWorldInfoPanel), "OnSetTarget")]
            [HarmonyPostfix]
            private static void OnSetTarget(RaceEventWorldInfoPanel __instance, ref ushort ___m_eventRouteID, ref UITemplateList<UIPanel> ___m_EventConfigs)
            {
                for (int i = 0; i < 4; i++)
                {
                    int scheduleIndex = i;

                    var dropDownDay = ___m_EventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownDay");
                    var dropDownMonth = ___m_EventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownMonth");
                    var dropDownHour = ___m_EventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownHour");
                    var dropDownMinute = ___m_EventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownMinute");
                    var dayOfWeek = ___m_EventConfigs.items[scheduleIndex].Find<UILabel>("DayOfWeek");

                    dropDownDay.tooltip = LocalizationProvider.Translate(TranslationKeys.RaceDayDropDownDateTimeTooltip);
                    dropDownMonth.tooltip = LocalizationProvider.Translate(TranslationKeys.RaceDayDropDownDateTimeTooltip);
                    dropDownHour.tooltip = LocalizationProvider.Translate(TranslationKeys.RaceDayDropDownDateTimeTooltip);
                    dropDownMinute.tooltip = LocalizationProvider.Translate(TranslationKeys.RaceDayDropDownDateTimeTooltip);

                    var dropdownFrequency = ___m_EventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownFrequency");
                    var dropdownAutoOccur = ___m_EventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownAutoOccur");

                    dropdownFrequency?.items = [
                        LocalizationProvider.Translate(TranslationKeys.RaceDayDropDownWeeklyFrequency),
                        LocalizationProvider.Translate(TranslationKeys.RaceDayDropDownDailyFrequency)
                    ];

                    dropdownAutoOccur?.items = [
                            LocalizationProvider.Translate(TranslationKeys.RaceDayDropDownDisableAutoOccur),
                            LocalizationProvider.Translate(TranslationKeys.RaceDayDropDownEnableAutoOccur)
                    ];

                    int year = Singleton<SimulationManager>.instance.m_currentGameTime.Year;
                    int month = int.Parse(dropDownMonth.selectedValue);
                    int day = int.Parse(dropDownDay.selectedValue);
                    int hour = int.Parse(dropDownHour.selectedValue);
                    int minute = int.Parse(dropDownMinute.selectedValue);

                    var dateTime = new DateTime(year, month, day, hour, minute, 0);
                    dayOfWeek.text = GetDayOfWeekLocalized(dateTime);

                    dropdownFrequency.tooltip = LocalizationProvider.Translate(TranslationKeys.RaceDayDropDownFrequencyTooltip);
                    dropdownAutoOccur.tooltip = LocalizationProvider.Translate(TranslationKeys.RaceDayDropDownAutoOccurTooltip);

                    var labelHour = ___m_EventConfigs.items[scheduleIndex].Find<UILabel>("LabelHour");
                    var labelMinute = ___m_EventConfigs.items[scheduleIndex].Find<UILabel>("LabelMinute");
                    var labelAutoOccur = ___m_EventConfigs.items[scheduleIndex].Find<UILabel>("LabelAutoOccur");
                    var labelDayOfWeek = ___m_EventConfigs.items[scheduleIndex].Find<UILabel>("LabelDayOfWeek");

                    labelHour?.text = LocalizationProvider.Translate(TranslationKeys.RaceDayLabelHour);
                    labelMinute?.text = LocalizationProvider.Translate(TranslationKeys.RaceDayLabelMinute);
                    labelAutoOccur?.text = LocalizationProvider.Translate(TranslationKeys.RaceDayLabelAutoOccur);
                    labelDayOfWeek?.text = LocalizationProvider.Translate(TranslationKeys.RaceDayLabelDayOfWeek);
                }
                
            }

            [HarmonyPatch(typeof(RaceEventWorldInfoPanel), "UpdatePastEvents")]
            [HarmonyPostfix]
            private static void UpdatePastEvents(RaceEventWorldInfoPanel __instance, ref InstanceID ___m_InstanceID, ref UITemplateList<UIPanel> ___m_PastEventList)
            {
                if (___m_InstanceID.Building != 0)
                {
                    ushort num2 = Singleton<BuildingManager>.instance.m_buildings.m_buffer[___m_InstanceID.Building].m_eventIndex;
                    if (num2 != 0)
                    {
                        if ((Singleton<EventManager>.instance.m_events.m_buffer[num2].m_flags & (EventData.Flags.Completed | EventData.Flags.Cancelled)) == 0)
                        {
                            num2 = Singleton<EventManager>.instance.m_events.m_buffer[num2].m_nextBuildingEvent;
                        }
                        ref var eventData = ref Singleton<EventManager>.instance.m_events.m_buffer[num2];
                        for (int num4 = 0; num4 < ___m_PastEventList.items.Count; num4++)
                        {
                            var uIPanel2 = ___m_PastEventList.items[num4];
                            var labelDate = uIPanel2.Find<UILabel>("LabelDate");
                            labelDate.text = Singleton<SimulationManager>.instance.FrameToTime(eventData.m_startFrame).ToString("dd/MM/yyyy");
                        }
                    }   
                }
            }

            [HarmonyPatch(typeof(RaceEventWorldInfoPanel), "UpdateEventSchedule")]
            [HarmonyPostfix]
            private static void UpdateEventSchedule(RaceEventWorldInfoPanel __instance, ref ushort ___m_eventRouteID, ref UIComponent[] ___m_PanelNextEvent, ref UILabel ___m_NextEventDate)
            {
                var scheduledEvents = Singleton<EventManager>.instance.m_eventRoutes.m_buffer[___m_eventRouteID].m_scheduledEvents;
                ushort num = Singleton<EventManager>.instance.m_eventRoutes.m_buffer[___m_eventRouteID].m_event;
                if(num == 0)
                {
                    ___m_NextEventDate.text = scheduledEvents[0].m_startDate.ToString("dd/MM/yyyy HH:mm");
                }
                else
                {
                    var dateTime = Singleton<SimulationManager>.instance.FrameToTime(Singleton<EventManager>.instance.m_events[num].m_startFrame);
                    ___m_NextEventDate.text = dateTime.ToString("dd/MM/yyyy HH:mm");
                }
                var buffer = Singleton<EventManager>.instance.m_eventRoutes.m_buffer;
                var scheduleData = buffer[___m_eventRouteID].m_scheduleData;
                var now = Singleton<SimulationManager>.instance.m_currentGameTime;

                for (int j = 0; j < 5; j++)
                {
                    ClearNextEventPanel(___m_PanelNextEvent[j]);
                }

                int startIndex = (num == 0) ? 1 : 0;
                int visibleRow = 0;
                for (int index = startIndex; index < scheduledEvents.Length && visibleRow < 5; index++)
                {
                    var scheduled = scheduledEvents[index];

                    if (scheduled.m_startDate <= DateTime.MinValue)
                    {
                        continue;
                    }

                    if (scheduled.m_startDate < now)
                    {
                        continue;
                    }

                    int scheduleIndex = scheduledEvents[index].m_scheduleIndex;

                    if (scheduleIndex < 0 || scheduleIndex >= buffer[___m_eventRouteID].m_scheduleCount)
                    {
                        continue;
                    }

                    var info = scheduleData[scheduleIndex].Info;
                    if (info == null)
                    {
                        continue;
                    }

                    var panel = ___m_PanelNextEvent[visibleRow];
                    panel.isVisible = true;

                    var dateLabel = panel.Find<UILabel>("Date");
                    var nameLabel = panel.Find<UILabel>("Name");
                    var costLabel = panel.Find<UILabel>("Cost");

                    dateLabel?.text = scheduled.m_startDate.ToString("dd/MM/yyyy HH:mm");
                    nameLabel?.text = scheduleData[scheduleIndex].Name;

                    var raceEventAI = info.GetEventAI() as RaceEventAI;
                    var tierInfo = raceEventAI != null ? raceEventAI.GetTierInfo(scheduleData[scheduleIndex].m_tier) : default;
                    costLabel?.text = LocaleFormatter.FormatCurrency(-tierInfo.m_prizePool);

                    visibleRow++;
                }

            }

            [HarmonyPatch(typeof(RaceEventWorldInfoPanel), "CreateNewEvent")]
            [HarmonyPrefix]
            private static bool CreateNewEvent(RaceEventWorldInfoPanel __instance, bool alwaysCreateNew, ref ushort ___m_eventRouteID, ref List<EventInfo> ___m_allowedEventInfos, ref UITabstrip ___m_tabstrip)
            {
                ushort m_eventRouteID = ___m_eventRouteID;
                var m_allowedEventInfos = ___m_allowedEventInfos;
                var m_tabstrip = ___m_tabstrip;
                Singleton<SimulationManager>.instance.AddAction(delegate
                {
                    if (m_eventRouteID != 0)
                    {
                        var buffer = Singleton<EventManager>.instance.m_eventRoutes.m_buffer;
                        int scheduleCount = buffer[m_eventRouteID].m_scheduleCount;
                        if (scheduleCount == 0 || alwaysCreateNew)
                        {
                            if (scheduleCount >= buffer[m_eventRouteID].m_scheduleData.Length)
                            {
                                return;
                            }
                            var candidate = AdjustEventStartTime(Singleton<SimulationManager>.instance.m_currentGameTime);
                            int retries = 0;
                            while (HasScheduleConflict(m_eventRouteID, scheduleCount, candidate) && retries++ < 32)
                            {
                                candidate = candidate.AddHours(8);
                                candidate = AdjustEventStartTime(candidate);
                            }
                            buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_scheduleID = ++Singleton<EventManager>.instance.m_eventScheduleCount;
                            buffer[m_eventRouteID].m_scheduleData[scheduleCount].Info = m_allowedEventInfos[0];
                            buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_startDay = (byte)(candidate.Day - 1);
                            buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_startMonth = (byte)(candidate.Month - 1);
                            buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_laps = 1;
                            buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_ticketPrice = 100;
                            buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_flags = EventRouteData.EventScheduleFlags.Suspended;
                            EventRouteTimeManager.SetEventTimeScheduleHour(m_eventRouteID, scheduleCount, (byte)candidate.Hour);
                            EventRouteTimeManager.SetEventTimeScheduleMinute(m_eventRouteID, scheduleCount, (byte)candidate.Minute);
                            EventRouteTimeManager.SetEventTimeScheduleFrequency(m_eventRouteID, scheduleCount, 0); // weekly default
                            EventRouteTimeManager.SetEventTimeScheduleAutoOccur(m_eventRouteID, scheduleCount, true); // auto occur by default
                            buffer[m_eventRouteID].m_scheduleCount++;
                            Singleton<EventManager>.instance.ScheduleEventRoute(m_eventRouteID);
                        }
                        ThreadHelper.dispatcher.Dispatch(delegate
                        {
                            m_tabstrip.selectedIndex = 1;
                            UpdateEventScheduleForm(__instance);
                        });
                    }
                });
                return false;
            }

            [HarmonyPatch(typeof(RaceEventWorldInfoPanel), "OnScheduleCancel")]
            [HarmonyPostfix]
            private static void OnScheduleCancel(RaceEventWorldInfoPanel __instance, int scheduleIndex, ref ushort ___m_eventRouteID)
            {
                var eventTimeSchedules = EventRouteTimeManager.GetEventTimeSchedules(___m_eventRouteID);

                eventTimeSchedules[scheduleIndex] = default;

                EventRouteTimeManager.SetEventTimeSchedule(___m_eventRouteID, eventTimeSchedules);
            }

            [HarmonyReversePatch]
            [HarmonyPatch(typeof(RaceEventWorldInfoPanel), "UpdateEventScheduleForm")]
            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void UpdateEventScheduleForm(object instance)
            {
                string message = "UpdateEventScheduleForm reverse Harmony patch wasn't applied";
                Debug.LogError(message);
                throw new NotImplementedException(message);
            }

            [HarmonyReversePatch]
            [HarmonyPatch(typeof(RaceEventWorldInfoPanel), "RefreshEventSchedule")]
            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void RefreshEventSchedule(object instance)
            {
                string message = "RefreshEventSchedule reverse Harmony patch wasn't applied";
                Debug.LogError(message);
                throw new NotImplementedException(message);
            }

            [HarmonyPatch(typeof(RaceEventWorldInfoPanel), "OnScheduleDayChanged")]
            [HarmonyPrefix]
            private static bool OnScheduleDayChanged(RaceEventWorldInfoPanel __instance, int scheduleIndex, int value, ref ushort ___m_eventRouteID, ref UITemplateList<UIPanel> ___m_EventConfigs)
            {
                var buffer = Singleton<EventManager>.instance.m_eventRoutes.m_buffer;
                var scheduleData = buffer[___m_eventRouteID].m_scheduleData;
                var eventTimeSchedules = EventRouteTimeManager.GetEventTimeSchedules(___m_eventRouteID);
                int year = Singleton<SimulationManager>.instance.m_currentGameTime.Year;
                int startMonth = scheduleData[scheduleIndex].m_startMonth + 1;
                int startHour = eventTimeSchedules[scheduleIndex].StartHour;
                int startMinute = eventTimeSchedules[scheduleIndex].StartMinute;

                byte startDay = scheduleData[scheduleIndex].m_startDay;
                byte b = (byte)value;
                if (startDay != b)
                {
                    int num = DateTime.DaysInMonth(2, startMonth);
                    bool flag = b + 1 > num;
                    if (flag)
                    {
                        b = (byte)(num - 1);
                    }
                    var uIDropDown = ___m_EventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownDay");
                    scheduleData[scheduleIndex].m_startDay = b;
                    RefreshEventSchedule(__instance);
                    if (flag)
                    {
                        uIDropDown.selectedIndex = b;
                    }
                    var startDateTime = new DateTime(year, startMonth, b + 1, startHour, startMinute, 0);
                    var newDateTime = AdjustEventStartTime(startDateTime);
                    var labelOverlapWarning = ___m_EventConfigs.items[scheduleIndex].Find<UILabel>("LabelOverlapWarning");
                    var buttonStartNow = ___m_EventConfigs.items[scheduleIndex].Find<UIButton>("ButtonStartNow");
                    if (HasScheduleConflict(___m_eventRouteID, scheduleIndex, newDateTime))
                    {
                        labelOverlapWarning.text = LocalizationProvider.Translate(TranslationKeys.RaceDayScheduleOverlapWarning);
                        buttonStartNow.isEnabled = false;
                    }
                    else
                    {
                        labelOverlapWarning.text = "";
                        buttonStartNow.isEnabled = true;
                    } 
                    string newDayOfWeek = GetDayOfWeekLocalized(newDateTime);
                    var uIDropDownHour = ___m_EventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownHour");
                    var uIDropDownMinute = ___m_EventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownMinute");
                    var dayOfWeek = ___m_EventConfigs.items[scheduleIndex].Find<UILabel>("DayOfWeek");
                    uIDropDownHour.selectedIndex = newDateTime.Hour;
                    uIDropDownMinute.selectedIndex = GetMinuteIndex(newDateTime.Minute);
                    dayOfWeek.text = newDayOfWeek;
                }
                return false;
            }

            [HarmonyPatch(typeof(RaceEventWorldInfoPanel), "OnScheduleMonthChanged")]
            [HarmonyPrefix]
            private static bool OnScheduleMonthChanged(RaceEventWorldInfoPanel __instance, int scheduleIndex, int value, ref ushort ___m_eventRouteID, ref UITemplateList<UIPanel> ___m_EventConfigs)
            {
                var buffer = Singleton<EventManager>.instance.m_eventRoutes.m_buffer;
                var scheduleData = buffer[___m_eventRouteID].m_scheduleData;
                var eventTimeSchedules = EventRouteTimeManager.GetEventTimeSchedules(___m_eventRouteID);
                int year = Singleton<SimulationManager>.instance.m_currentGameTime.Year;
                int startDay = scheduleData[scheduleIndex].m_startDay + 1;
                int startHour = eventTimeSchedules[scheduleIndex].StartHour;
                int startMinute = eventTimeSchedules[scheduleIndex].StartMinute;

                byte startMonth = scheduleData[scheduleIndex].m_startMonth;
                byte b = (byte)value;
                if (startMonth != b)
                {
                    scheduleData[scheduleIndex].m_startMonth = b;
                    byte b2 = (byte)(scheduleData[scheduleIndex].m_startDay + 1);
                    int num = DateTime.DaysInMonth(2, b + 1);
                    if (b2 > num)
                    {
                        var uIDropDown = ___m_EventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownDay");
                        uIDropDown.selectedIndex = num - 1;
                    }
                    RefreshEventSchedule(__instance);
                    var startDateTime = new DateTime(year, b + 1, startDay, startHour, startMinute, 0);
                    var newDateTime = AdjustEventStartTime(startDateTime);
                    var labelOverlapWarning = ___m_EventConfigs.items[scheduleIndex].Find<UILabel>("LabelOverlapWarning");
                    var buttonStartNow = ___m_EventConfigs.items[scheduleIndex].Find<UIButton>("ButtonStartNow");
                    if (HasScheduleConflict(___m_eventRouteID, scheduleIndex, newDateTime))
                    {
                        labelOverlapWarning.text = LocalizationProvider.Translate(TranslationKeys.RaceDayScheduleOverlapWarning);
                        buttonStartNow.isEnabled = false;
                    }
                    else
                    {
                        labelOverlapWarning.text = "";
                        buttonStartNow.isEnabled = true;
                    }
                    string newDayOfWeek = GetDayOfWeekLocalized(newDateTime);
                    var uIDropDownHour = ___m_EventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownHour");
                    var uIDropDownMinute = ___m_EventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownMinute");
                    var dayOfWeek = ___m_EventConfigs.items[scheduleIndex].Find<UILabel>("DayOfWeek");
                    uIDropDownHour.selectedIndex = newDateTime.Hour;
                    uIDropDownMinute.selectedIndex = GetMinuteIndex(newDateTime.Minute);
                    dayOfWeek.text = newDayOfWeek;
                }
                return false;
            }

            private static void OnScheduleHourChanged(int scheduleIndex, int value, RaceEventWorldInfoPanel instance)
            {
                ushort routeID = Traverse.Create(instance).Field("m_eventRouteID").GetValue<ushort>();
                var eventConfigs = Traverse.Create(instance).Field("m_EventConfigs").GetValue<UITemplateList<UIPanel>>();
                var eventTimeSchedules = EventRouteTimeManager.GetEventTimeSchedules(routeID);
                var buffer = Singleton<EventManager>.instance.m_eventRoutes.m_buffer;
                var scheduleData = buffer[routeID].m_scheduleData;
                int year = Singleton<SimulationManager>.instance.m_currentGameTime.Year;
                int startMonth = scheduleData[scheduleIndex].m_startMonth + 1;
                int startDay = scheduleData[scheduleIndex].m_startDay + 1;
                int startMinute = eventTimeSchedules[scheduleIndex].StartMinute;

                byte startHour = eventTimeSchedules[scheduleIndex].StartHour;
                byte b = (byte)value;
                if (startHour != b)
                {
                    var startDateTime = new DateTime(year, startMonth, startDay, b, startMinute, 0);
                    var newDateTime = AdjustEventStartTime(startDateTime);
                    var labelOverlapWarning = eventConfigs.items[scheduleIndex].Find<UILabel>("LabelOverlapWarning");
                    var buttonStartNow = eventConfigs.items[scheduleIndex].Find<UIButton>("ButtonStartNow");
                    if (HasScheduleConflict(routeID, scheduleIndex, newDateTime))
                    {
                        labelOverlapWarning.text = LocalizationProvider.Translate(TranslationKeys.RaceDayScheduleOverlapWarning);
                        buttonStartNow.isEnabled = false;
                    }
                    else
                    {
                        labelOverlapWarning.text = "";
                        buttonStartNow.isEnabled = true;
                    }
                    EventRouteTimeManager.SetEventTimeScheduleHour(routeID, scheduleIndex, (byte)newDateTime.Hour);
                    RefreshEventSchedule(instance);
                    var uIDropDownHour = eventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownHour");
                    var uIDropDownMinute = eventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownMinute");
                    uIDropDownHour.selectedIndex = newDateTime.Hour;
                    uIDropDownMinute.selectedIndex = GetMinuteIndex(newDateTime.Minute);
                }
            }

            private static DateTime AdjustEventStartTime(DateTime eventStartTime)
            {
                var result = eventStartTime;

                while (true)
                {
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

                    var earliest = TimeSpan.FromHours(earliestHour);
                    var latest = TimeSpan.FromHours(latestHour);
                    var current = result.TimeOfDay;

                    if (current < earliest)
                    {
                        return result.Date + earliest;
                    }

                    if (current <= latest)
                    {
                        return result;
                    }

                    result = result.Date.AddDays(1);
                }
            }

            private static void OnScheduleFrequencyChanged(int scheduleIndex, int value, RaceEventWorldInfoPanel instance)
            {
                ushort routeID = Traverse.Create(instance).Field("m_eventRouteID").GetValue<ushort>();
                var eventConfigs = Traverse.Create(instance).Field("m_EventConfigs").GetValue<UITemplateList<UIPanel>>();
                var eventTimeSchedules = EventRouteTimeManager.GetEventTimeSchedules(routeID);
                byte frequency = eventTimeSchedules[scheduleIndex].Frequency;
                byte b = (byte)value;
                if (frequency != b)
                {
                    var uIDropDown = eventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownFrequency");
                    EventRouteTimeManager.SetEventTimeScheduleFrequency(routeID, scheduleIndex, b);
                    RefreshEventSchedule(instance);
                    if (uIDropDown.selectedIndex != b)
                    {
                        uIDropDown.selectedIndex = b;
                    }
                }
            }

            private static void OnScheduleMinuteChanged(int scheduleIndex, int value, RaceEventWorldInfoPanel instance)
            {
                ushort routeID = Traverse.Create(instance).Field("m_eventRouteID").GetValue<ushort>();
                var eventConfigs = Traverse.Create(instance).Field("m_EventConfigs").GetValue<UITemplateList<UIPanel>>();
                var eventTimeSchedules = EventRouteTimeManager.GetEventTimeSchedules(routeID);
                var buffer = Singleton<EventManager>.instance.m_eventRoutes.m_buffer;
                var scheduleData = buffer[routeID].m_scheduleData;
                int year = Singleton<SimulationManager>.instance.m_currentGameTime.Year;
                int startMonth = scheduleData[scheduleIndex].m_startMonth + 1;
                int startDay = scheduleData[scheduleIndex].m_startDay + 1;
                int startHour = eventTimeSchedules[scheduleIndex].StartHour;

                var uIDropDown = eventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownMinute");
                byte startMinute = eventTimeSchedules[scheduleIndex].StartMinute;
                byte b = (byte)value;
                int minute = int.Parse(uIDropDown.items[b]);
                if (startMinute != minute)
                {
                    var startDateTime = new DateTime(year, startMonth, startDay, startHour, minute, 0);
                    var newDateTime = AdjustEventStartTime(startDateTime);
                    var labelOverlapWarning = eventConfigs.items[scheduleIndex].Find<UILabel>("LabelOverlapWarning");
                    var buttonStartNow = eventConfigs.items[scheduleIndex].Find<UIButton>("ButtonStartNow");
                    if (HasScheduleConflict(routeID, scheduleIndex, newDateTime))
                    {
                        labelOverlapWarning.text = LocalizationProvider.Translate(TranslationKeys.RaceDayScheduleOverlapWarning);
                        buttonStartNow.isEnabled = false;
                    }
                    else
                    {
                        labelOverlapWarning.text = "";
                        buttonStartNow.isEnabled = true;
                    }
                    
                    EventRouteTimeManager.SetEventTimeScheduleMinute(routeID, scheduleIndex, (byte)newDateTime.Minute);
                    RefreshEventSchedule(instance);
                    uIDropDown.selectedIndex = GetMinuteIndex(newDateTime.Minute);
                }
            }

            private static void OnScheduleAutoOccurChanged(int scheduleIndex, int value, RaceEventWorldInfoPanel instance)
            {
                ushort routeID = Traverse.Create(instance).Field("m_eventRouteID").GetValue<ushort>();
                var eventConfigs = Traverse.Create(instance).Field("m_EventConfigs").GetValue<UITemplateList<UIPanel>>();
                var eventTimeSchedules = EventRouteTimeManager.GetEventTimeSchedules(routeID);
                bool autoOccur = eventTimeSchedules[scheduleIndex].AutoOccur;
                bool b = value == 1;
                if (autoOccur != b)
                {
                    var uIDropDown = eventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownAutoOccur");
                    EventRouteTimeManager.SetEventTimeScheduleAutoOccur(routeID, scheduleIndex, b);
                    RefreshEventSchedule(instance);
                    int index = b ? 1 : 0;
                    if (uIDropDown.selectedIndex != index)
                    {
                        uIDropDown.selectedIndex = index;
                    }
                }
            }

            private static string GetDayOfWeekLocalized(DateTime dateTime)
            {
                string dayName = dateTime.DayOfWeek.ToString();
                string keyToTranslate = "RaceDayLabelDayOfWeek" + dayName;
                return LocalizationProvider.Translate(keyToTranslate);
            }

            private static void ClearNextEventPanel(UIComponent panel)
            {
                panel.isVisible = false;

                var date = panel.Find<UILabel>("Date");
                var name = panel.Find<UILabel>("Name");
                var cost = panel.Find<UILabel>("Cost");

                date?.text = string.Empty;
                name?.text = string.Empty;
                cost?.text = string.Empty;
            }

            private static int GetMinuteIndex(int minute) => minute switch
            {
                0 => 0,
                15 => 1,
                30 => 2,
                45 => 3,
                _ => 0
            };

        }

        public static bool HasScheduleConflict(ushort routeID, int editedScheduleIndex, DateTime candidateStart)
        {
            ref var route = ref Singleton<EventManager>.instance.m_eventRoutes.m_buffer[routeID];

            // Check current real event
            ushort currentEventId = route.m_event;
            if (currentEventId != 0)
            {
                ref var ev = ref Singleton<EventManager>.instance.m_events.m_buffer[currentEventId];
                var currentStart = TimeAdjustment.GetOriginalTime(ev.m_startFrame);

                if (Overlaps(candidateStart, candidateStart + TimeSpan.FromHours(7), currentStart, currentStart + TimeSpan.FromHours(7)))
                {
                    return true;
                }
            }

            // Check future scheduled starts
            var scheduledEvents = route.m_scheduledEvents;
            for (int i = 0; i < scheduledEvents.Length; i++)
            {
                var scheduled = scheduledEvents[i];
                if (scheduled.m_startDate == DateTime.MinValue)
                {
                    continue;
                }

                if (scheduled.m_scheduleIndex == editedScheduleIndex)
                {
                    continue;
                }

                if (Overlaps(candidateStart, candidateStart + TimeSpan.FromHours(7), scheduled.m_startDate, scheduled.m_startDate + TimeSpan.FromHours(7)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Overlaps(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd) => aStart < bEnd && aEnd > bStart;

    }
}
