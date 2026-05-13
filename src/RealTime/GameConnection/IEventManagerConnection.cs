// IEventManagerConnection.cs

namespace RealTime.GameConnection
{
    using System;
    using RealTime.Events;

    /// <summary>An interface for the game specific logic related to the event management.</summary>
    internal interface IEventManagerConnection
    {
        /// <summary>Gets the flags of an event with specified ID.</summary>
        /// <param name="eventId">The ID of the event to get flags of.</param>
        /// <returns>The event flags or <see cref="EventData.Flags.None"/> if none found.</returns>
        internal EventData.Flags GetEventFlags(ushort eventId);

        /// <summary>
        /// Gets a collection of the IDs of upcoming city events in the specified time interval.
        /// </summary>
        /// <param name="earliestTime">The start time of the interval to get events from.</param>
        /// <param name="latestTime">The end time of the interval to get events from.</param>
        /// <returns>A collection of the city event IDs.</returns>
        internal IReadOnlyList<ushort> GetUpcomingEvents(DateTime earliestTime, DateTime latestTime);

        /// <summary>Gets various information about a city event with specified ID.</summary>
        /// <param name="eventId">The ID of the city event to get information for.</param>
        /// <param name="eventInfo">A <see cref="VanillaEventInfo"/> ref-struct containing the event information.</param>
        /// <returns><c>true</c> if the information was retrieved; otherwise, <c>false</c>.</returns>
        internal bool TryGetEventInfo(ushort eventId, out VanillaEventInfo eventInfo);

        /// <summary>Gets the start time of a city event with specified ID.</summary>
        /// <param name="eventId">The ID of the city event to get start time of.</param>
        /// <param name="startTime">The start time of the event with the specified ID.</param>
        /// <returns><c>true</c> if the start time was retrieved; otherwise, <c>false</c>.</returns>
        internal bool TryGetEventStartTime(ushort eventId, out DateTime startTime);

        /// <summary>Gets the color of a city event with specified ID.</summary>
        /// <param name="eventId">The ID of the city event to get the color of.</param>
        /// <returns>The color of the event.</returns>
        internal EventColor GetEventColor(ushort eventId);

        /// <summary>Sets the start time of the event to the specified value.</summary>
        /// <param name="eventId">The ID of the event to change.</param>
        /// <param name="startTime">The new event start time.</param>
        internal void SetStartTime(ushort eventId, DateTime startTime);

        /// <summary>Gets the AI type of a city event with specified ID.</summary>
        /// <param name="eventId">The ID of the event to get the AI type of.</param>
        /// <returns>The AI type of the event.</returns>
        internal string GetEventAIType(ushort eventId);

        /// <summary>Checks if a city event with specified ID has free capacity.</summary>
        /// <param name="eventId">The ID of the event to check.</param>
        /// <returns><c>true</c> if the event has free capacity; otherwise, <c>false</c>.</returns>
        /// <returns>The AI type of the event.</returns>
        internal bool HasFreeEventCapacity(ushort buildingId);
    }
}
