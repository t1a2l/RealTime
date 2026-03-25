// WorldInfoPanelPatch.cs

namespace RealTime.Patches
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using ColossalFramework;
    using ColossalFramework.Math;
    using ColossalFramework.Threading;
    using ColossalFramework.UI;
    using HarmonyLib;
    using RealTime.Config;
    using RealTime.CustomAI;
    using RealTime.Events;
    using RealTime.GameConnection;
    using RealTime.Localization;
    using RealTime.Managers;
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
        public static ILocalizationProvider localizationProvider { get; set; }

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

            private static UserEventCreationPanel userEventCreationPanel;

            private static EventSelectionPanel eventSelectionPanel;


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

                    if(userEventCreationPanel == null)
                    {
                        userEventCreationPanel = m_cityServiceWorldInfoPanel.component.AddUIComponent<UserEventCreationPanel>();
                    }

                    if(eventSelectionPanel == null)
                    {
                        eventSelectionPanel = buttonPanels.AddUIComponent<EventSelectionPanel>();
                    }

                    if (buildingAI is ExtractingFacilityAI || buildingAI is ProcessingFacilityAI)
                    {
                        checkBoxXposition = 360f;
                        checkBoxYposition = 0f;
                        panelHeight = 40f;
                    }

                    userEventCreationPanel.isVisible = false;
                    eventSelectionPanel.UpdateData(userEventCreationPanel);
                    eventSelectionPanel.CheckAndSetupEvents();

                    CityServiceOperationHoursPanel.UpdateData(panelHeight, localizationProvider);
                    CityServiceOperationHoursCheckBoxPanel.UpdateData(checkBoxXposition, checkBoxYposition, localizationProvider, CityServiceOperationHoursPanel);

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

                    UniqueFactoryOperationHoursPanel.UpdateData(panelHeight, localizationProvider);
                    UniqueFactoryOperationHoursCheckBoxPanel.UpdateData(checkBoxXposition, checkBoxYposition, localizationProvider, UniqueFactoryOperationHoursPanel);

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

                    WarehouseOperationHoursPanel.UpdateData(panelHeight, localizationProvider);
                    WarehouseOperationHoursCheckBoxPanel.UpdateData(checkBoxXposition, checkBoxYposition, localizationProvider, WarehouseOperationHoursPanel);

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

                    ZonedBuildingOperationHoursPanel.UpdateData(panelHeight, localizationProvider);
                    ZonedBuildingOperationHoursCheckBoxPanel.UpdateData(checkBoxXposition, checkBoxYposition, localizationProvider, ZonedBuildingOperationHoursPanel);

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

            [HarmonyPatch(typeof(CityServiceWorldInfoPanel), "OnSetTarget")]
            [HarmonyPostfix]
            public static void OnSetTarget()
            {
                if (s_visitorsLabel == null || m_endYearButton == null)
                {
                    CreateUI();
                }
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

                if (s_visitorsLabel == null)
                {
                    s_visitorsLabel = UILabels.CreatePositionedLabel(buttonPanels, 65f, 280f, "VisitorsLabel", "Visitors", textScale: 0.75f);
                    s_visitorsLabel.textColor = new Color32(185, 221, 254, 255);
                    s_visitorsLabel.font = Resources.FindObjectsOfTypeAll<UIFont>().FirstOrDefault(f => f.name == "OpenSans-Regular");
                    s_visitorsLabel.relativePosition = new Vector2(200f, 26f);
                }

                if (m_endYearButton == null)
                {
                    string endYearButtonText = localizationProvider.Translate(TranslationKeys.AcademicYearEndYearButtonText);
                    string endYearButtonTooltipText = localizationProvider.Translate(TranslationKeys.AcademicYearEndYearButtonTooltip);
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
        }

        [HarmonyPatch]
        private sealed class ZonedBuildingWorldInfoPanelPatch
        {
            private static UILabel s_hotelLabel;

            [HarmonyPatch(typeof(ZonedBuildingWorldInfoPanel), "UpdateBindings")]
            [HarmonyPostfix]
            private static void UpdateBindings()
            {
                // Currently selected building.
                ushort building = WorldInfoPanel.GetCurrentInstanceID().Building;

                // Create hotel label if it isn't already set up.
                if (s_hotelLabel == null)
                {
                    // Get info panel.
                    var infoPanel = UIView.library.Get<ZonedBuildingWorldInfoPanel>(typeof(ZonedBuildingWorldInfoPanel).Name);

                    // Add current visitor count label.
                    s_hotelLabel = UILabels.CreatePositionedLabel(infoPanel.component, 65f, 280f, "HotelLabel", "Rooms Ocuppied", textScale: 0.75f);
                    s_hotelLabel.textColor = new Color32(185, 221, 254, 255);
                    s_hotelLabel.font = Resources.FindObjectsOfTypeAll<UIFont>().FirstOrDefault(f => f.name == "OpenSans-Regular");

                    // Position under existing Highly Educated workers count row in line with total workplace count label.
                    var situationLabel = infoPanel.Find("WorkSituation");
                    var workerLabel = infoPanel.Find("HighlyEducatedWorkers");
                    if (situationLabel != null && workerLabel != null)
                    {
                        s_hotelLabel.absolutePosition = new Vector2(situationLabel.absolutePosition.x + 200f, workerLabel.absolutePosition.y + 25f);
                    }
                    else
                    {
                        Debug.Log("couldn't find ZonedBuildingWorldInfoPanel components");
                    }
                }

                // Local references.
                var buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                var buildingData = buildingBuffer[building];
                var buildingInfo = buildingData.Info;

                // Is this a hotel building?
                if (buildingInfo.GetAI() is CommercialBuildingAI && BuildingManagerConnection.IsHotel(building))
                {
                    // Hotel show the label
                    s_hotelLabel.Show();

                    // Display hotel rooms ocuppied count out of max hotel rooms.
                    s_hotelLabel.text = buildingData.m_roomUsed + " / " + buildingData.m_roomMax + " Rooms";

                }
                else
                {
                    // Not a hotel hide the label
                    s_hotelLabel.Hide();
                }
            }
        }

        [HarmonyPatch]
        private sealed class RaceEventWorldInfoPanelPatch
        {
            private delegate void UpdatePastEventDelegate(ushort eventIndex, ref EventData eventData);

            [HarmonyPatch(typeof(RaceEventWorldInfoPanel), "Start")]
            [HarmonyPostfix]
            private static void Start(RaceEventWorldInfoPanel __instance, ref ushort ___m_eventRouteID, ref UITemplateList<UIPanel> ___m_EventConfigs, ref UITemplateList<UIPanel> ___m_PastEventList)
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

                    var buttonStartNow = panel.Find<UIButton>("ButtonStartNow");
                    buttonStartNow.relativePosition = new Vector3(8f, 80f);

                    int scheduleIndex = i;

                    // setup dropdowns
                    var dropdownDay = panel.Find<UIDropDown>("DropdownDay");
                    var dropdownMonth = panel.Find<UIDropDown>("DropdownMonth");
                    var dropdownFrequency = UnityEngine.Object.Instantiate(dropdownDay.gameObject, dropdownDay.parent.transform).GetComponent<UIDropDown>();
                    var dropdownHour = UnityEngine.Object.Instantiate(dropdownDay.gameObject, dropdownDay.parent.transform).GetComponent<UIDropDown>();
                    var dropdownMinute = UnityEngine.Object.Instantiate(dropdownMonth.gameObject, dropdownMonth.parent.transform).GetComponent<UIDropDown>();
                    var dropdownAutoOccur = UnityEngine.Object.Instantiate(dropdownMonth.gameObject, dropdownMonth.parent.transform).GetComponent<UIDropDown>();

                    // setup dropdowns labels
                    var labelDay = panel.Find<UILabel>("LabelDay");
                    var labelMonth = panel.Find<UILabel>("LabelMonth");
                    var labelFrequency = UnityEngine.Object.Instantiate(labelDay.gameObject, labelDay.parent.transform).GetComponent<UILabel>();
                    var labelHour = UnityEngine.Object.Instantiate(labelDay.gameObject, labelDay.parent.transform).GetComponent<UILabel>();
                    var labelMinute = UnityEngine.Object.Instantiate(labelMonth.gameObject, labelMonth.parent.transform).GetComponent<UILabel>();
                    var labelAutoOccur = UnityEngine.Object.Instantiate(labelMonth.gameObject, labelMonth.parent.transform).GetComponent<UILabel>();

                    // get y positions of dropdown and label
                    float dropdownDayY = dropdownDay.relativePosition.y;
                    float labelDayY = labelDay.relativePosition.y;
                    float newDropdownY = 57f;
                    float newlabelY = 63f;

                    // dropdowns relative positions
                    dropdownDay.relativePosition = new Vector3(40f, dropdownDayY);
                    dropdownMonth.relativePosition = new Vector3(160f, dropdownDayY);
                    dropdownFrequency.relativePosition = new Vector3(310f, dropdownDayY);
                    dropdownHour.relativePosition = new Vector3(40f, newDropdownY);
                    dropdownMinute.relativePosition = new Vector3(160f, newDropdownY);
                    dropdownAutoOccur.relativePosition = new Vector3(310f, newDropdownY);

                    // long dropdowns new width
                    dropdownFrequency.width = 110f;
                    dropdownAutoOccur.width = 110f;

                    // labels relative positions
                    labelDay.relativePosition = new Vector3(-40f, newlabelY);
                    labelMonth.relativePosition = new Vector3(80f, newlabelY);
                    labelFrequency.relativePosition = new Vector3(230f, newlabelY);
                    labelHour.relativePosition = new Vector3(-40f, newlabelY);
                    labelMinute.relativePosition = new Vector3(80f, newlabelY);                 
                    labelAutoOccur.relativePosition = new Vector3(230f, newlabelY);

                    // new dropdowns names
                    dropdownHour.name = "DropdownHour";
                    dropdownFrequency.name = "DropdownFrequency";
                    dropdownMinute.name = "DropdownMinute";
                    dropdownAutoOccur.name = "LabelAutoOccur";

                    // new labels names
                    labelHour.name = "LabelHour";
                    labelFrequency.name = "LabelFrequency";
                    labelMinute.name = "LabelMinute";
                    labelAutoOccur.name = "LabelAutoOccur";

                    // new labels texts
                    labelHour.text = localizationProvider.Translate(TranslationKeys.RaceDayLabelHour);
                    labelFrequency.text = localizationProvider.Translate(TranslationKeys.RaceDayLabelFrequency);
                    labelMinute.text = localizationProvider.Translate(TranslationKeys.RaceDayLabelMinute); 
                    labelAutoOccur.text = localizationProvider.Translate(TranslationKeys.RaceDayLabelAutoOccur);

                    // new dropdown items
                    dropdownHour.items = [.. Enumerable.Range(0, 24).Select(i => i.ToString("D2"))];
                    dropdownFrequency.items = [
                        localizationProvider.Translate(TranslationKeys.RaceDayDropDownWeeklyFrequency),
                        localizationProvider.Translate(TranslationKeys.RaceDayDropDownDailyFrequency)
                    ];
                    dropdownMinute.items = [.. Enumerable.Range(0, 60).Select(i => i.ToString("D2"))];
                    dropdownAutoOccur.items = [
                        localizationProvider.Translate(TranslationKeys.RaceDayDropDownDisableAutoOccur),
                        localizationProvider.Translate(TranslationKeys.RaceDayDropDownEnableAutoOccur)
                    ];

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

                        var dropDownHour_action = eventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownHour");
                        var dropDownFrequency_action = eventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownFrequency");
                        var dropDownMinute_action = eventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownMinute");
                        var dropDownAutoOccur_action = eventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownAutoOccur");

                        dropDownHour_action.selectedIndex = eventTimeSchedules[scheduleIndex].StartHour;
                        dropDownFrequency_action.selectedIndex = eventTimeSchedules[scheduleIndex].Frequency;
                        dropDownMinute_action.selectedIndex = eventTimeSchedules[scheduleIndex].StartMinute;
                        dropDownAutoOccur_action.selectedIndex = eventTimeSchedules[scheduleIndex].AutoOccur ? 1 : 0;

                        dropDownHour_action.isEnabled = flag2;
                        dropDownFrequency_action.isEnabled = flag2;
                        dropDownMinute_action.isEnabled = flag2;
                        dropDownAutoOccur_action.isEnabled = flag2;

                        dropDownHour_action.opacity = (!flag2) ? 0.4f : 1f;
                        dropDownFrequency_action.opacity = (!flag2) ? 0.4f : 1f;
                        dropDownMinute_action.opacity = (!flag2) ? 0.4f : 1f;
                        dropDownAutoOccur_action.opacity = (!flag2) ? 0.4f : 1f;

                        var buttonStartNow = panel.Find<UIButton>("ButtonStartNow");
                        buttonStartNow.relativePosition = new Vector3(8f, 80f);
                    }; 
                }


                for (int j = 0; j < ___m_PastEventList.items.Count; j++)
                {
                    var uIPanel2 = ___m_PastEventList.items[j];

                    int index = j;

                    var UpdatePastEventDelegate = Traverse.Create(__instance).Field("UpdatePastEventDelegate").GetValue<UpdatePastEventDelegate>();

                    uIPanel2.objectUserData = (UpdatePastEventDelegate)delegate (ushort eventIndex, ref EventData eventData)
                    {
                        UpdatePastEventDelegate?.Invoke(eventIndex, ref eventData);  // Run game code first

                        var pastEventList = Traverse.Create(__instance).Field("m_PastEventList").GetValue<UITemplateList<UIPanel>>();

                        var labelDate = pastEventList.items[index].Find<UILabel>("LabelDate");
                        labelDate.text = Singleton<SimulationManager>.instance.FrameToTime(eventData.m_startFrame).ToString("dd/MM/yyyy HH:mm");
                    };
                }
            }

            [HarmonyPatch(typeof(RaceEventWorldInfoPanel), "UpdateEventSchedule")]
            [HarmonyPostfix]
            private static void UpdateEventSchedule(ref ushort ___m_eventRouteID, ref UIComponent[] ___m_PanelNextEvent)
            {
                var scheduledEvents = Singleton<EventManager>.instance.m_eventRoutes.m_buffer[___m_eventRouteID].m_scheduledEvents;
                ushort num = Singleton<EventManager>.instance.m_eventRoutes.m_buffer[___m_eventRouteID].m_event;
                int num4 = (num == 0) ? 1 : 0;
                for (int j = 0; j < 5; j++)
                {
                    var uILabel = ___m_PanelNextEvent[j].Find<UILabel>("Date");
                    uILabel.text = scheduledEvents[j + num4].m_startDate.ToString("dd/MM/yyyy HH:mm");
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
                            var dateTime = Singleton<SimulationManager>.instance.m_currentGameTime;
                            buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_scheduleID = ++Singleton<EventManager>.instance.m_eventScheduleCount;
                            buffer[m_eventRouteID].m_scheduleData[scheduleCount].Info = m_allowedEventInfos[0];
                            buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_startDay = (byte)(dateTime.Day - 1);
                            buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_startMonth = (byte)(dateTime.Month - 1);
                            buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_laps = 1;
                            buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_ticketPrice = 100;
                            buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_flags = EventRouteData.EventScheduleFlags.Suspended;
                            EventRouteTimeManager.SetEventTimeScheduleHour(m_eventRouteID, scheduleCount, (byte)dateTime.Hour);
                            EventRouteTimeManager.SetEventTimeScheduleMinute(m_eventRouteID, scheduleCount, (byte)dateTime.Minute);
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

            private static void OnScheduleHourChanged(int scheduleIndex, int value, RaceEventWorldInfoPanel instance)
            {
                ushort routeID = Traverse.Create(instance).Field("m_eventRouteID").GetValue<ushort>();
                var eventConfigs = Traverse.Create(instance).Field("m_EventConfigs").GetValue<UITemplateList<UIPanel>>();
                var eventTimeSchedules = EventRouteTimeManager.GetEventTimeSchedules(routeID);

                var buffer = Singleton<EventManager>.instance.m_eventRoutes.m_buffer;
                var scheduleData = buffer[routeID].m_scheduleData;
                int year = Singleton<SimulationManager>.instance.m_currentGameTime.Year;

                byte startHour = eventTimeSchedules[scheduleIndex].StartHour;
                byte b = (byte)value;
                if (startHour != b)
                {
                    
                    var startDateTime = new DateTime(year, scheduleData[scheduleIndex].m_startMonth + 1, scheduleData[scheduleIndex].m_startDay + 1, b, eventTimeSchedules[scheduleIndex].StartMinute, 0);
                    b = (byte)((byte)AdjustEventStartTime(startDateTime) - 1);

                    EventRouteTimeManager.SetEventTimeScheduleHour(routeID, scheduleIndex, b);
                    RefreshEventSchedule(instance);

                    var uIDropDown = eventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownHour");
                    if (uIDropDown.selectedIndex != b)
                    {
                        uIDropDown.selectedIndex = b;
                    }
                }
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
                byte startMinute = eventTimeSchedules[scheduleIndex].StartMinute;
                byte b = (byte)value;
                if (startMinute != b)
                {
                    var uIDropDown = eventConfigs.items[scheduleIndex].Find<UIDropDown>("DropdownMinute");
                    EventRouteTimeManager.SetEventTimeScheduleMinute(routeID, scheduleIndex, b);
                    RefreshEventSchedule(instance);
                    if (uIDropDown.selectedIndex != b)
                    {
                        uIDropDown.selectedIndex = b;
                    }
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
        }

    }
}
