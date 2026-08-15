// RealTimeInfoPanelBase.cs

namespace RealTime.UI
{
    using System;
    using System.Text;
    using ColossalFramework;
    using ColossalFramework.UI;
    using RealTime.CustomAI;
    using SkyTools.Localization;
    using SkyTools.Tools;
    using SkyTools.UI;
    using static Localization.TranslationKeys;

    /// <summary>A base class for the customized world info panels.</summary>
    /// <typeparam name="T">The type of the game world info panel to customize.</typeparam>
    /// <remarks>Initializes a new instance of the <see cref="RealTimeInfoPanelBase{T}"/> class.</remarks>
    /// <param name="panelName">Name of the game's panel object.</param>
    /// <param name="residentAI">The custom resident AI.</param>
    /// <param name="localizationProvider">The localization provider to use for text translation.</param>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="residentAI"/> or <paramref name="localizationProvider"/> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="panelName"/> is null or an empty string.
    /// </exception>
    internal abstract class RealTimeInfoPanelBase<T>(string panelName, RealTimeResidentAI<ResidentAI, Citizen> residentAI, ILocalizationProvider localizationProvider) : CustomInfoPanelBase<T>(panelName) where T : WorldInfoPanel
    {
        private const string ComponentId = "RealTimeInfoSchedule";
        private const string AgeEducationLabelName = "AgeEducation";
        private const float LineHeight = 14f;

        private readonly RealTimeResidentAI<ResidentAI, Citizen> residentAI = residentAI ?? throw new System.ArgumentNullException(nameof(residentAI));
        private readonly ILocalizationProvider localizationProvider = localizationProvider ?? throw new System.ArgumentNullException(nameof(localizationProvider));

        // private readonly ITimeInfo timeInfo;
        private UILabel scheduleLabel;

        // The panel is reused. This cache must always be associated with a citizen.
        private uint cachedCitizenId;
        private Citizen.Location cachedLocation;
        private CitizenSchedule scheduleCopy;
        private bool hasCachedDisplayState;
        private int updateCounter;

        /// <summary>Disables the custom citizen info panel, if it is enabled.</summary>
        protected sealed override void DisableCore()
        {
            if (scheduleLabel == null)
            {
                return;
            }

            ItemsPanel.RemoveUIComponent(scheduleLabel);
            UnityEngine.Object.Destroy(scheduleLabel.gameObject);
            scheduleLabel = null;
        }

        /// <summary>Updates the citizen information for the citizen with specified ID.</summary>
        /// <param name="citizenId">The citizen ID.</param>
        /// <param name="debugMode">debugMode.</param>
        protected void UpdateCitizenInfo(uint citizenId, bool debugMode)
        {
            updateCounter++;

            if (citizenId == 0)
            {
                DebugPanel("invalid citizen ID: 0");
                ClearCustomPanelState();
                HideCustomPanel();
                return;
            }

            var citizenManager = Singleton<CitizenManager>.instance;

            if (citizenId >= citizenManager.m_citizens.m_buffer.Length)
            {
                DebugPanel($"citizen ID out of range: {citizenId}");
                ClearCustomPanelState();
                HideCustomPanel();
                return;
            }

            var citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];

            if ((citizen.m_flags & Citizen.Flags.Created) == 0)
            {
                DebugPanel($"citizen is not created: {citizenId}");
                ClearCustomPanelState();
                HideCustomPanel();
                return;
            }

            bool citizenChanged = cachedCitizenId != 0 && cachedCitizenId != citizenId;

            if (citizenChanged)
            {
                DebugPanel($"citizen changed {cachedCitizenId} -> {citizenId}; invalidating panel cache");

                hasCachedDisplayState = false;
                scheduleCopy = default;
            }

            ref var schedule = ref residentAI.GetCitizenSchedule(citizenId);

