// WorkBehavior.cs

namespace RealTime.CustomAI
{
    using System;
    using RealTime.Config;
    using RealTime.Simulation;
    using SkyTools.Tools;

    /// <summary>
    /// A class containing methods for managing the citizens' work behavior.
    /// </summary>
    /// <remarks>Initializes a new instance of the <see cref="WorkBehavior"/> class.</remarks>
    /// <param name="config">The configuration to run with.</param>
    /// <param name="randomizer">The randomizer implementation.</param>
    /// <param name="timeInfo">The time information source.</param>
    /// <param name="travelBehavior">A behavior that provides simulation info for the citizens traveling.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    internal sealed class WorkBehavior(
        RealTimeConfig config,
        IRandomizer randomizer,
        ITimeInfo timeInfo,
        ITravelBehavior travelBehavior) : IWorkBehavior
    {
        private readonly RealTimeConfig config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly IRandomizer randomizer = randomizer ?? throw new ArgumentNullException(nameof(randomizer));
        private readonly ITimeInfo timeInfo = timeInfo ?? throw new ArgumentNullException(nameof(timeInfo));
        private readonly ITravelBehavior travelBehavior = travelBehavior ?? throw new ArgumentNullException(nameof(travelBehavior));

        /// <summary>Notifies this object that a new game day starts.</summary>
        public void BeginNewDay()
        {
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
            schedule.Schedule(ResidentState.Unknown, timeInfo.Now.FutureHour(departureHour));
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

        private float GetOvertime(Citizen.AgeGroup citizenAge) => citizenAge switch
        {
            Citizen.AgeGroup.Young or Citizen.AgeGroup.Adult => randomizer.ShouldOccur(config.OnTimeQuota)
                                    ? 0
                                    : config.MaxOvertime * randomizer.GetRandomValue(100u) / 100f,
            _ => 0,
        };
    }
}
