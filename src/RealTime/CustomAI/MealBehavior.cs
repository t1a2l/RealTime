// MealBehavior.cs

namespace RealTime.CustomAI
{
    using System;
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
            if (schedule.CurrentState == ResidentState.EatMeal)
            {
                Log.Debug(LogCategory.Schedule, $"  - already eating a meal");
                return false;
            }

            if ((citizenAge == Citizen.AgeGroup.Child || citizenAge == Citizen.AgeGroup.Teen) && schedule.SchoolStatus == SchoolStatus.Studying)
            {
                Log.Debug(LogCategory.Schedule, $"  - kids dont eat meals while at school");
                return false;
            }

            uint eatingOutChance = spareTimeBehavior.GetEatingOutChance(citizenAge);
            Log.Debug(LogCategory.Schedule, $"  - citizen age is {citizenAge}, go out to eat a meal chance is {eatingOutChance}");
            if (!randomizer.ShouldOccur(eatingOutChance))
            {
                return false;
            }

            if (timeInfo.IsNightTime || randomizer.ShouldOccur(config.LocalBuildingSearchQuota))
            {
                schedule.Hint = ScheduleHint.LocalMealOnly;
            }

            return true;
        }

        /// <summary>Check if the citizen should go to eat a work or schoool related meal .</summary>
        /// <param name="schedule">The citizen's schedule.</param>
        /// <param name="citizenAge">The citizen's age group.</param>
        /// <param name="mealType">The citizen's meal type.</param>
        /// <returns><c>true</c> if the citizen should go to eat a meal; otherwise, <c>false</c>.</returns>
        public bool ShouldScheduleWorkOrSchoolMeal(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge, MealType mealType)
        {
            if (schedule.CurrentState == ResidentState.EatMeal)
            {
                Log.Debug(LogCategory.Schedule, $"  - already eating a meal");
                return false;
            }

            if ((citizenAge == Citizen.AgeGroup.Child || citizenAge == Citizen.AgeGroup.Teen) && schedule.SchoolStatus == SchoolStatus.Studying)
            {
                Log.Debug(LogCategory.Schedule, $"  - kids dont eat meals while at school");
                return false;
            }

            if (mealType == MealType.Breakfast)
            {
                Log.Debug(LogCategory.Schedule, $"  - citizen age is {citizenAge}, work or school BreakfastQuota is {config.BreakfastBeforeWorkOrSchoolQuota}");
                return config.IsBreakfastTimeEnabledBeforeWorkOrSchool && randomizer.ShouldOccur(config.BreakfastBeforeWorkOrSchoolQuota);
            }
            else if (mealType == MealType.Lunch)
            {
                Log.Debug(LogCategory.Schedule, $"  - citizen age is {citizenAge}, work or school LunchQuota is {config.LunchDuringWorkOrSchoolQuota}");
                return config.IsLunchTimeEnabledDuringWorkOrSchool && randomizer.ShouldOccur(config.LunchDuringWorkOrSchoolQuota);
            }
            else if (mealType == MealType.Supper)
            {
                Log.Debug(LogCategory.Schedule, $"  - citizen age is {citizenAge}, work or school SupperQuota is {config.SupperAfterWorkOrSchoolQuota}");
                return config.IsSupperTimeEnabledAfterWorkOrSchool && randomizer.ShouldOccur(config.SupperAfterWorkOrSchoolQuota);
            }

            return false;
        }

        /// <summary>Update the citizen's meal type according to the time of day.</summary>
        /// <param name="citizenId">The citizen id.</param>
        /// <param name="schedule">The citizen's schedule.</param>
        public void UpdateMealTypeByTimeOfDay(uint citizenId, ref CitizenSchedule schedule)
        {
            if (timeInfo.CurrentHour >= config.BreakfastBegin && timeInfo.CurrentHour <= 10f)
            {
                Log.Debug(LogCategory.Schedule, timeInfo.Now, $"Citizen {citizenId} - updating work none meal type to {MealType.Breakfast}");
                schedule.UpdateMealType(MealType.Breakfast);
            }
            else if (timeInfo.CurrentHour >= config.LunchBegin && timeInfo.CurrentHour <= 13f)
            {
                Log.Debug(LogCategory.Schedule, timeInfo.Now, $"Citizen {citizenId} - updating work none meal type to {MealType.Lunch}");
                schedule.UpdateMealType(MealType.Lunch);
            }
            else if (timeInfo.CurrentHour >= config.SupperBegin && timeInfo.CurrentHour <= 20f)
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
            if (hour >= config.BreakfastBegin && hour <= 10f)
            {
                mealType = MealType.Breakfast;
                mealBegin = config.BreakfastBegin;
                mealDuration = config.BreakfastDuration;
            }
            else if (hour >= config.LunchBegin && hour <= 13f)
            {
                mealType = MealType.Lunch;
                mealBegin = config.LunchBegin;
                mealDuration = config.LunchDuration;
            }
            else if (hour >= config.SupperBegin && hour <= 20f)
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
    }
}
