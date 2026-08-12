// MealBehavior.cs

namespace RealTime.CustomAI
{
    using System;
    using System.Collections.Generic;
    using RealTime.Config;
    using RealTime.Simulation;
    using SkyTools.Tools;

    /// <summary>
    /// A class containing methods for managing the citizens' meal behavior.
    /// </summary>
    /// <remarks>Initializes a new instance of the <see cref="MealBehavior"/> class.</remarks>
    /// <param name="config">The configuration to run with.</param>
    /// <param name="randomizer">The randomizer implementation.</param>
    /// <param name="timeInfo">The time information source.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    internal sealed class MealBehavior(
        RealTimeConfig config,
        IRandomizer randomizer,
        ITimeInfo timeInfo,
        ISpareTimeBehavior spareTimeBehavior) : IMealBehavior
    {
        private readonly RealTimeConfig config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly IRandomizer randomizer = randomizer ?? throw new ArgumentNullException(nameof(randomizer));
        private readonly ITimeInfo timeInfo = timeInfo ?? throw new ArgumentNullException(nameof(timeInfo));
        private readonly ISpareTimeBehavior spareTimeBehavior = spareTimeBehavior ?? throw new ArgumentNullException(nameof(spareTimeBehavior));

        internal readonly struct MealWindow(MealType mealType, float begin, float duration, float end, uint quota, bool enabled)
        {
            public MealType MealType { get; } = mealType;
            public float Begin { get; } = begin;
            public float Duration { get; } = duration;
            public float End { get; } = end;
            public uint Quota { get; } = quota;
            public bool Enabled { get; } = enabled;
        }

        internal readonly struct ScheduledMealOpportunity(MealType mealType, DateTime beginTime, DateTime endTime, float overlapHours, float score)
        {
            public MealType MealType { get; } = mealType;
            public DateTime BeginTime { get; } = beginTime;
            public DateTime EndTime { get; } = endTime;
            public float OverlapHours { get; } = overlapHours;
            public float Score { get; } = score;
        }

        /// <summary>Notifies this object that a new game day starts.</summary>
        public void BeginNewDay()
        {
        }

        /// <summary>Check if the citizen should go to eat a normal meal.</summary>
        /// <param name="schedule">The citizen's schedule.</param>
        /// <param name="citizenAge">The citizen's age group.</param>
        /// <param name="mealType">The citizen's meal type.</param>
        /// <returns><c>true</c> if the citizen should go to eat a normal meal; otherwise, <c>false</c>.</returns>
        public bool ShouldScheduleMeal(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge, MealType mealType)
        {
            schedule.ResetDailyMealsIfNeeded(timeInfo.Now.DayOfYear);

            if (schedule.CurrentState == ResidentState.EatMeal)
            {
                Log.Debug(LogCategory.Schedule, $"  - already eating a meal");
                return false;
            }

            if (mealType == MealType.None)
            {
                Log.Debug(LogCategory.Schedule, $"  - meal type is None");
                return false;
            }

            if (mealType == MealType.Other)
            {
                Log.Debug(LogCategory.Schedule, $"  - meal type is Other (snack)");
                return ShouldScheduleSnack(ref schedule, citizenAge);
            }

            if (schedule.HasMealScheduledOrConsumedToday(mealType))
            {
                Log.Debug(LogCategory.Schedule, $"  - already consumed {mealType} today");
                return false;
            }

            uint eatingOutChance = spareTimeBehavior.GetEatingOutChance(citizenAge);
            uint adjustedMealChance = schedule.GetAdjustedMealChance(eatingOutChance);
            Log.Debug(LogCategory.Schedule, $"  - citizen age is {citizenAge}, go out to eat a meal chance is {eatingOutChance} and adjustedMealChance is {adjustedMealChance}");

            if (!randomizer.ShouldOccur(adjustedMealChance))
            {
                return false;
            }

            if (timeInfo.IsNightTime || randomizer.ShouldOccur(config.LocalBuildingSearchQuota))
            {
                schedule.Hint = ScheduleHint.LocalMealOnly;
            }

            return true;
        }

        /// <summary>Check if the citizen should go to eat a meal while at work or school.</summary>
        /// <param name="schedule">The citizen's schedule.</param>
        /// <param name="citizenAge">The citizen's age group.</param>
        /// <param name="mealType">The citizen's meal type.</param>
        /// <returns><c>true</c> if the citizen should go to eat a meal while at work or school; otherwise, <c>false</c>.</returns>
        public bool ShouldScheduleWorkOrSchoolMeal(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge, MealType mealType)
        {
            schedule.ResetDailyMealsIfNeeded(timeInfo.Now.DayOfYear);

            if (schedule.CurrentState == ResidentState.EatMeal)
            {
                Log.Debug(LogCategory.Schedule, $"  - already eating a meal");
                return false;
            }

            if (mealType != MealType.Breakfast && mealType != MealType.Lunch && mealType != MealType.Supper)
            {
                Log.Debug(LogCategory.Schedule, $"  - {mealType} is not a work/school full meal");
                return false;
            }

            if (schedule.HasMealScheduledOrConsumedToday(mealType))
            {
                Log.Debug(LogCategory.Schedule, $"  - {mealType} is already scheduled or consumed today");
                return false;
            }

            if ((citizenAge == Citizen.AgeGroup.Child || citizenAge == Citizen.AgeGroup.Teen) && schedule.SchoolStatus == SchoolStatus.Studying)
            {
                Log.Debug(LogCategory.Schedule, $"  - child or teen cannot order a full meal while at school");
                return false;
            }

            bool enabled;
            uint quota;

            switch (mealType)
            {
                case MealType.Breakfast:
                    enabled = config.IsBreakfastTimeEnabledDuringWorkOrSchool;
                    quota = config.BreakfastDuringWorkOrSchoolQuota;
                    break;

                case MealType.Lunch:
                    enabled = config.IsLunchTimeEnabledDuringWorkOrSchool;
                    quota = config.LunchDuringWorkOrSchoolQuota;
                    break;

                case MealType.Supper:
                    enabled = config.IsSupperTimeEnabledDuringWorkOrSchool;
                    quota = config.SupperDuringWorkOrSchoolQuota;
                    break;

                default:
                    return false;
            }

            Log.Debug(LogCategory.Schedule, $" - citizen age is {citizenAge}, work/school {mealType} quota is {quota}, enabled is {enabled}");
            return enabled && randomizer.ShouldOccur(quota);
        }

        /// <summary>Update the citizen's meal type according to the time of day.</summary>
        /// <param name="citizenId">The citizen id.</param>
        /// <param name="schedule">The citizen's schedule.</param>
        public void UpdateMealTypeByTimeOfDay(uint citizenId, ref CitizenSchedule schedule)
        {
            if (timeInfo.CurrentHour >= config.BreakfastBegin && timeInfo.CurrentHour <= config.BreakfastEnd)
            {
                Log.Debug(LogCategory.Schedule, timeInfo.Now, $"Citizen {citizenId} - updating work none meal type to {MealType.Breakfast}");
                schedule.UpdateMealType(MealType.Breakfast);
            }
            else if (timeInfo.CurrentHour >= config.LunchBegin && timeInfo.CurrentHour <= config.LunchEnd)
            {
                Log.Debug(LogCategory.Schedule, timeInfo.Now, $"Citizen {citizenId} - updating work none meal type to {MealType.Lunch}");
                schedule.UpdateMealType(MealType.Lunch);
            }
            else if (timeInfo.CurrentHour >= config.SupperBegin && timeInfo.CurrentHour <= config.SupperEnd)
            {
                Log.Debug(LogCategory.Schedule, timeInfo.Now, $"Citizen {citizenId} - updating work none meal type to {MealType.Supper}");
                schedule.UpdateMealType(MealType.Supper);
            }
            else
            {
                Log.Debug(LogCategory.Schedule, timeInfo.Now, $"Citizen {citizenId} - updating work none meal type to {MealType.Other}");
                schedule.UpdateMealType(MealType.Other);
            }
        }

        /// <summary>Return the meal type, begin time and duration by the given hour.</summary>
        /// <param name="hour">The hour to find the meal.</param>
        /// <param name="mealType">The selected meal type.</param>
        /// <param name="mealBegin">The selected meal start time.</param>
        /// <param name="mealDuration">The selected meal duration.</param>
        public void GetMealDataByTimeOfDay(float hour, out MealType mealType, out float mealBegin, out float mealDuration)
        {
            if (hour >= config.BreakfastBegin && hour <= config.BreakfastEnd)
            {
                mealType = MealType.Breakfast;
                mealBegin = config.BreakfastBegin;
                mealDuration = config.BreakfastDuration;
            }
            else if (hour >= config.LunchBegin && hour <= config.LunchEnd)
            {
                mealType = MealType.Lunch;
                mealBegin = config.LunchBegin;
                mealDuration = config.LunchDuration;
            }
            else if (hour >= config.SupperBegin && hour <= config.SupperEnd)
            {
                mealType = MealType.Supper;
                mealBegin = config.SupperBegin;
                mealDuration = config.SupperDuration;
            }
            else
            {
                mealType = MealType.Other;
                mealBegin = 0;
                mealDuration = 0.5f;
            }
            Log.Debug(LogCategory.Schedule, timeInfo.Now, $" - citizen selected a meal according to hour {hour} - meal type is {mealType}, meal begin at {mealBegin} and duration is {mealDuration}");
        }

        /// <summary>Try to get the best work or school meal opportunity.</summary>
        /// <param name="schedule">The citizen's schedule.</param>
        /// <param name="now">The current time.</param>
        /// <param name="opportunity">The scheduled meal opportunity.</param>
        /// <returns>True if a meal opportunity was found; otherwise, false.</returns>
        public bool TryGetBestWorkOrSchoolMealOpportunity(ref CitizenSchedule schedule, DateTime now, out ScheduledMealOpportunity opportunity)
        {
            opportunity = default;

            const float minimumOverlapHours = 0.5f;
            float blockStartHour;
            float blockEndHour;

            if (schedule.WorkStatus == WorkStatus.Working)
            {
                Log.Debug(LogCategory.Schedule, $"  - find best work meal opportunity, block start: {schedule.WorkShiftStartTime}, block end: {schedule.WorkShiftEndTime}");
                blockStartHour = schedule.WorkShiftStartTime;
                blockEndHour = schedule.WorkShiftEndTime;
            }
            else if (schedule.SchoolStatus == SchoolStatus.Studying)
            {
                Log.Debug(LogCategory.Schedule, $"  - find best school meal opportunity, block start: {schedule.SchoolClassStartTime}, block end: {schedule.SchoolClassEndTime}");
                blockStartHour = schedule.SchoolClassStartTime;
                blockEndHour = schedule.SchoolClassEndTime;
            }
            else
            {
                return false;
            }

            float normalizedBlockEnd = NormalizeHourRangeEnd(blockStartHour, blockEndHour);
            ScheduledMealOpportunity? best = null;
            Log.Debug(LogCategory.Schedule, $"  - normalized block end: {normalizedBlockEnd}");

            foreach (var window in GetConfiguredMealWindows())
            {
                if (!window.Enabled)
                {
                    continue;
                }

                if (schedule.HasMealScheduledOrConsumedToday(window.MealType))
                {
                    continue;
                }

                if (!HasWorkOrSchoolMealDelayPassed(ref schedule))
                {
                    continue;
                }

                float mealBegin = window.Begin;
                float mealEnd = window.End;

                if (mealEnd < mealBegin)
                {
                    mealEnd += 24f;
                }

                float overlap = GetOverlapHours(blockStartHour, normalizedBlockEnd, mealBegin, mealEnd);
                if (overlap < minimumOverlapHours)
                {
                    continue;
                }

                if (!randomizer.ShouldOccur(window.Quota))
                {
                    continue;
                }

                Log.Debug(LogCategory.Schedule, $"  - found meal opportunity: {window.MealType}, begin: {window.Begin}, duration: {window.Duration}, overlap: {overlap}");

                var beginTime = ToFutureDateTime(now, window.Begin);
                var endTime = beginTime.AddHours(window.Duration);

                Log.Debug(LogCategory.Schedule, $"  - begin time: {beginTime}, end time: {endTime}");

                float midpoint = window.Begin + window.Duration / 2f;
                float blockMidpoint = blockStartHour + (normalizedBlockEnd - blockStartHour) / 2f;
                float midpointDistance = Math.Abs(blockMidpoint - midpoint);

                float score = overlap * 10f - midpointDistance;

                Log.Debug(LogCategory.Schedule, $"  - meal midpoint: {midpoint}, block midpoint: {blockMidpoint}, midpoint distance: {midpointDistance}, score: {score}");

                var candidate = new ScheduledMealOpportunity(window.MealType, beginTime, endTime, overlap, score);

                if (best == null || candidate.Score > best.Value.Score)
                {
                    best = candidate;
                }

                Log.Debug(LogCategory.Schedule, $"  - best meal opportunity so far: {best.Value.MealType}, score: {best.Value.Score}");
            }

            if (best == null)
            {
                Log.Debug(LogCategory.Schedule, $"  - no suitable meal opportunity found");
                return false;
            }

            opportunity = best.Value;
            Log.Debug(LogCategory.Schedule, $"  - selected meal opportunity: {opportunity.MealType}, score: {opportunity.Score}");
            return true;
        }

        /// <summary>Get the meal duration for the given meal type.</summary>
        /// <param name="mealType">The type of the meal.</param>
        /// <returns>The duration of the meal in hours.</returns>
        public float GetMealDuration(MealType mealType) => mealType switch
        {
            MealType.Breakfast => config.BreakfastDuration,
            MealType.Lunch => config.LunchDuration,
            MealType.Supper => config.SupperDuration,
            MealType.Other => 0.5f,
            _ => 0f,
        };

        private bool HasWorkOrSchoolMealDelayPassed(ref CitizenSchedule schedule)
        {
            float startHour;

            if (schedule.WorkStatus == WorkStatus.Working)
            {
                startHour = schedule.WorkShiftStartTime;
            }
            else if (schedule.SchoolStatus == SchoolStatus.Studying)
            {
                startHour = schedule.SchoolClassStartTime;
            }
            else
            {
                return false;
            }

            Log.Debug(LogCategory.Schedule, $"  - checking work/school meal delay, current hour: {timeInfo.CurrentHour}, start hour: {startHour}");

            // Check if work/school hasn't started yet
            if (timeInfo.CurrentHour < startHour && (startHour - timeInfo.CurrentHour) < 12f)
            {
                Log.Debug(LogCategory.Schedule, $"  - work/school hasn't started yet, current hour: {timeInfo.CurrentHour}, start hour: {startHour}");
                return false; // Work/school hasn't started yet
            }

            float elapsed = GetHoursSinceDailyStart(timeInfo.CurrentHour, startHour);
            Log.Debug(LogCategory.Schedule, $"  - elapsed time since work/school start: {elapsed}");

            return elapsed >= Constants.MinimumWorkOrSchoolTimeBeforeMeal;
        }

        private static float GetHoursSinceDailyStart(float currentHour, float startHour)
        {
            float result = currentHour - startHour;

            Log.Debug(LogCategory.Schedule, $"  - hours since daily start: {result}");

            if (result < 0f)
            {
                result += 24f;
            }

            Log.Debug(LogCategory.Schedule, $"  - adjusted hours since daily start: {result}");

            return result;
        }

        private IEnumerable<MealWindow> GetConfiguredMealWindows()
        {
            yield return new MealWindow(
                MealType.Breakfast,
                config.BreakfastBegin,
                config.BreakfastDuration,
                config.BreakfastEnd,
                config.BreakfastDuringWorkOrSchoolQuota,
                config.IsBreakfastTimeEnabledDuringWorkOrSchool);

            yield return new MealWindow(
                MealType.Lunch,
                config.LunchBegin,
                config.LunchDuration,
                config.LunchEnd,
                config.LunchDuringWorkOrSchoolQuota,
                config.IsLunchTimeEnabledDuringWorkOrSchool);

            yield return new MealWindow(
                MealType.Supper,
                config.SupperBegin,
                config.SupperDuration,
                config.SupperEnd,
                config.SupperDuringWorkOrSchoolQuota,
                config.IsSupperTimeEnabledDuringWorkOrSchool);
        }

        private static float GetOverlapHours(float range1Start, float range1End, float range2Start, float range2End) =>
            Math.Max(0f, Math.Min(range1End, range2End) - Math.Max(range1Start, range2Start));

        private static float NormalizeHourRangeEnd(float start, float end) => end < start ? end + 24f : end;

        private static DateTime ToFutureDateTime(DateTime now, float hour)
        {
            var dt = now.Date.AddHours(hour);
            return dt < now ? dt.AddDays(1) : dt;
        }

        private bool ShouldScheduleSnack(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge)
        {
            if (IsSchoolAgeCitizen(citizenAge) && schedule.SchoolStatus == SchoolStatus.Studying)
            {
                Log.Debug(LogCategory.Schedule, "  - child or teen cannot go out for a snack while at school");
                return false;
            }

            if (!schedule.CanHaveSnack(timeInfo.Now))
            {
                Log.Debug(LogCategory.Schedule, "  - citizen cannot go out for a snack at this time");
                return false;
            }

            uint snackChance = GetSnackChance(citizenAge);

            Log.Debug(LogCategory.Schedule, $"  - snack chance for {citizenAge} is {snackChance}");

            if (!randomizer.ShouldOccur(snackChance))
            {
                Log.Debug(LogCategory.Schedule, $"  - citizen did not go out for a snack (chance {snackChance})");
                return false;
            }

            schedule.Hint = ScheduleHint.LocalMealOnly;
            Log.Debug(LogCategory.Schedule, "  - citizen went out for a snack");
            return true;
        }

        private uint GetSnackChance(Citizen.AgeGroup citizenAge) => citizenAge switch
        {
            Citizen.AgeGroup.Child => 8u,
            Citizen.AgeGroup.Teen => 12u,
            Citizen.AgeGroup.Young => 10u,
            Citizen.AgeGroup.Adult => 8u,
            Citizen.AgeGroup.Senior => 4u,
            _ => 0u,
        };

        private static bool IsSchoolAgeCitizen(Citizen.AgeGroup citizenAge) => citizenAge == Citizen.AgeGroup.Child || citizenAge == Citizen.AgeGroup.Teen;
    }
}
