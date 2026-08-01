// IMealBehavior.cs

namespace RealTime.CustomAI
{
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
    }
}
