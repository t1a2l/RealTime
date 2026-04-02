// ISchoolBehavior.cs

namespace RealTime.CustomAI
{
    using System;

    /// <summary>
    /// An interface for the citizens work behavior.
    /// </summary>
    internal interface ISchoolBehavior
    {
        /// <summary>Notifies this object that a new game day starts.</summary>
        internal void BeginNewDay();

        /// <summary>Check if the citizen should go to school</summary>
        /// <param name="schedule">The citizen's schedule.</param>
        /// <returns><c>true</c> if the citizen should go to school; otherwise, <c>false</c>.</returns>
        internal bool ShouldScheduleGoToSchool(ref CitizenSchedule schedule);

        /// <summary>Updates the citizen's school schedule by determining the time for going to school.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="currentBuilding">The ID of the building where the citizen is currently located.</param>
        /// <param name="simulationCycle">The duration (in hours) of a full citizens simulation cycle.</param>
        /// <returns>The time when going to school</returns>
        internal DateTime ScheduleGoToSchoolTime(ref CitizenSchedule schedule, ushort currentBuilding, float simulationCycle);

        /// <summary>Updates the citizen's school schedule by determining the time for returning from school.</summary>
        /// <param name="citizenId">The citizen's ID.</param>
        /// <param name="schedule">The citizen's schedule to update.</param>
        internal void ScheduleReturnFromSchool(uint citizenId, ref CitizenSchedule schedule);

        /// <summary>Updates the citizen's school class parameters in the specified citizen's <paramref name="schedule"/>.</summary>
        /// <param name="schedule">The citizen's schedule to update the school class time in.</param>
        /// <param name="citizenAge">The age of the citizen.</param>
        internal void UpdateSchoolClass(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge);

        /// <summary>Updates the citizen's school schedule by checking if he will or will not eat a meal.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        /// <param name="schoolBuilding">The citizen's school building.</param>
        /// <param name="mealType">The meal type the citizen is going to eat.</param>
        /// <returns><c>true</c> if a breakfast was scheduled; otherwise, <c>false</c>.</returns>
        internal bool ScheduleMeal(ref CitizenSchedule schedule, ushort schoolBuilding, MealType mealType);

        /// <summary>Updates the citizen's school schedule by determining the returning from meal time.</summary>
        /// <param name="schedule">The citizen's schedule to update.</param>
        internal void ScheduleReturnFromMeal(ref CitizenSchedule schedule);
    }
}
