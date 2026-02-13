// WorldInfoPanelPatch.cs

namespace RealTime.Patches
{
    using ColossalFramework.UI;
    using ColossalFramework;
    using System;
    using HarmonyLib;
    using RealTime.CustomAI;
    using RealTime.UI;
    using RealTime.Events;
    using RealTime.GameConnection;
    using UnityEngine;
    using System.Linq;
    using SkyTools.Localization;
    using RealTime.Config;
    using System.Text;
    using RealTime.Utils;
    using RealTime.Localization;
    using System.Reflection;
    using System.Collections.Generic;
    using RealTime.Managers;
    using System.Runtime.CompilerServices;

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
        internal static class VarsitySportsArenaPanelPatch
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
        internal static class HotelWorldInfoPanelPatch
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
                if (buttonStartEvent != null)
                {
                    buttonStartEvent.text = "Schedule";
                }
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
        internal static class FestivalPanelPatch
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
        private sealed class ZonedBuildingWorldInfoPanelPatch
        {
            private static BuildingOperationHoursUIPanel zonedBuildingOperationHoursUIPanel;

            private static UILabel s_hotelLabel;

            [HarmonyPatch(typeof(ZonedBuildingWorldInfoPanel), "OnSetTarget")]
            [HarmonyPostfix]
            private static void OnSetTarget()
            {
                if (zonedBuildingOperationHoursUIPanel == null)
                {
                    ZonedCreateUI();
                }
                zonedBuildingOperationHoursUIPanel.UpdateBuildingData();
            }

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
                    s_hotelLabel = UiUtils.CreateLabel(infoPanel.component, 65f, 280f, "Rooms Ocuppied", textScale: 0.75f);
                    s_hotelLabel.textColor = new Color32(185, 221, 254, 255);
                    s_hotelLabel.font = Resources.FindObjectsOfTypeAll<UIFont>().FirstOrDefault((UIFont f) => f.name == "OpenSans-Regular");

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

            private static void ZonedCreateUI()
            {
                var m_zonedBuildingWorldInfoPanel = GameObject.Find("(Library) ZonedBuildingWorldInfoPanel").GetComponent<ZonedBuildingWorldInfoPanel>();
                var makeHistoricalPanel = m_zonedBuildingWorldInfoPanel.Find("MakeHistoricalPanel").GetComponent<UIPanel>();
                if (makeHistoricalPanel == null)
                {
                    return;
                }
                zonedBuildingOperationHoursUIPanel = new BuildingOperationHoursUIPanel(m_zonedBuildingWorldInfoPanel, makeHistoricalPanel, 350f, 6f, 0f, localizationProvider);
            }
        }

        [HarmonyPatch]
        private sealed class CityServiceWorldInfoPanelPatch
        {
            private static BuildingOperationHoursUIPanel cityServiceOperationHoursUIPanel;

            private static UILabel s_visitorsLabel;

            private static UIButton m_endYearButton;

            [HarmonyPatch(typeof(CityServiceWorldInfoPanel), "OnSetTarget")]
            [HarmonyPostfix]
            private static void OnSetTarget(InstanceID ___m_InstanceID)
            {
                var buildingAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[___m_InstanceID.Building].Info.m_buildingAI;
                if (cityServiceOperationHoursUIPanel == null || s_visitorsLabel == null || m_endYearButton == null)
                {
                    CityServiceCreateUI(buildingAI);
                }
                float checkBoxYposition = 16f;
                if (buildingAI is ExtractingFacilityAI || buildingAI is ProcessingFacilityAI)
                {
                    checkBoxYposition = 0f;
                }
                cityServiceOperationHoursUIPanel.CheckBoxYposition = checkBoxYposition;
                cityServiceOperationHoursUIPanel.UpdateBuildingData();
            }

            [HarmonyPatch(typeof(CityServiceWorldInfoPanel), "UpdateBindings")]
            [HarmonyPostfix]
            private static void UpdateBindings()
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
                if(RealTimeConfig.DebugMode && buildingInfo.GetAI() is MainCampusBuildingAI)
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
                    if(academicYearData.DidLastYearEnd)
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

            private static void CityServiceCreateUI(BuildingAI buildingAI)
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
                float checkBoxXposition = 320f;
                float checkBoxYposition = 16f;
                float panelHeight = 0f;
                if (buildingAI is ExtractingFacilityAI || buildingAI is ProcessingFacilityAI)
                {
                    checkBoxXposition = 360f;
                    checkBoxYposition = 0f;
                    panelHeight = 40f;
                }

