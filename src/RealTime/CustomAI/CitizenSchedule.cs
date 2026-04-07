// CitizenSchedule.cs

namespace RealTime.CustomAI
{
    using System;
    using RealTime.Core;
    using RealTime.Managers;
    using static Constants;

    /// <summary>A container struct that holds information about the detailed resident citizen state.
    /// Note that this struct is intentionally made mutable to increase performance.</summary>
    internal struct CitizenSchedule
    {
        /// <summary>The size of the buffer in bytes to store the data.</summary>
        public const int DataRecordSize = 8;

        /// <summary>The citizen's current state.</summary>
        public ResidentState CurrentState;

        /// <summary>The citizen's schedule hint.</summary>
        public ScheduleHint Hint;

        /// <summary>The ID of the building where an event takes place, if the citizen schedules to attend one.</summary>
        public ushort EventBuilding;

        /// <summary>The citizen's work status.</summary>
        public WorkStatus WorkStatus;

        /// <summary>The citizen's school status.</summary>
        public SchoolStatus SchoolStatus;

        /// <summary>The citizen find visit place attempts.</summary>
        public int FindVisitPlaceAttempts;

        /// <summary>The number of days the citizen will be on vacation (including the current day).</summary>
        public byte VacationDaysLeft;

        /// <summary>The ID of the citizen's work building. If it doesn't equal the game's value, the work shift data needs to be updated.</summary>
        public ushort WorkBuilding;

        /// <summary>The ID of the citizen's school building. If it doesn't equal the game's value, the school data needs to be updated.</summary>
        public ushort SchoolBuilding;

        /// <summary>The time when citizen started their last journey.</summary>
        public DateTime DepartureTime;

        private const float TravelTimeMultiplier = ushort.MaxValue / MaxTravelTime;

        /// <summary>Gets the citizen's next scheduled state.</summary>
        public ResidentState ScheduledState { get; private set; }

        /// <summary>Gets the citizen's previous scheduled state.</summary>
        public ResidentState LastScheduledState { get; private set; }

        /// <summary>Gets the time when the citizen will perform the next state change.</summary>
        public DateTime ScheduledStateTime { get; private set; }

        /// <summary>Gets the citizen's next scheduled meal type.</summary>
        public MealType ScheduledMealType { get; private set; }

        /// <summary>Gets the citizen's previous scheduled meal type.</summary>
        public MealType LastScheduledMealType { get; private set; }

        /// <summary>
        /// Gets the travel time (in hours) from citizen's home to the work building. The maximum value is
        /// determined by the <see cref="MaxTravelTime"/> constant.
        /// </summary>
        public float TravelTimeToWork { get; private set; }

        /// <summary>
        /// Gets the travel time (in hours) from citizen's home to the school building. The maximum value is
        /// determined by the <see cref="MaxTravelTime"/> constant.
        /// </summary>
        public float TravelTimeToSchool { get; private set; }

        /// <summary>Gets the citizen's work shift.</summary>
        public WorkShift WorkShift { get; private set; }

        /// <summary>Gets the daytime hour when the citizen's work shift starts.</summary>
        public float WorkShiftStartHour { get; private set; }

        /// <summary>Gets the daytime hour when the citizen's work shift ends.</summary>
        public float WorkShiftEndHour { get; private set; }

        /// <summary>Gets a value indicating whether this citizen works on weekends.</summary>
        public bool WorksOnWeekends { get; private set; }

        /// <summary>Gets the citizen's school class.</summary>
        public SchoolClass SchoolClass { get; private set; }

        /// <summary>Gets the daytime hour when the citizen's school class starts.</summary>
        public float SchoolClassStartHour { get; private set; }

        /// <summary>Gets the daytime hour when the citizen's school class ends.</summary>
        public float SchoolClassEndHour { get; private set; }

        /// <summary>Updates the travel time that the citizen needs to read the work building.</summary>
        /// <param name="arrivalTime">
        /// The arrival time at the work building. Must be great than <see cref="DepartureTime"/>.
        /// </param>
        public void UpdateTravelTimeToWork(DateTime arrivalTime)
        {
            if (arrivalTime < DepartureTime || DepartureTime == default)
            {
                return;
            }

            float onTheWayHours = (float)(arrivalTime - DepartureTime).TotalHours;
            if (onTheWayHours > MaxTravelTime)
            {
                onTheWayHours = MaxTravelTime;
            }

            TravelTimeToWork = TravelTimeToWork == 0 ? onTheWayHours : (TravelTimeToWork + onTheWayHours) / 2;
        }