            if (schedule.LastScheduledState == ResidentState.Ignored)
            {
                DebugPanel($"citizen {citizenId} is ignored");
                cachedCitizenId = citizenId;
                hasCachedDisplayState = false;
                HideCustomPanel();
                return;
            }

            // Update values that this panel displays before comparing the cache.
            // CurrentState is derived here and was previously excluded from the
            // early-return comparison.
            UpdateCitizenState(citizenId, citizen, ref schedule);

            var currentLocation = citizen.CurrentLocation;

            bool changed = !hasCachedDisplayState || cachedCitizenId != citizenId || !HasSameDisplayedState(citizen, currentLocation, schedule, scheduleCopy, cachedLocation);

            if (!changed)
            {
                DebugPanel($"citizen {citizenId}: display state unchanged");
                return;
            }

            cachedCitizenId = citizenId;
            cachedLocation = currentLocation;
            scheduleCopy = schedule;
            hasCachedDisplayState = true;

            DebugPanel($"citizen {citizenId}: rebuilding panel; location={currentLocation}, last={schedule.LastScheduledState}, next={schedule.ScheduledState}, current={schedule.CurrentState}");

            BuildTextInfo(citizenId, citizen, ref schedule, debugMode);
        }

        /// <summary>Builds up the custom UI objects for the info panel.</summary>
        /// <returns><c>true</c> on success; otherwise, <c>false</c>.</returns>
        protected sealed override bool InitializeCore()
        {
            var statusLabel = ItemsPanel.Find<UILabel>(AgeEducationLabelName);

            if (statusLabel == null)
            {
                return false;
            }

            scheduleLabel = UIComponentTools.CreateCopy(statusLabel, ItemsPanel, ComponentId);

            scheduleLabel.width = 270;
            scheduleLabel.zOrder = statusLabel.zOrder + 1;
            scheduleLabel.isVisible = false;
            scheduleLabel.text = string.Empty;
            scheduleLabel.height = 0;

            ClearCustomPanelState();
            return true;
        }

        private static bool HasSameDisplayedState(Citizen citizen, Citizen.Location currentLocation, in CitizenSchedule left, in CitizenSchedule right, Citizen.Location previousLocation)
        {
            if (currentLocation != previousLocation)
            {
                return false;
            }

            if (left.LastScheduledState != right.LastScheduledState ||
                left.ScheduledState != right.ScheduledState ||
                left.CurrentState != right.CurrentState ||
                left.LastScheduledMealType != right.LastScheduledMealType ||
                left.ScheduledMealType != right.ScheduledMealType ||
                left.ScheduledStateTime != right.ScheduledStateTime ||
                left.VacationDaysLeft != right.VacationDaysLeft)
            {
                return false;
            }

            bool isStudent = IsStudent(citizen);

            if (isStudent)
            {
                return left.SchoolStatus == right.SchoolStatus && left.SchoolClass == right.SchoolClass;
            }

            return left.WorkStatus == right.WorkStatus && left.WorkShift == right.WorkShift && left.ShiftIndex == right.ShiftIndex;
        }

        private static bool IsStudent(Citizen citizen)
        {
            var ageGroup = Citizen.GetAgeGroup(citizen.m_age);
            return (citizen.m_flags & Citizen.Flags.Student) != 0 || ageGroup == Citizen.AgeGroup.Child || ageGroup == Citizen.AgeGroup.Teen;
        }

