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

        /// <summary> The maximum travel time (in hours) that can be stored for a citizen. This is used to limit the travel time value and prevent overflow during serialization.</summary>
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

        /// <summary>Gets the time when the citizen's work shift starts.</summary>
        public float WorkShiftStartTime { get; private set; }

        /// <summary>Gets the time when the citizen's work shift ends.</summary>
        public float WorkShiftEndTime { get; private set; }

        /// <summary>The citizen's work shift index. only applicable if <see cref="WorkShift.Assigned"/> is used.</summary>
        public int ShiftIndex { get; private set; }

        /// <summary>Gets the citizen's school class.</summary>
        public SchoolClass SchoolClass { get; private set; }

        /// <summary>Gets the time when the citizen's school class starts.</summary>
        public float SchoolClassStartTime { get; private set; }

        /// <summary>Gets the time when the citizen's school class ends.</summary>
        public float SchoolClassEndTime { get; private set; }

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
        /// <param name="startTime">The work shift start time.</param>
        /// <param name="endTime">The work shift end time.</param>
        /// <param name="worksOnWeekends">if <c>true</c>, the citizen works on weekends.</param>
        public void UpdateWorkShift(WorkShift workShift, int shiftIndex, float startTime, float endTime)
        {
            WorkShift = workShift;
            ShiftIndex = workShift == WorkShift.Assigned ? shiftIndex : -1;
            WorkShiftStartTime = startTime;
            WorkShiftEndTime = endTime;
        }

        /// <summary>Updates the school class data for this citizen's schedule.</summary>
        /// <param name="schoolClass">The citizen's school class.</param>
        /// <param name="startTime">The school class start time.</param>
        /// <param name="endTime">The school class end time.</param>
        public void UpdateSchoolClass(SchoolClass schoolClass, float startTime, float endTime)
        {
            SchoolClass = schoolClass;
            SchoolClassStartTime = startTime;
            SchoolClassEndTime = endTime;
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
                UpdateWorkShiftHours(WorkShift, -1, workBuilding);
            }
            if(SchoolClass != SchoolClass.NoSchool)
            {
                UpdateSchoolClassHours(SchoolClass);
            }
        }

        public void UpdateWorkShiftHours(WorkShift workShift, int shiftIndex, ushort workBuildingId)
        {
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

            float workBegin;
            float workEnd;

            if (workShift == WorkShift.Unemployed)
            {
                UpdateWorkShift(workShift, -1, 0, 0);
            }

            if (workShift == WorkShift.Assigned)
            {
                workTime.WorkShifts[shiftIndex].GetShiftHours(out workBegin, out workEnd);
                UpdateWorkShift(workShift, shiftIndex, workBegin, workEnd);
                return;
            }

            // legacy support for work shifts - if the work shift is not assigned, the shift hours will be determined by the default rules based on the building service and work shift type
            shiftIndex = workShift switch
            {
                WorkShift.First or WorkShift.ContinuousDay => 0,
                WorkShift.Second or WorkShift.ContinuousNight => 1,
                WorkShift.Night => 2,
                _ => 0,
            };
            workTime.WorkShifts[shiftIndex].GetShiftHours(out workBegin, out workEnd);
            UpdateWorkShift(workShift, shiftIndex, workBegin, workEnd);
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
