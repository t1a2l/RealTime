// VanillaEvent.cs

namespace RealTime.Events
{
    using System;
    using System.Collections.Generic;
    using RealTime.GameConnection;
    using RealTime.Simulation;
    using SkyTools.Tools;

    /// <summary>A class for the default game city event.</summary>
    /// <seealso cref="CityEventBase"/>
    /// <remarks>Initializes a new instance of the <see cref="VanillaEvent"/> class.</remarks>
    /// <param name="id">The event ID.</param>
    /// <param name="duration">The city event duration in hours.</param>
    /// <param name="ticketPrice">The event ticket price.</param>
    /// <param name="eventManager">An <see cref="IEventManagerConnection"/> reference.</param>
    internal sealed class VanillaEvent(ushort id, float duration, float ticketPrice, IEventManagerConnection eventManager) : CityEventBase
    {
        private readonly float duration = duration;
        private readonly float ticketPrice = ticketPrice;
        private readonly IEventManagerConnection eventManager = eventManager ?? throw new System.ArgumentNullException(nameof(eventManager));

        /// <summary>Gets the vanilla event ID.</summary>
        public ushort EventId { get; } = id;

        /// <summary>
        /// Gets the event color.
        /// </summary>
        public override EventColor Color => eventManager.GetEventColor(EventId);

        /// <summary>
        /// Cache for the list of building IDs attending this city event, including the building ID this event takes place in.
        /// </summary>
        private HashSet<ushort> _attendanceBuildings;

        /// <summary>Accepts an event attendee with specified properties.</summary>
        /// <param name="age">The attendee age.</param>
        /// <param name="gender">The attendee gender.</param>
        /// <param name="education">The attendee education.</param>
        /// <param name="wealth">The attendee wealth.</param>
        /// <param name="wellbeing">The attendee wellbeing.</param>
        /// <param name="happiness">The attendee happiness.</param>
        /// <param name="randomizer">A reference to the game's randomizer.</param>
        /// <param name="buildingClass">the class of the building the event is taking place in.</param>
        /// <param name="targetBuilding">The building ID where the citizen can attend the event.</param>
        /// <returns>
        /// <c>true</c> if the event attendee with specified properties is accepted and can attend
        /// this city event; otherwise, <c>false</c>.
        /// </returns>
        public override bool TryAcceptAttendee(Citizen.AgeGroup age, Citizen.Gender gender, Citizen.Education education, Citizen.Wealth wealth,
            Citizen.Wellbeing wellbeing, Citizen.Happiness happiness, IRandomizer randomizer, ItemClass buildingClass, out ushort targetBuilding)
        {
            targetBuilding = 0;

            // budget check
            if (ticketPrice > GetCitizenBudgetForEvent(wealth, randomizer))
            {
                Log.Debug(LogCategory.Events, $"Citizen with wealth {wealth} cannot afford event ticket price {ticketPrice} in event {BuildingId}.");
                return false;
            }

            string aiType = eventManager.GetEventAIType(EventId);
            Log.Debug(LogCategory.Events, $"Event {BuildingId} has AI type {aiType}.");

            // age/event-type check
            if (age == Citizen.AgeGroup.Child)
            {
                if (aiType == "ConcertAI")
                {
                    Log.Debug(LogCategory.Events, $"Citizen with age {age} cannot attend event {BuildingId} with AI type {aiType}.");
                    return false;
                }
            }

            // NEW: check capacity across all buildings (main + stands)
            foreach (ushort buildingId in GetAttendanceBuildings())
            {
                if (eventManager.HasFreeEventCapacity(buildingId))
                {
                    Log.Debug(LogCategory.Events, $"Citizen can attend event {BuildingId} in building {buildingId}.");
                    targetBuilding = buildingId;
                    return true; // at least one building has space
                }
            }

            Log.Debug(LogCategory.Events, $"Citizen cannot attend event {BuildingId} because all buildings are at full capacity.");
            return false;
            
        }

        /// <summary>
        /// Configures this event to take place in the specified building and at the specified start time.
        /// </summary>
        /// <param name="buildingId">The building ID this city event should take place in.</param>
        /// <param name="buildingName">
        /// The localized name of the building this city event should take place in.
        /// </param>
        /// <param name="startTime">The city event start time.</param>
        public override void Configure(ushort buildingId, string buildingName, DateTime startTime)
        {
            base.Configure(buildingId, buildingName, startTime);
            _attendanceBuildings = null; // invalidate cache
        }

        /// <summary>Calculates the city event duration.</summary>
        /// <returns>This city event duration in hours.</returns>
        protected override float GetDuration() => duration;

        /// <summary> Gets the list of building IDs that are attending this city event, including the building ID this event takes place in.</summary>
        /// <returns>A set of building IDs attending this city event.</returns>
        public override HashSet<ushort> GetAttendanceBuildings()
        {
            if (_attendanceBuildings != null)
            {
                return _attendanceBuildings;
            }

            ushort routeId = BuildingManager.instance.m_buildings.m_buffer[BuildingId].m_eventRouteIndex;
            var route = EventManager.instance.m_eventRoutes.m_buffer[routeId];

            _attendanceBuildings = route.m_stands != null && route.m_stands.Count > 0 ? [.. route.m_stands, BuildingId] : [BuildingId];
            return _attendanceBuildings;
        }
    }
}
