// SchoolBehavior.cs

namespace RealTime.CustomAI
{
    using System;
    using ColossalFramework;
    using RealTime.Config;
    using RealTime.Simulation;
    using SkyTools.Tools;

    /// <summary>
    /// A class containing methods for managing the citizens' school behavior.
    /// </summary>
    /// <remarks>Initializes a new instance of the <see cref="SchoolBehavior"/> class.</remarks>
    /// <param name="config">The configuration to run with.</param>
    /// <param name="randomizer">The randomizer implementation.</param>
    /// <param name="buildingManager">The building manager implementation.</param>
    /// <param name="timeInfo">The time information source.</param>
    /// <param name="travelBehavior">A behavior that provides simulation info for the citizens traveling.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    internal sealed class SchoolBehavior(
        RealTimeConfig config,
        IRandomizer randomizer,
        ITimeInfo timeInfo,
        ITravelBehavior travelBehavior) : ISchoolBehavior
    {
        private readonly RealTimeConfig config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly IRandomizer randomizer = randomizer ?? throw new ArgumentNullException(nameof(randomizer));
        private readonly ITimeInfo timeInfo = timeInfo ?? throw new ArgumentNullException(nameof(timeInfo));
        private readonly ITravelBehavior travelBehavior = travelBehavior ?? throw new ArgumentNullException(nameof(travelBehavior));

        private DateTime lunchBegin;
        private DateTime lunchEnd;

        public void BeginNewDay()
        {
            var today = timeInfo.Now.Date;
            lunchBegin = today.AddHours(config.LunchBegin);
            lunchEnd = today.AddHours(config.LunchEnd);
        }

        /// <summary>Updates the citizen's school class parameters in the specified citizen's <paramref name="schedule"/>.</summary>
        /// <param name="schedule">The citizen's schedule to update the school class in.</param>
        /// <param name="citizenAge">The age of the citizen.</param>
        public void UpdateSchoolClass(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge)
        {
            if (schedule.SchoolBuilding == 0 || citizenAge == Citizen.AgeGroup.Senior)
            {
                schedule.UpdateSchoolClass(SchoolClass.NoSchool, 0, 0);
                return;
            }

            var schoolBuilding = BuildingManager.instance.m_buildings.m_buffer[schedule.SchoolBuilding];

            var level = schoolBuilding.Info.m_class.m_level;

            float schoolBegin = config.SchoolBegin;
            float schoolEnd = config.SchoolEnd;

            SchoolClass schoolClass;

            switch (level)
            {
                case ItemClass.Level.Level1:
                case ItemClass.Level.Level2:
                    schoolClass = SchoolClass.DayClass;
                    break;

                case ItemClass.Level.Level3:
                    schoolClass = randomizer.ShouldOccur(config.NightClassQuota) ? SchoolClass.NightClass : SchoolClass.DayClass;
                    break;

                default:
                    return;
            }

            switch (schoolClass)
            {
                case SchoolClass.DayClass:
                    break;

                case SchoolClass.NightClass:
                    schoolBegin = config.SchoolEnd;
                    schoolEnd = 20;
                    break;
            }

            schedule.UpdateSchoolClass(schoolClass, schoolBegin, schoolEnd);
        }

        /// <summary>Check if the citizen should go to school</summary>
        /// <param name="schedule">The citizen's schedule.</param>
        /// <returns><c>true</c> if the citizen should go to school; otherwise, <c>false</c>.</returns>
        public bool ShouldScheduleGoToSchool(ref CitizenSchedule schedule)
        {
            Log.Debug(LogCategory.Schedule, $"  - current status is {schedule.CurrentState}");

            if (schedule.CurrentState == ResidentState.AtSchool)
            {
                return false;
            }

            Log.Debug(LogCategory.Schedule, $"  - option IsWeekendEnabled is {config.IsWeekendEnabled}, weekend is {timeInfo.Now.IsWeekend()}");

            if (config.IsWeekendEnabled && timeInfo.Now.IsWeekend())
            {
                return false;
            }

            float halfClassLength = (schedule.SchoolClassEndHour - schedule.SchoolClassStartHour) / 2;

            Log.Debug(LogCategory.Schedule, $"  - halfClassLength is {halfClassLength} and current hour is {timeInfo.CurrentHour}");

            Log.Debug(LogCategory.Schedule, $"  - result is {timeInfo.CurrentHour + halfClassLength < schedule.SchoolClassEndHour}");

            return timeInfo.CurrentHour + halfClassLength < schedule.SchoolClassEndHour;
        }

        /// <summary>Updates the citizen's school schedule by determining the time for going to school.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="currentBuilding">The ID of the building where the citizen is currently located.</param>
        /// <param name="simulationCycle">The duration (in hours) of a full citizens simulation cycle.</param>
        /// <returns>The time when going to school</returns>
        public DateTime ScheduleGoToSchoolTime(ref CitizenSchedule schedule, ushort currentBuilding, float simulationCycle)
        {
            var now = timeInfo.Now;

            float travelTime = GetTravelTimeToSchool(ref schedule, currentBuilding);

            var schoolEndTime = now.FutureHour(schedule.SchoolClassEndHour);
            var departureTime = now.FutureHour(schedule.SchoolClassStartHour - travelTime - simulationCycle);

            Log.Debug(LogCategory.Schedule, $"  - school start hour is {schedule.SchoolClassStartHour}, school end hour is {schedule.SchoolClassEndHour}");
            Log.Debug(LogCategory.Schedule, $"  - travel time is {travelTime}, schoolEndTime is {schoolEndTime}, simulationCycle is {simulationCycle}, departureTime is {departureTime}");

            if (departureTime > schoolEndTime && now.AddHours(travelTime + simulationCycle) < schoolEndTime)
            {
                departureTime = now;
            }

            Log.Debug(LogCategory.Schedule, $"  - new departureTime is {departureTime}");

            return departureTime;
        }

        /// <summary>Updates the citizen's school schedule by determining the meal time.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="schoolBuilding">The citizen's school building.</param>
        /// <param name="mealType">The meal type the citizen is going to eat.</param>
        /// <returns><c>true</c> if a meal was scheduled; otherwise, <c>false</c>.</returns>
        public bool ScheduleMeal(ref CitizenSchedule schedule, ushort schoolBuilding, MealType mealType)
        {
            if (mealType == MealType.Breakfast)
            {
                float minGoToBreakfastHour = config.WakeUpHour;
                float maxGoToBreakfastHour = schedule.SchoolClassStartHour;

                Log.Debug(LogCategory.Schedule, $"  - School status is {schedule.SchoolStatus}");
                if (schedule.SchoolStatus == SchoolStatus.None
                    && schedule.SchoolClass == SchoolClass.DayClass
                    && timeInfo.CurrentHour >= minGoToBreakfastHour && timeInfo.CurrentHour <= maxGoToBreakfastHour
                    && WillGoToMeal(schoolBuilding, mealType))
                {
                    schedule.Schedule(ResidentState.GoToMeal, mealType);
                    return true;
                }

                return false;
            }
            else if (mealType == MealType.Lunch)
            {
                int hours = (int)(lunchBegin - timeInfo.Now).TotalHours;

                Log.Debug(LogCategory.Schedule, $"  - School status is {schedule.SchoolStatus}");
                if (hours >= 2.5 && schedule.SchoolStatus == SchoolStatus.Studying
                    && schedule.SchoolClass == SchoolClass.DayClass
                    && WillGoToMeal(schoolBuilding, mealType))
                {
                    schedule.Schedule(ResidentState.GoToMeal, lunchBegin, mealType);
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>Updates the citizen's school schedule by determining the returning from lunch time.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        public void ScheduleReturnFromMeal(ref CitizenSchedule schedule)
        {
            if (schedule.ScheduledMealType == MealType.Lunch && schedule.SchoolStatus == SchoolStatus.Studying)
            {
                schedule.Schedule(ResidentState.GoToSchool, lunchEnd);
            }
        }

        /// <summary>Updates the citizen's school schedule by determining the time for returning from school.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        public void ScheduleReturnFromSchool(uint citizenId, ref CitizenSchedule schedule)
        {
            if (schedule.SchoolStatus != SchoolStatus.Studying)
            {
                return;
            }

            Log.Debug(LogCategory.Schedule, timeInfo.Now, $"The Citizen {citizenId} end school hour is {schedule.SchoolClassEndHour} and current hour is {timeInfo.CurrentHour}");

            float time = 0;
            if (timeInfo.CurrentHour - schedule.SchoolClassEndHour > 0)
            {
                time = timeInfo.CurrentHour - schedule.SchoolClassEndHour;
            }

            Log.Debug(LogCategory.Schedule, timeInfo.Now, $"The Citizen {citizenId} time is {time}");

            float departureHour = schedule.SchoolClassEndHour + time;

            Log.Debug(LogCategory.Schedule, timeInfo.Now, $"The Citizen {citizenId} departureHour is {departureHour}");

            if (departureHour < timeInfo.CurrentHour)
            {
                departureHour = timeInfo.CurrentHour;
            }

            Log.Debug(LogCategory.Schedule, timeInfo.Now, $"The Citizen {citizenId} departureHour is {departureHour} and future hour is {timeInfo.Now.FutureHour(departureHour):dd.MM.yy HH:mm}");

            if (WillGoToMeal(schedule.SchoolBuilding, MealType.Supper))
            {
                schedule.Schedule(ResidentState.GoToMeal, timeInfo.Now.FutureHour(departureHour));
            }
            else
            {
                schedule.Schedule(ResidentState.Unknown, timeInfo.Now.FutureHour(departureHour));
            }
        }

        private float GetTravelTimeToSchool(ref CitizenSchedule schedule, ushort buildingId)
        {
            float result = schedule.CurrentState == ResidentState.AtHome ? schedule.TravelTimeToSchool : 0;

            Log.Debug(LogCategory.Schedule, $"  - schedule CurrentState is {schedule.CurrentState}, schedule TravelTimeToSchool is {schedule.TravelTimeToSchool}, result is {result}");

            if (result <= 0)
            {
                result = travelBehavior.GetEstimatedTravelTime(buildingId, schedule.SchoolBuilding);
            }

            return result;
        }

        private bool WillGoToMeal(ushort schoolBuildingId, MealType mealType)
        {
            var schoolBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[schoolBuildingId];
            if (schoolBuilding.Info.GetAI() is not CampusBuildingAI)
            {
                return false;
            }

            if (mealType == MealType.Breakfast)
            {
                Log.Debug(LogCategory.Schedule, $"  - BreakfastQuota is {config.BreakfastBeforeWorkOrSchoolQuota}");
                if (!config.IsBreakfastTimeEnabledBeforeWorkOrSchool)
                {
                    return false;
                }
                return randomizer.ShouldOccur(config.BreakfastBeforeWorkOrSchoolQuota);
            }
            else if (mealType == MealType.Lunch)
            {
                Log.Debug(LogCategory.Schedule, $"  - LunchQuota is {config.LunchDuringWorkOrSchoolQuota}");
                if (!config.IsLunchTimeEnabledDuringWorkOrSchool)
                {
                    return false;
                }
                return randomizer.ShouldOccur(config.LunchDuringWorkOrSchoolQuota);
            }
            else if (mealType == MealType.Supper)
            {
                Log.Debug(LogCategory.Schedule, $"  - SupperQuota is {config.SupperAfterWorkOrSchoolQuota}");
                if (!config.IsSupperTimeEnabledAfterWorkOrSchool)
                {
                    return false;
                }
                return randomizer.ShouldOccur(config.SupperAfterWorkOrSchoolQuota);
            }

            return false;
        }

    }
}
