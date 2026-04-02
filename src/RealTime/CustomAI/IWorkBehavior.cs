// IWorkBehavior.cs

namespace RealTime.CustomAI
{
    using System;

    /// <summary>
    /// An interface for the citizens work behavior.
    /// </summary>
    internal interface IWorkBehavior
    {
        /// <summary>Notifies this object that a new game day starts.</summary>
        internal void BeginNewDay();

        /// <summary>Check if the citizen should go to work</summary>
        /// <param name="schedule">The citizen's schedule.</param>
        /// <returns><c>true</c> if the citizen should go to work; otherwise, <c>false</c>.</returns>
        internal bool ShouldScheduleGoToWork(ref CitizenSchedule schedule);

        /// <summary>Updates the citizen's work schedule by determining the time for going to work.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="currentBuilding">The ID of the building where the citizen is currently located.</param>
        /// <param name="simulationCycle">The duration (in hours) of a full citizens simulation cycle.</param>
        /// <returns>departuretime</returns>
        internal DateTime ScheduleGoToWorkTime(ref CitizenSchedule schedule, ushort currentBuilding, float simulationCycle);

        /// <summary>Updates the citizen's work schedule by checking if he will or will not eat a meal.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="citizenAge">The citizen's age.</param>
        /// <param name="mealType">The meal type the citizen is going to eat.</param>
        /// <returns><c>true</c> if a meal was scheduled; otherwise, <c>false</c>.</returns>
        internal bool ScheduleMeal(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge, MealType mealType);

        /// <summary>Updates the citizen's work schedule by determining the returning from meal time.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        internal void ScheduleReturnFromMeal(ref CitizenSchedule schedule);

        /// <summary>Updates the citizen's work schedule by determining the time for returning from work.</summary>
        /// <param name="citizenId">The citizen's ID.</param>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="citizenAge">The age of the citizen.</param>
        internal void ScheduleReturnFromWork(uint citizenId, ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge);

        /// <summary>Updates the citizen's work shift parameters in the specified citizen's <paramref name="schedule"/>.</summary>
        /// <param name="schedule">The citizen's schedule to update the work shift in.</param>
        /// <param name="citizenAge">The age of the citizen.</param>
        /// <param name="chosenWorkShift">The chosen work shift to update</param>
        internal void UpdateWorkShift(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge, WorkShift chosenWorkShift);

    }
}