        private void BuildTextInfo(uint citizenId, Citizen citizen, ref CitizenSchedule schedule, bool debugMode)
        {
            if (scheduleLabel == null)
            {
                return;
            }

            var info = new StringBuilder(256);
            float labelHeight = 0;

            if (debugMode)
            {
                AppendLine(info, ref labelHeight, "CitizenId", citizenId);
                AppendLine(info, ref labelHeight, "CurrentLocation", citizen.CurrentLocation);
                AppendLine(info, ref labelHeight, "CitizenInstance", citizen.m_instance);
                AppendLine(info, ref labelHeight, "VisitBuilding", citizen.m_visitBuilding);
                AppendLine(info, ref labelHeight, "HomeBuilding", citizen.m_homeBuilding);
                AppendLine(info, ref labelHeight, "WorkBuilding", citizen.m_workBuilding);
                AppendLine(info, ref labelHeight, "PanelUpdate", updateCounter);

                if (TryGetCitizenInstance(citizenId, citizen, out var instance))
                {
                    AppendLine(info, ref labelHeight, "InstanceCitizen", instance.m_citizen);
                    AppendLine(info, ref labelHeight, "InstanceFlags", instance.m_flags);
                    AppendLine(info, ref labelHeight, "InstanceTargetBuilding", instance.m_targetBuilding);
                }
                else
                {
                    AppendLine(info, ref labelHeight, "Instance", "Invalid");
                }
            }

            if (schedule.LastScheduledState != ResidentState.Unknown)
            {
                string action = TranslateScheduledAction(schedule.LastScheduledState, schedule.LastScheduledMealType);
                AppendTranslatedLine(info, ref labelHeight, CurrentPlannedAction, action);
            }

            if (schedule.ScheduledStateTime != default)
            {
                string label = localizationProvider.Translate(NextScheduledActionTime);
                string value = schedule.ScheduledStateTime.ToString("t", localizationProvider.CurrentCulture);
                AppendLine(info, ref labelHeight, label, value);
            }

            if (schedule.ScheduledState != ResidentState.Unknown)
            {
                string action = TranslateScheduledAction(schedule.ScheduledState, schedule.ScheduledMealType);
                AppendTranslatedLine(info, ref labelHeight, NextScheduledAction, action);
            }

            if (schedule.CurrentState != ResidentState.Unknown)
            {
                string action = localizationProvider.Translate(CurrentState + "." + schedule.CurrentState);

                if (schedule.CurrentState == ResidentState.EatMeal && schedule.LastScheduledMealType != MealType.None)
                {
                    string mealType = localizationProvider.Translate("MealType." + schedule.LastScheduledMealType);

                    if (!string.IsNullOrEmpty(mealType))
                    {
                        action += " " + mealType;
                    }
                }

                AppendTranslatedLine(info, ref labelHeight, CurrentState, action);
            }

            AppendSchoolOrWorkInfo(info, ref labelHeight, citizen, ref schedule);

            scheduleLabel.height = labelHeight;
            scheduleLabel.text = info.ToString();
            SetCustomPanelVisibility(scheduleLabel, info.Length > 0);
        }

        private string TranslateScheduledAction(ResidentState state, MealType mealType)
        {
            string action = localizationProvider.Translate("ScheduledAction." + state);

            if (state == ResidentState.GoToMeal && mealType != MealType.None)
            {
                string translatedMeal = localizationProvider.Translate("MealType." + mealType);

                if (!string.IsNullOrEmpty(translatedMeal))
                {
                    action += " " + translatedMeal;
                }
            }

            return action;
        }

        private void AppendSchoolOrWorkInfo(StringBuilder info, ref float labelHeight, Citizen citizen, ref CitizenSchedule schedule)
        {
            if (IsStudent(citizen))
            {
                if (schedule.SchoolClass == SchoolClass.NoSchool)
                {
                    return;
                }

                string schoolClass = localizationProvider.Translate(
                    SchoolClassKey + "." + schedule.SchoolClass);

                if (string.IsNullOrEmpty(schoolClass))
                {
                    return;
                }

                AppendLine(info, ref labelHeight, null, schoolClass);

                if (schedule.SchoolStatus == SchoolStatus.OnVacation)
                {
                    string vacation = localizationProvider.Translate(SchoolClassOnVacation);

                    if (!string.IsNullOrEmpty(vacation))
                    {
                        info.Append(' ');
                        info.AppendFormat(vacation, schedule.VacationDaysLeft);
                    }
                }

                return;
            }

            if (schedule.WorkShift == WorkShift.Unemployed || schedule.ShiftIndex == -1)
            {
                return;
            }

            int shift = schedule.ShiftIndex + 1;
            string workShift = localizationProvider.Translate(WorkShiftKey + "." + schedule.WorkShift);

            if (string.IsNullOrEmpty(workShift))
            {
                return;
            }

            AppendLine(info, ref labelHeight, null, workShift + " " + shift);

            if (schedule.WorkStatus == WorkStatus.OnVacation)
            {
                string vacation = localizationProvider.Translate(WorkStatusOnVacation);

                if (!string.IsNullOrEmpty(vacation))
                {
                    info.Append(' ');
                    info.AppendFormat(vacation, schedule.VacationDaysLeft);
                }
            }
        }

