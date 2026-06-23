// WorkBehavior.cs

namespace RealTime.CustomAI
{
    using System;
    using RealTime.Config;
    using RealTime.Events;
    using RealTime.GameConnection;
    using RealTime.Simulation;
    using SkyTools.Tools;

    /// <summary>
    /// A class containing methods for managing the citizens' work behavior.
    /// </summary>
    /// <remarks>Initializes a new instance of the <see cref="WorkBehavior"/> class.</remarks>
    /// <param name="config">The configuration to run with.</param>
    /// <param name="randomizer">The randomizer implementation.</param>
    /// <param name="buildingManager">The building manager implementation.</param>
    /// <param name="timeInfo">The time information source.</param>
    /// <param name="travelBehavior">A behavior that provides simulation info for the citizens traveling.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    internal sealed class WorkBehavior(
        RealTimeConfig config,
        IRandomizer randomizer,
        IBuildingManagerConnection buildingManager,
        ITimeInfo timeInfo,
        ITravelBehavior travelBehavior,
        IRealTimeEventManager eventManager) : IWorkBehavior
    {
        private readonly RealTimeConfig config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly IRandomizer randomizer = randomizer ?? throw new ArgumentNullException(nameof(randomizer));
        private readonly IBuildingManagerConnection buildingManager = buildingManager ?? throw new ArgumentNullException(nameof(buildingManager));
        private readonly ITimeInfo timeInfo = timeInfo ?? throw new ArgumentNullException(nameof(timeInfo));
        private readonly ITravelBehavior travelBehavior = travelBehavior ?? throw new ArgumentNullException(nameof(travelBehavior));
        private readonly IRealTimeEventManager eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));

        private DateTime lunchBegin;
        private DateTime lunchEnd;

        /// <summary>Notifies this object that a new game day starts.</summary>
        public void BeginNewDay()
        {
            var today = timeInfo.Now.Date;
            lunchBegin = today.AddHours(config.LunchBegin);
            lunchEnd = today.AddHours(config.LunchEnd);
        }

        /// <summary>Updates the citizen's work shift parameters in the specified citizen's <paramref name="schedule"/>.</summary>
        /// <param name="schedule">The citizen's schedule to update the work shift in.</param>
        /// <param name="citizenAge">The age of the citizen.</param>
        /// <param name="chosenWorkShiftIndex">The index of the work shift chosen by the user in the building's info panel.</param>
        public void UpdateWorkShift(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge, int chosenWorkShiftIndex)
        {
            if (schedule.WorkBuilding == 0 || citizenAge == Citizen.AgeGroup.Senior || chosenWorkShiftIndex == -1)
            {
                schedule.UpdateWorkShift(WorkShift.Unemployed, -1, 0, 0);
                return;
            }

            schedule.UpdateWorkShiftHours(WorkShift.Assigned, chosenWorkShiftIndex, schedule.WorkBuilding);          
        }

        /// <summary>Check if the citizen should go to work</summary>
        /// <param name="schedule">The citizen's schedule.</param>
        /// <returns><c>true</c> if the citizen should go to work; otherwise, <c>false</c>.</returns>
        public bool ShouldScheduleGoToWork(ref CitizenSchedule schedule)
        {
            if (schedule.CurrentState == ResidentState.EatMeal)
            {
                Log.Debug(LogCategory.Schedule, $"  - current status is {schedule.CurrentState} and the meal type is {schedule.LastScheduledMealType}");
            }
            else
            {
                Log.Debug(LogCategory.Schedule, $"  - current status is {schedule.CurrentState}");
            }

            if (schedule.CurrentState == ResidentState.AtWork)
            {
                return false;
            }

            float halfShiftLength = (schedule.WorkShiftEndTime - schedule.WorkShiftStartTime) / 2;

            Log.Debug(LogCategory.Schedule, $"  - halfShiftLength is {halfShiftLength} and current hour is {timeInfo.CurrentHour}");

            Log.Debug(LogCategory.Schedule, $"  - result is {timeInfo.CurrentHour + halfShiftLength < schedule.WorkShiftEndTime}");

            return timeInfo.CurrentHour + halfShiftLength < schedule.WorkShiftEndTime;
        }

        /// <summary>Updates the citizen's work schedule by determining the time for going to work.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="currentBuilding">The ID of the building where the citizen is currently located.</param>
        /// <param name="simulationCycle">The duration (in hours) of a full citizens simulation cycle.</param>
        /// <returns>The time when going to work</returns>
        public DateTime ScheduleGoToWorkTime(ref CitizenSchedule schedule, ushort currentBuilding, float simulationCycle)
        {
            var now = timeInfo.Now;

            float travelTime = GetTravelTimeToWork(ref schedule, currentBuilding);

            var workEndTime = now.FutureHour(schedule.WorkShiftEndTime);  
            var departureTime = now.FutureHour(schedule.WorkShiftStartTime - travelTime - simulationCycle);

            Log.Debug(LogCategory.Schedule, $"  - works shift start time is {schedule.WorkShiftStartTime}, works shift end time is {schedule.WorkShiftEndTime}");
            Log.Debug(LogCategory.Schedule, $"  - travel time is {travelTime}, workEndTime is {workEndTime}, simulationCycle is {simulationCycle}, departureTime is {departureTime}");

            if (departureTime > workEndTime && now.AddHours(travelTime + simulationCycle) < workEndTime)
            {
                departureTime = now;
            }

            Log.Debug(LogCategory.Schedule, $"  - new departureTime is {departureTime}");

            return departureTime;
        }

        /// <summary>Updates the citizen's work schedule by determining the lunch time.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="citizenAge">The citizen's age.</param>
        /// <param name="mealType">The meal type the citizen is going to eat.</param>
        /// <returns><c>true</c> if a lunch time was scheduled; otherwise, <c>false</c>.</returns>
        public bool ScheduleMeal(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge, MealType mealType)
        {
            if (mealType == MealType.Breakfast)
            {
                float minGoToBreakfastHour = config.WakeUpHour;
                float maxGoToBreakfastHour = schedule.WorkShiftStartTime;

                Log.Debug(LogCategory.Schedule, $"  - Work status is {schedule.WorkStatus}, working in shift {schedule.WorkShift}");
                if (schedule.WorkStatus == WorkStatus.None
                    && (schedule.WorkShift == WorkShift.First || schedule.WorkShift == WorkShift.ContinuousDay)
                    && timeInfo.CurrentHour >= minGoToBreakfastHour && timeInfo.CurrentHour <= maxGoToBreakfastHour
                    && WillGoToMeal(citizenAge, mealType))
                {
                    schedule.Schedule(ResidentState.GoToMeal, mealType);
                    return true;
                }

                return false;
            }
            else if (mealType == MealType.Lunch)
            {
                int hours = (int)(lunchBegin - timeInfo.Now).TotalHours;

                if (hours >= 2.5 && schedule.WorkStatus == WorkStatus.Working
                    && (schedule.WorkShift == WorkShift.First || schedule.WorkShift == WorkShift.ContinuousDay)
                    && WillGoToMeal(citizenAge, mealType))
                {
                    schedule.Schedule(ResidentState.GoToMeal, lunchBegin, mealType);
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>Updates the citizen's work schedule by determining the returning from meal time.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        public void ScheduleReturnFromMeal(ref CitizenSchedule schedule)
        {
            if (schedule.ScheduledMealType == MealType.Lunch && schedule.WorkStatus == WorkStatus.Working)
            {
                schedule.Schedule(ResidentState.GoToWork, lunchEnd);
            }
        }

        /// <summary>Updates the citizen's work schedule by determining the time for returning from work.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="citizenAge">The age of the citizen.</param>
        public void ScheduleReturnFromWork(uint citizenId, ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge)
        {
            if (schedule.WorkStatus != WorkStatus.Working)
            {
                return;
            }

            Log.Debug(LogCategory.Schedule, timeInfo.Now, $"The Citizen {citizenId} end work time is {schedule.WorkShiftEndTime} and current hour is {timeInfo.CurrentHour}");
            
            float time = 0;
            if (timeInfo.CurrentHour - schedule.WorkShiftEndTime > 0)
            {
                time = timeInfo.CurrentHour - (schedule.WorkShiftEndTime + GetOvertime(citizenAge));
            }

            Log.Debug(LogCategory.Schedule, timeInfo.Now, $"The Citizen {citizenId} time is {time}");

            float departureHour = schedule.WorkShiftEndTime + GetOvertime(citizenAge) + time;

            Log.Debug(LogCategory.Schedule, timeInfo.Now, $"The Citizen {citizenId} departureHour is {departureHour}");

            if (departureHour < timeInfo.CurrentHour)
            {
                departureHour = timeInfo.CurrentHour;
            }

            Log.Debug(LogCategory.Schedule, timeInfo.Now, $"The Citizen {citizenId} departureHour is {departureHour} and future hour is {timeInfo.Now.FutureHour(departureHour):dd.MM.yy HH:mm}");

            if(WillGoToMeal(citizenAge, MealType.Supper))
            {
                schedule.Schedule(ResidentState.GoToMeal, timeInfo.Now.FutureHour(departureHour));
            }
            else
            {
                schedule.Schedule(ResidentState.Unknown, timeInfo.Now.FutureHour(departureHour));
            }  
        }

        private float GetTravelTimeToWork(ref CitizenSchedule schedule, ushort buildingId)
        {
            float result = schedule.CurrentState == ResidentState.AtHome ? schedule.TravelTimeToWork : 0;

            Log.Debug(LogCategory.Schedule, $"  - schedule CurrentState is {schedule.CurrentState}, schedule TravelTimeToWork is {schedule.TravelTimeToWork}, result is {result}");

            if (result <= 0)
            {
                result = travelBehavior.GetEstimatedTravelTime(buildingId, schedule.WorkBuilding);
            }

            return result;
        }

        private bool WillGoToMeal(Citizen.AgeGroup citizenAge, MealType mealType)
        {
            switch (citizenAge)
            {
                case Citizen.AgeGroup.Child:
                case Citizen.AgeGroup.Teen:
                case Citizen.AgeGroup.Senior:
                    return false;
            }

            if (mealType == MealType.Breakfast)
            {
                Log.Debug(LogCategory.Schedule, $"  - citizen age is {citizenAge}, BreakfastQuota is {config.BreakfastBeforeWorkOrSchoolQuota}");
                if (!config.IsBreakfastTimeEnabledBeforeWorkOrSchool)
                {
                    return false;
                }
                return randomizer.ShouldOccur(config.BreakfastBeforeWorkOrSchoolQuota);
            }
            else if (mealType == MealType.Lunch)
            {
                Log.Debug(LogCategory.Schedule, $"  - citizen age is {citizenAge}, LunchQuota is {config.LunchDuringWorkOrSchoolQuota}");
                if (!config.IsLunchTimeEnabledDuringWorkOrSchool)
                {
                    return false;
                }
                return randomizer.ShouldOccur(config.LunchDuringWorkOrSchoolQuota);
            }
            else if (mealType == MealType.Supper)
            {
                Log.Debug(LogCategory.Schedule, $"  - citizen age is {citizenAge}, SupperQuota is {config.SupperAfterWorkOrSchoolQuota}");
                if (!config.IsSupperTimeEnabledAfterWorkOrSchool)
                {
                    return false;
                }
                return randomizer.ShouldOccur(config.SupperAfterWorkOrSchoolQuota);
            }

            return false;
        }

        private float GetOvertime(Citizen.AgeGroup citizenAge)
        {
            switch (citizenAge)
            {
                case Citizen.AgeGroup.Young:
                case Citizen.AgeGroup.Adult:
                    return randomizer.ShouldOccur(config.OnTimeQuota)
                        ? 0
                        : config.MaxOvertime * randomizer.GetRandomValue(100u) / 100f;

                default:
                    return 0;
            }
        }
    }
}