        /// <summary>Updates the travel time that the citizen needs to read the school building.</summary>
        /// <param name="arrivalTime">
        /// The arrival time at the school building. Must be great than <see cref="DepartureTime"/>.
        /// </param>
        public void UpdateTravelTimeToSchool(DateTime arrivalTime)
        {
            if (arrivalTime < DepartureTime || DepartureTime == default)
            {
                return;
            }

            float onTheWayHours = (float)(arrivalTime - DepartureTime).TotalHours;
            if (onTheWayHours > MaxTravelTime)
            {
                onTheWayHours = MaxTravelTime;
            }

            TravelTimeToSchool = TravelTimeToSchool == 0 ? onTheWayHours : (TravelTimeToSchool + onTheWayHours) / 2;
        }

        /// <summary>Updates the work shift data for this citizen's schedule.</summary>
        /// <param name="workShift">The citizen's work shift.</param>
        /// <param name="startHour">The work shift start hour.</param>
        /// <param name="endHour">The work shift end hour.</param>
        /// <param name="worksOnWeekends">if <c>true</c>, the citizen works on weekends.</param>
        public void UpdateWorkShift(WorkShift workShift, float startHour, float endHour, bool worksOnWeekends)
        {
            WorkShift = workShift;
            WorkShiftStartHour = startHour;
            WorkShiftEndHour = endHour;
            WorksOnWeekends = worksOnWeekends;
        }

        /// <summary>Updates the school class data for this citizen's schedule.</summary>
        /// <param name="schoolClass">The citizen's school class.</param>
        /// <param name="startHour">The school class start hour.</param>
        /// <param name="endHour">The school class end hour.</param>
        public void UpdateSchoolClass(SchoolClass schoolClass, float startHour, float endHour)
        {
            SchoolClass = schoolClass;
            SchoolClassStartHour = startHour;
            SchoolClassEndHour = endHour;
        }

        /// <summary>Schedules next actions for the citizen with a specified action time.</summary>
        /// <param name="nextState">The next scheduled citizen's state.</param>
        /// <param name="nextStateTime">The time when the scheduled state must change.</param>
        public void Schedule(ResidentState nextState, DateTime nextStateTime, MealType mealType = MealType.None)
        {
            LastScheduledState = ScheduledState;
            ScheduledState = nextState;
            ScheduledStateTime = nextStateTime;
            LastScheduledMealType = ScheduledMealType;
            ScheduledMealType = mealType;
        }

        /// <summary>Schedules next actions for the citizen with no action time (ASAP).</summary>
        /// <param name="nextState">The next scheduled citizen's state.</param>
        public void Schedule(ResidentState nextState, MealType mealType = MealType.None)
        {
            // Note: not calling the overload to avoid additional method call - this method will be called frequently
            LastScheduledState = ScheduledState;
            ScheduledState = nextState;
            ScheduledStateTime = default;
            LastScheduledMealType = ScheduledMealType;
            ScheduledMealType = mealType;
        }

        /// <summary>Updates the schedule state for this citizen.</summary>
        /// <param name="scheduledState">The citizen's schedule state.</param>
        /// <param name="lastScheduledState">The citizen's last schedule state.</param>
        /// <param name="scheduledStateTime">The citizen's schedule state time.</param>
        public void UpdateScheduleState(ResidentState scheduledState, ResidentState lastScheduledState, DateTime scheduledStateTime, MealType mealType, MealType lastScheduledMealType)
        {
            ScheduledState = scheduledState;
            LastScheduledState = lastScheduledState;
            ScheduledStateTime = scheduledStateTime;
            ScheduledMealType = mealType;
            LastScheduledMealType = lastScheduledMealType;
        }

        /// <summary>Updates the travel time to work for this citizen.</summary>
        /// <param name="travelTimeToWork">The citizen's schedule state.</param>
        public void UpdateTravelTimeToWork(float travelTimeToWork) => TravelTimeToWork = travelTimeToWork;

        /// <summary>Updates the travel time to school for this citizen.</summary>
        /// <param name="travelTimeToSchool">The citizen's schedule state.</param>
        public void UpdateTravelTimeToSchool(float travelTimeToSchool) => TravelTimeToSchool = travelTimeToSchool;

