// RealTimeInfoPanelBase.cs

namespace RealTime.UI
{
    using System.Text;
    using ColossalFramework;
    using ColossalFramework.UI;
    using RealTime.CustomAI;
    // using RealTime.Simulation;
    using SkyTools.Localization;
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
    internal abstract class RealTimeInfoPanelBase<T>(string panelName, RealTimeResidentAI<ResidentAI, Citizen> residentAI, ILocalizationProvider localizationProvider) : CustomInfoPanelBase<T>(panelName)
        where T : WorldInfoPanel
    {
        private const string ComponentId = "RealTimeInfoSchedule";
        private const string AgeEducationLabelName = "AgeEducation";
        private const float LineHeight = 14f;

        private readonly RealTimeResidentAI<ResidentAI, Citizen> residentAI = residentAI ?? throw new System.ArgumentNullException(nameof(residentAI));
        private readonly ILocalizationProvider localizationProvider = localizationProvider ?? throw new System.ArgumentNullException(nameof(localizationProvider));
        // private readonly ITimeInfo timeInfo;
        private UILabel scheduleLabel;
        private CitizenSchedule scheduleCopy;

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
            if (citizenId == 0)
            {
                SetCustomPanelVisibility(scheduleLabel, visible: false);
                return;
            }

            ref var schedule = ref residentAI.GetCitizenSchedule(citizenId);

            var citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];

            if ((citizen.m_flags & Citizen.Flags.Student) != 0 || Citizen.GetAgeGroup(citizen.m_age) == Citizen.AgeGroup.Child || Citizen.GetAgeGroup(citizen.m_age) == Citizen.AgeGroup.Teen)
            {
                if (schedule.LastScheduledState == scheduleCopy.LastScheduledState
                && schedule.ScheduledStateTime == scheduleCopy.ScheduledStateTime
                && schedule.SchoolStatus == scheduleCopy.SchoolStatus
                && schedule.VacationDaysLeft == scheduleCopy.VacationDaysLeft
                && schedule.SchoolClass == scheduleCopy.SchoolClass)
                {
                    return;
                }
            }
            else
            {
                if (schedule.LastScheduledState == scheduleCopy.LastScheduledState
                && schedule.ScheduledStateTime == scheduleCopy.ScheduledStateTime
                && schedule.WorkStatus == scheduleCopy.WorkStatus
                && schedule.VacationDaysLeft == scheduleCopy.VacationDaysLeft
                && schedule.WorkShift == scheduleCopy.WorkShift)
                {
                    return;
                }
            }
            

            if (schedule.LastScheduledState == ResidentState.Ignored)
            {
                return;
            }

            UpdateCitizenState(citizenId, ref schedule);