                cityServiceOperationHoursUIPanel ??= new BuildingOperationHoursUIPanel(m_cityServiceWorldInfoPanel, buttonPanels, checkBoxXposition, checkBoxYposition, panelHeight, localizationProvider);
                if (s_visitorsLabel == null)
                {
                    s_visitorsLabel = UiUtils.CreateLabel(buttonPanels, 65f, 280f, "Visitors", textScale: 0.75f);
                    s_visitorsLabel.textColor = new Color32(185, 221, 254, 255);
                    s_visitorsLabel.font = Resources.FindObjectsOfTypeAll<UIFont>().FirstOrDefault((UIFont f) => f.name == "OpenSans-Regular");
                    s_visitorsLabel.relativePosition = new Vector2(200f, 26f);
                }
                if (m_endYearButton == null)
                {
                    string endYearButtonText = localizationProvider.Translate(TranslationKeys.AcademicYearEndYearButtonText);
                    string endYearButtonTooltipText = localizationProvider.Translate(TranslationKeys.AcademicYearEndYearButtonTooltip);
                    m_endYearButton = UiUtils.CreateButton(buttonPanels, 133f, 19.5f, "EndYear", endYearButtonText, endYearButtonTooltipText);
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

                if(eventData.Info.GetAI() is AcademicYearAI)
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
                var m_activeCampusAreas = (List<byte>)typeof(DistrictManager).GetField("m_activeCampusAreas", BindingFlags.Static | BindingFlags.NonPublic).GetValue(instance);
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
                typeof(DistrictManager).GetField("m_activeCampusAreas", BindingFlags.Static | BindingFlags.NonPublic).SetValue(instance, m_activeCampusAreas);
                academicYearReportPanel.PopupPanel(m_activeCampusAreas, 0, wasTriggeredByButton: true);
                var campusWorldInfoPanel = UIView.library.Get<CampusWorldInfoPanel>("CampusWorldInfoPanel");
                if (campusWorldInfoPanel.component.isVisible)
                {
                    campusWorldInfoPanel.OnAcademicYearEnded();
                };
            }
        }

        [HarmonyPatch]
        private sealed class UniqueFactoryWorldInfoPanelPatch
        {
            private static BuildingOperationHoursUIPanel uniqueFactoryOperationHoursUIPanel;

            [HarmonyPatch(typeof(UniqueFactoryWorldInfoPanel), "OnSetTarget")]
            [HarmonyPostfix]
            private static void OnSetTarget()
            {
                if (uniqueFactoryOperationHoursUIPanel == null)
                {
                    UniqueFactoryCreateUI();
                }
                uniqueFactoryOperationHoursUIPanel.UpdateBuildingData();
            }

            private static void UniqueFactoryCreateUI()
            {
                var m_uniqueFactoryWorldInfoPanel = GameObject.Find("(Library) UniqueFactoryWorldInfoPanel").GetComponent<UniqueFactoryWorldInfoPanel>();
                var IncomeExpensesSection = m_uniqueFactoryWorldInfoPanel?.Find("IncomeExpensesSection").GetComponent<UIPanel>();
                if (IncomeExpensesSection == null)
                {
                    return;
                } 
                uniqueFactoryOperationHoursUIPanel ??= new BuildingOperationHoursUIPanel(m_uniqueFactoryWorldInfoPanel, IncomeExpensesSection, 320f, 0f, 0f, localizationProvider);
            }
        }

        [HarmonyPatch]
        private sealed class WarehouseWorldInfoPanelPatch
        {
            private static BuildingOperationHoursUIPanel warehouseOperationHoursUIPanel;

            [HarmonyPatch(typeof(WarehouseWorldInfoPanel), "OnSetTarget")]
            [HarmonyPostfix]
            private static void OnSetTarget(InstanceID ___m_InstanceID)
            {
                var buildingAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[___m_InstanceID.Building].Info.m_buildingAI;
                if (warehouseOperationHoursUIPanel == null)
                {
                    WarehouseCreateUI(buildingAI);
                }
                float checkBoxYposition = 500f;
                if (buildingAI is WarehouseAI warehouse && warehouse.m_storageType == TransferManager.TransferReason.None)
                {
                    checkBoxYposition = 550f;
                }
                warehouseOperationHoursUIPanel.CheckBoxYposition = checkBoxYposition;
                warehouseOperationHoursUIPanel.UpdateBuildingData();
            }

            private static void WarehouseCreateUI(BuildingAI buildingAI)
            {
                var m_warehouseWorldInfoPanel = GameObject.Find("(Library) WarehouseWorldInfoPanel").GetComponent<WarehouseWorldInfoPanel>();
                var WarehousePanel = GameObject.Find("(Library) WarehouseWorldInfoPanel").GetComponent<UIPanel>();
                if (WarehousePanel == null)
                {
                    return;
                }
                float checkBoxYposition = 500f;
                if (buildingAI is WarehouseAI warehouse && warehouse.m_storageType == TransferManager.TransferReason.None)
                {
                    checkBoxYposition = 550f;
                }
                warehouseOperationHoursUIPanel ??= new BuildingOperationHoursUIPanel(m_warehouseWorldInfoPanel, WarehousePanel, 320f, checkBoxYposition, 0f, localizationProvider);
            }
        }

        [HarmonyPatch]
        private sealed class LivingCreatureWorldInfoPanelPatch
        {
            private static UIButton m_clearScheduleButton;

            [HarmonyPatch(typeof(LivingCreatureWorldInfoPanel), "OnSetTarget")]
            [HarmonyPostfix]
            private static void OnSetTarget(ref InstanceID ___m_InstanceID)
            {
                if(___m_InstanceID.Citizen != 0)
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
                m_clearScheduleButton = UiUtils.CreateButton(citizenInfoPanel.component, -10f, 90f, "ClearSchedule", "", "Clear the citizen schedule", 30, 30);
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
                if(citizen.GetAI() is ResidentAI)
                {
                    RealTimeResidentAI.ClearCitizenSchedule(citizenID);
                }
            }
        }
    }
}