        /// <summary>Reads this instance from the specified source buffer.</summary>
        /// <param name="source">The source buffer. Must have length of <see cref="DataRecordSize"/> elements.</param>
        /// <param name="referenceTime">The reference time (in ticks) to use for time deserialization.</param>
        public void Read(byte[] source, long referenceTime, ushort workBuilding)
        {
            WorkShift = (WorkShift)(source[0] & 0xF);
            WorkStatus = (WorkStatus)(source[0] >> 4);
            ScheduledState = (ResidentState)(source[1] & 0xF);

            if(ScheduledState == ResidentState.GoToBreakfast || ScheduledState == ResidentState.GoToLunch)
            {
                ScheduledState = ResidentState.GoToMeal;
            }

            if (ScheduledState == ResidentState.Breakfast || ScheduledState == ResidentState.Lunch)
            {
                ScheduledState = ResidentState.EatMeal;
            }

            VacationDaysLeft = (byte)(source[1] >> 4);

            int minutes = source[2] + (source[3] << 8);
            ScheduledStateTime = minutes == 0 ? default : new DateTime(minutes * TimeSpan.TicksPerMinute + referenceTime);

            int travelTime = source[4] + (source[5] << 8);
            TravelTimeToWork = travelTime / TravelTimeMultiplier;

            SchoolClass = (SchoolClass)(source[6] & 0xF);
            SchoolStatus = (SchoolStatus)(source[6] >> 4);

            FindVisitPlaceAttempts = source[7] & 0xF;

            if (WorkShift != WorkShift.Unemployed && WorkShift != WorkShift.Event && workBuilding != 0)
            {
                UpdateWorkShiftHours(WorkShift, workBuilding);
            }
            if(SchoolClass != SchoolClass.NoSchool)
            {
                UpdateSchoolClassHours(SchoolClass);
            }
        }

        public void UpdateWorkShiftHours(WorkShift workShift, ushort workBuildingId)
        {
            var config = RealTimeMod.configProvider.Configuration;

            var workBuildingInfo = BuildingManager.instance.m_buildings.m_buffer[workBuildingId].Info;

            BuildingWorkTimeManager.WorkTime workTime;

            if (!BuildingWorkTimeManager.BuildingWorkTimeExist(workBuildingId))
            {
                if (!BuildingWorkTimeManager.ShouldHaveBuildingWorkTime(workBuildingId))
                {
                    return;
                }
                workTime = BuildingWorkTimeManager.CreateBuildingWorkTime(workBuildingId, workBuildingInfo);
            }
            else
            {
                workTime = BuildingWorkTimeManager.GetBuildingWorkTime(workBuildingId);
            }

            float workBegin = config.WorkBegin;
            float workEnd = config.WorkEnd;

            var service = workBuildingInfo.m_class.m_service;

            switch (workShift)
            {
                case WorkShift.First when workTime.HasExtendedWorkShift:
                    float extendedShiftBegin = Math.Min(config.SchoolBegin, config.WakeUpHour);
                    if (service == ItemClass.Service.Education || service == ItemClass.Service.PlayerEducation) // teachers
                    {
                        workBegin = Math.Min(EarliestWakeUp, extendedShiftBegin);
                    }
                    else
                    {
                        extendedShiftBegin = config.WakeUpHour;
                        workBegin = Math.Min(EarliestWakeUp, extendedShiftBegin);
                    }
                    workEnd = config.SchoolEnd;
                    break;

                case WorkShift.First:
                    workBegin = config.WorkBegin;
                    workEnd = config.WorkEnd;
                    break;

                case WorkShift.Second:
                    if (service == ItemClass.Service.Education || service == ItemClass.Service.PlayerEducation) // night class at university (teacher)
                    {
                        workBegin = config.SchoolEnd;
                        workEnd = 22f;
                    }
                    else
                    {
                        workBegin = config.WorkEnd;
                        workEnd = config.GoToSleepHour;
                    }
                    break;

                case WorkShift.Night:
                    workBegin = config.GoToSleepHour;
                    workEnd = config.WorkBegin;
                    break;

                case WorkShift.ContinuousDay:
                    workBegin = 8f;
                    workEnd = 20f;
                    break;

                case WorkShift.ContinuousNight:
                    workBegin = 20f;
                    workEnd = 8f;
                    break;

            }

            UpdateWorkShift(workShift, workBegin, workEnd, workTime.WorkAtWeekands);
        }

        public void UpdateSchoolClassHours(SchoolClass schoolClass)
        {
            var config = RealTimeMod.configProvider.Configuration;

            float schoolBegin = config.SchoolBegin;
            float schoolEnd = config.SchoolEnd;

            switch (schoolClass)
            {
                case SchoolClass.DayClass:
                    schoolBegin = config.SchoolBegin;
                    schoolEnd = config.SchoolEnd;
                    break;

                case SchoolClass.NightClass:
                    schoolBegin = config.SchoolEnd;
                    schoolEnd = 20;
                    break;
            }

            UpdateSchoolClass(schoolClass, schoolBegin, schoolEnd);
        }
    }
}