            SetCustomPanelVisibility(scheduleLabel, false);
            scheduleCopy = schedule;
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
            return true;
        }

        private void BuildTextInfo(uint citizenId, Citizen citizen, ref CitizenSchedule schedule, bool debugMode)
        {
            var info = new StringBuilder(100);
            float labelHeight = 0;

            if(debugMode)
            {
                info.Append("CitizenId").Append(": ").Append(citizenId);
                info.AppendLine();
                labelHeight += LineHeight;
                info.Append("CurrentLocation").Append(": ").Append(citizen.CurrentLocation.ToString());
                info.AppendLine();
                labelHeight += LineHeight;
            }

            if (schedule.LastScheduledState != ResidentState.Unknown)
            {
                string action = localizationProvider.Translate(ScheduledAction + "." + schedule.LastScheduledState.ToString());
                if(schedule.ScheduledMealType != MealType.None)
                {
                    string mealType = localizationProvider.Translate("ScheduledMealType." + schedule.ScheduledMealType.ToString());
                    if (!string.IsNullOrEmpty(mealType))
                    {
                        action += $" {mealType}";
                    }
                }
                if (!string.IsNullOrEmpty(action))
                {
                    info.Append(localizationProvider.Translate(ScheduledAction)).Append(": ").Append(action);
                    labelHeight += LineHeight;
                }
            }

            if (schedule.ScheduledStateTime != default)
            {
                string action = localizationProvider.Translate(NextScheduledAction);
                if (!string.IsNullOrEmpty(action))
                {
                    if (info.Length > 0)
                    {
                        info.AppendLine();
                    }

                    info.Append(action).Append(": ").Append(schedule.ScheduledStateTime.ToString(localizationProvider.CurrentCulture));
                    labelHeight += LineHeight;
                }
            }

            if (schedule.CurrentState != ResidentState.Unknown)
            {
                string action = localizationProvider.Translate(CurrentState + "." + schedule.CurrentState.ToString());
                if (schedule.ScheduledMealType != MealType.None)
                {
                    string mealType = localizationProvider.Translate("ScheduledMealType." + schedule.ScheduledMealType.ToString());
                    if (!string.IsNullOrEmpty(mealType))
                    {
                        action += $" {mealType}";
                    }
                }
                if (!string.IsNullOrEmpty(action))
                {
                    if (info.Length > 0)
                    {
                        info.AppendLine();
                    }

                    info.Append(localizationProvider.Translate(CurrentState)).Append(": ").Append(action);
                    labelHeight += LineHeight;
                }
            }

            //if (schedule.TravelTimeToWork != 0)
            //{
            //    string action = "Travel Time";
            //    if (!string.IsNullOrEmpty(action))
            //    {
            //        if (info.Length > 0)
            //        {
            //            info.AppendLine();
            //        }
            //        info.Append(action).Append(": ").Append(schedule.TravelTimeToWork);
            //        labelHeight += LineHeight;
            //    }
            //}

            //if (schedule.WorkShiftStartHour != 0)
            //{
            //    string action = "Work Start";
            //    if (!string.IsNullOrEmpty(action))
            //    {
            //        if (info.Length > 0)
            //        {
            //            info.AppendLine();
            //        }
            //        var now = timeInfo.Now;
            //        var workStartTime = now.FutureHour(schedule.WorkShiftStartHour);

            //        info.Append(action).Append(": ").Append(workStartTime.ToString("dd/MM/yyyy HH:mm"));
            //        labelHeight += LineHeight;
            //    }
            //}

            //if (schedule.WorkShiftEndHour != 0)
            //{
            //    string action = "Work End";
            //    if (!string.IsNullOrEmpty(action))
            //    {
            //        if (info.Length > 0)
            //        {
            //            info.AppendLine();
            //        }

            //        var now = timeInfo.Now;
            //        var workEndTime = now.FutureHour(schedule.WorkShiftEndHour);

            //        info.Append(action).Append(": ").Append(workEndTime.ToString("dd/MM/yyyy HH:mm"));
            //        labelHeight += LineHeight;
            //    }
            //}

            if ((citizen.m_flags & Citizen.Flags.Student) != 0 || Citizen.GetAgeGroup(citizen.m_age) == Citizen.AgeGroup.Child || Citizen.GetAgeGroup(citizen.m_age) == Citizen.AgeGroup.Teen)
            {
                if (schedule.SchoolClass != SchoolClass.NoSchool)
                {
                    string schoolClass = localizationProvider.Translate(SchoolClassKey + "." + schedule.SchoolClass.ToString());
                    if (!string.IsNullOrEmpty(schoolClass))
                    {
                        if (info.Length > 0)
                        {
                            info.AppendLine();
                        }

                        info.Append(schoolClass);
                        labelHeight += LineHeight;

                        if (schedule.SchoolStatus == SchoolStatus.OnVacation)
                        {
                            string vacation = localizationProvider.Translate(SchoolClassOnVacation);
                            if (!string.IsNullOrEmpty(vacation))
                            {
                                info.Append(' ');
                                info.AppendFormat(vacation, schedule.VacationDaysLeft);
                            }
                        }
                    }
                }
            }
            else
            {
                if (schedule.WorkShift != WorkShift.Unemployed)
                {
                    string workShift = localizationProvider.Translate(WorkShiftKey + "." + schedule.WorkShift.ToString());
                    if (!string.IsNullOrEmpty(workShift))
                    {
                        if (info.Length > 0)
                        {
                            info.AppendLine();
                        }

                        info.Append(workShift);
                        labelHeight += LineHeight;

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
                }
            }
            scheduleLabel.height = labelHeight;
            scheduleLabel.text = info.ToString();
            SetCustomPanelVisibility(scheduleLabel, info.Length > 0);
        }

        private void UpdateCitizenState(uint citizenId, ref CitizenSchedule schedule)
        {
            if (schedule.CurrentState == ResidentState.Ignored)
            {
                return;
            }

            var citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];

            var citizenInstance = Singleton<CitizenManager>.instance.m_instances.m_buffer[citizen.m_instance];

            if ((citizen.m_flags & Citizen.Flags.DummyTraffic) != 0)
            {
                schedule.CurrentState = ResidentState.Ignored;
                return;
            }

            var location = citizen.CurrentLocation;
            if (location == Citizen.Location.Moving)
            {
                if((citizenInstance.m_flags & CitizenInstance.Flags.OnTour) != 0 || (citizenInstance.m_flags & CitizenInstance.Flags.TargetIsNode) != 0)
                {
                    schedule.Hint = ScheduleHint.OnTour;
                }

                schedule.CurrentState = ResidentState.InTransition;
                return;
            }

            ushort currentBuilding = citizen.GetBuildingByLocation();
            if (currentBuilding == 0)
            {
                schedule.CurrentState = ResidentState.Unknown;
                return;
            }

            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[currentBuilding];

            
            if ((building.m_flags & Building.Flags.Evacuating) != 0)
            {
                schedule.CurrentState = ResidentState.Evacuating;
                return;
            }

            var buildingService = building.Info.GetService();
            var buildingSubService = building.Info.GetSubService();
            switch (location)
            {
                case Citizen.Location.Home:
                    schedule.CurrentState = ResidentState.AtHome;
                    return;

                case Citizen.Location.Work:
                    if (citizen.m_visitBuilding == currentBuilding && schedule.WorkStatus != WorkStatus.Working)
                    {
                        // A citizen may visit their own work building (e.g. shopping),
                        // but the game sets the location to 'work' even if the citizen visits the building.
                        goto case Citizen.Location.Visit;
                    }

                    switch (buildingService)
                    {
                        case ItemClass.Service.Electricity:
                        case ItemClass.Service.Water:
                        case ItemClass.Service.HealthCare:
                        case ItemClass.Service.PoliceDepartment:
                        case ItemClass.Service.FireDepartment:
                        case ItemClass.Service.Disaster:
                            if (DisasterManager.instance.IsEvacuating(BuildingManager.instance.m_buildings.m_buffer[currentBuilding].m_position))
                            {
                                schedule.CurrentState = ResidentState.InShelter;
                                return;
                            }

                            break;
                    }

                    schedule.CurrentState = (citizen.m_flags & Citizen.Flags.Student) != 0 || Citizen.GetAgeGroup(citizen.m_age) == Citizen.AgeGroup.Child || Citizen.GetAgeGroup(citizen.m_age) == Citizen.AgeGroup.Teen
                        ? ResidentState.AtSchool
                        : ResidentState.AtWork;
                    return;

                case Citizen.Location.Visit:
                    switch (buildingService)
                    {
                        case ItemClass.Service.Beautification:
                        case ItemClass.Service.Monument:
                        case ItemClass.Service.Tourism:
                        case ItemClass.Service.Commercial
                            when buildingSubService == ItemClass.SubService.CommercialLeisure && schedule.WorkStatus != WorkStatus.Working:
                            if (schedule.LastScheduledState == ResidentState.GoToRelax)
                            {
                                schedule.CurrentState = ResidentState.Relaxing;
                            }
                            else if (schedule.LastScheduledState == ResidentState.GoToMeal)
                            {
                                schedule.CurrentState = ResidentState.EatMeal;
                            }
                            return;

                        case ItemClass.Service.Commercial:
                            if (schedule.WorkStatus == WorkStatus.Working && schedule.LastScheduledState == ResidentState.GoToMeal)
                            {
                                schedule.CurrentState = ResidentState.EatMeal;
                            }
                            else
                            {
                                if (schedule.LastScheduledState == ResidentState.GoShopping)
                                {
                                    schedule.CurrentState = ResidentState.Shopping;
                                }
                                else if (schedule.LastScheduledState == ResidentState.GoToMeal)
                                {
                                    schedule.CurrentState = ResidentState.EatMeal;
                                }
                            }
                            return;

                        case ItemClass.Service.Disaster when schedule.LastScheduledState == ResidentState.GoToShelter:
                            schedule.CurrentState = ResidentState.InShelter;
                            return;
                    }

                    schedule.CurrentState = ResidentState.Visiting;
                    return;
            }

            return;
        }
    }
}