        private void UpdateCitizenState(uint citizenId, Citizen citizen, ref CitizenSchedule schedule)
        {
            var timeNow = SimulationManager.instance.m_currentGameTime;

            if ((citizen.m_flags & Citizen.Flags.DummyTraffic) != 0)
            {
                schedule.CurrentState = ResidentState.Ignored;
                return;
            }

            if (!TryGetCitizenInstance(citizenId, citizen, out var citizenInstance))
            {
                schedule.CurrentState = ResidentState.Unknown;
                DebugState(timeNow, citizenId, "invalid or mismatched citizen instance");
                return;
            }

            var location = citizen.CurrentLocation;

            DebugState(timeNow, citizenId, $"location={location}, instance={citizen.m_instance}, instanceCitizen={citizenInstance.m_citizen}, instanceFlags={citizenInstance.m_flags}");

            if (location == Citizen.Location.Moving)
            {
                if((citizenInstance.m_flags & CitizenInstance.Flags.OnTour) != 0 || (citizenInstance.m_flags & CitizenInstance.Flags.TargetIsNode) != 0)
                {
                    schedule.Hint = ScheduleHint.OnTour;
                }

                Log.Debug(LogCategory.State, timeNow, $"UpdateCitizenInfo - Citizen {citizenId} CurrentState is {schedule.CurrentState}");
                schedule.CurrentState = ResidentState.InTransition;
                return;
            }

            ushort currentBuilding = citizen.GetBuildingByLocation();

            if (currentBuilding == 0 || currentBuilding >= Singleton<BuildingManager>.instance.m_buildings.m_buffer.Length)
            {
                schedule.CurrentState = ResidentState.Unknown;
                DebugState(timeNow, citizenId, "no valid current building");
                return;
            }

            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[currentBuilding];

            if (building.Info == null)
            {
                schedule.CurrentState = ResidentState.Unknown;
                DebugState(timeNow, citizenId, "current building has no info");
                return;
            }

            if ((building.m_flags & Building.Flags.Evacuating) != 0)
            {
                schedule.CurrentState = ResidentState.Evacuating;
                return;
            }

            var service = building.Info.GetService();
            var subService = building.Info.GetSubService();

            switch (location)
            {
                case Citizen.Location.Home:
                    schedule.CurrentState = ResidentState.AtHome;
                    return;

                case Citizen.Location.Work:
                    if (citizen.m_visitBuilding == currentBuilding && schedule.WorkStatus != WorkStatus.Working)
                    {
                        // The game may report Work while the citizen is visiting
                        // their own workplace. Treat that case as Visit.
                        HandleVisitState(schedule, service, subService);
                        return;
                    }

                    if (IsShelterService(service) && DisasterManager.instance.IsEvacuating(building.m_position))
                    {
                        schedule.CurrentState = ResidentState.InShelter;
                        return;
                    }

                    schedule.CurrentState = (citizen.m_flags & Citizen.Flags.Student) != 0 ? ResidentState.AtSchool : ResidentState.AtWork;
                    return;

                case Citizen.Location.Visit:
                    HandleVisitState(schedule, service, subService);
                    return;

                default:
                    schedule.CurrentState = ResidentState.Unknown;
                    return;
            }
        }

