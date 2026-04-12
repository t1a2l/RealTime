// VanillaEventInfo.cs

namespace RealTime.GameConnection
{
    using System;

    /// <summary>
    /// A ref-struct consolidating the information about a vanilla game event.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="VanillaEventInfo"/> struct.
    /// </remarks>
    /// <param name="buildingId">The ID of the building where the event takes place.</param>
    /// <param name="startTime">The event start date and time.</param>
    /// <param name="duration">The event duration in hours.</param>
    /// <param name="ticketPrice">The ticket price for the event.</param>
    internal readonly ref struct VanillaEventInfo(ushort buildingId, DateTime startTime, float duration, float ticketPrice)
    {

        /// <summary>
        /// Gets the ID of the building where the event takes place.
        /// </summary>
        public ushort BuildingId { get; } = buildingId;

        /// <summary>
        /// Gets the event start date and time.
        /// </summary>
        public DateTime StartTime { get; } = startTime;

        /// <summary>
        /// Gets the event duration in hours.
        /// </summary>
        public float Duration { get; } = duration;

        /// <summary>
        /// Gets the ticket price for the event.
        /// </summary>
        public float TicketPrice { get; } = ticketPrice;
    }
}
