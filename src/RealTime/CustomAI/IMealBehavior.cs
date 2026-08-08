// IMealBehavior.cs

namespace RealTime.CustomAI
{
    using System;
    using static RealTime.CustomAI.MealBehavior;

    /// <summary>
    /// An interface for the citizens meal behavior.
    /// </summary>
    internal interface IMealBehavior
    {
        /// <summary>Notifies this object that a new game day starts.</summary>
        internal void BeginNewDay();

        /// <summary>Check if the citizen should go to eat a normal meal.</summary>
        /// <param name="schedule">The citizen's schedule.</param>
        /// <param name="citizenAge">The citizen's age group.</param>
        /// <param name="mealType">The citizen's meal type.</param>
        /// <returns><c>true</c> if the citizen should go to eat a normal meal; otherwise, <c>false</c>.</returns>
        internal bool ShouldScheduleMeal(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge, MealType mealType);

        /// <summary>Check if the citizen should go to eat a work or schoool related meal .</summary>
        /// <param name="schedule">The citizen's schedule.</param>
        /// <param name="citizenAge">The citizen's age group.</param>
        /// <param name="mealType">The citizen's meal type.</param>
        /// <returns><c>true</c> if the citizen should go to eat a meal; otherwise, <c>false</c>.</returns>
        internal bool ShouldScheduleWorkOrSchoolMeal(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge, MealType mealType);

        /// <summary>Update the citizen's meal type according to the time of day.</summary>
        /// <param name="citizenId">The citizen id.</param>
        /// <param name="schedule">The citizen's schedule.</param>
        internal void UpdateMealTypeByTimeOfDay(uint citizenId, ref CitizenSchedule schedule);

        /// <summary>Return the meal type, begin time and duration by the given hour.</summary>
        /// <param name="hour">The hour to find the meal.</param>
        /// <param name="mealType">The selected meal type.</param>
        /// <param name="mealBegin">The selected meal start time.</param>
        /// <param name="mealDuration">The selected meal duration.</param>
        internal void GetMealDataByTimeOfDay(float hour, out MealType mealType, out float mealBegin, out float mealDuration);

        /// <summary>Try to get the best work or school meal opportunity.</summary>
        /// <param name="schedule">The citizen's schedule.</param>
        /// <param name="now">The current time.</param>
        /// <param name="opportunity">The scheduled meal opportunity.</param>
        /// <returns>True if a meal opportunity was found; otherwise, false.</returns>
        internal bool TryGetBestWorkOrSchoolMealOpportunity(ref CitizenSchedule schedule, DateTime now, out ScheduledMealOpportunity opportunity);

        /// <summary>Get the meal duration for the given meal type.</summary>
        /// <param name="mealType">The type of the meal.</param>
        /// <returns>The duration of the meal in hours.</returns>
        internal float GetMealDuration(MealType mealType);
    }
}