        private static void HandleVisitState(CitizenSchedule schedule, ItemClass.Service service, ItemClass.SubService subService)
        {
            if ((service == ItemClass.Service.Beautification ||
                 service == ItemClass.Service.Monument ||
                 service == ItemClass.Service.Tourism ||
                 service == ItemClass.Service.Commercial &&
                 subService == ItemClass.SubService.CommercialLeisure) &&
                schedule.WorkStatus != WorkStatus.Working)
            {
                if (schedule.LastScheduledState == ResidentState.GoToRelax)
                {
                    schedule.CurrentState = ResidentState.Relaxing;
                }
                else if (schedule.LastScheduledState == ResidentState.GoToMeal)
                {
                    schedule.CurrentState = ResidentState.EatMeal;
                }
                else
                {
                    schedule.CurrentState = ResidentState.Visiting;
                }

                return;
            }

            if (service == ItemClass.Service.Commercial)
            {
                if (schedule.LastScheduledState == ResidentState.GoShopping)
                {
                    schedule.CurrentState = ResidentState.Shopping;
                }
                else if (schedule.LastScheduledState == ResidentState.GoToMeal)
                {
                    schedule.CurrentState = ResidentState.EatMeal;
                }
                else
                {
                    schedule.CurrentState = ResidentState.Visiting;
                }

                return;
            }

            if (service == ItemClass.Service.Disaster && schedule.LastScheduledState == ResidentState.GoToShelter)
            {
                schedule.CurrentState = ResidentState.InShelter;
                return;
            }

            schedule.CurrentState = ResidentState.Visiting;
        }

        private static bool IsShelterService(ItemClass.Service service) => service == ItemClass.Service.Electricity ||
                   service == ItemClass.Service.Water ||
                   service == ItemClass.Service.HealthCare ||
                   service == ItemClass.Service.PoliceDepartment ||
                   service == ItemClass.Service.FireDepartment ||
                   service == ItemClass.Service.Disaster;

        private static bool TryGetCitizenInstance(uint citizenId, Citizen citizen, out CitizenInstance instance)
        {
            instance = default;

            ushort instanceId = citizen.m_instance;
            var manager = Singleton<CitizenManager>.instance;

            if (instanceId == 0 || instanceId >= manager.m_instances.m_buffer.Length)
            {
                return false;
            }

            instance = manager.m_instances.m_buffer[instanceId];

            return (instance.m_flags & CitizenInstance.Flags.Created) != 0 && instance.m_citizen == citizenId;
        }

        private void ClearCustomPanelState()
        {
            cachedCitizenId = 0;
            cachedLocation = default;
            scheduleCopy = default;
            hasCachedDisplayState = false;
        }

        private void HideCustomPanel()
        {
            if (scheduleLabel == null)
            {
                return;
            }

            scheduleLabel.text = string.Empty;
            scheduleLabel.height = 0;
            SetCustomPanelVisibility(scheduleLabel, false);
        }

        private static void AppendTranslatedLine(StringBuilder info, ref float labelHeight, string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            AppendLine(info, ref labelHeight, key, value);
        }

        private static void AppendLine(StringBuilder info, ref float labelHeight, string label, object value)
        {
            if (info.Length > 0)
            {
                info.AppendLine();
            }

            if (!string.IsNullOrEmpty(label))
            {
                info.Append(label).Append(": ");
            }

            info.Append(value);
            labelHeight += LineHeight;
        }

        private void DebugPanel(string message) => Log.Debug(LogCategory.State, SimulationManager.instance.m_currentGameTime, $"InfoPanel update #{updateCounter}: {message}");

        private static void DebugState(DateTime time, uint citizenId, string message) => Log.Debug(LogCategory.State, time, $"UpdateCitizenState - citizen {citizenId}: {message}");
    }
}
