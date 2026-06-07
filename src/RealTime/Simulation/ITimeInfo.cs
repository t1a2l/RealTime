// ITimeInfo.cs

namespace RealTime.Simulation
{
    using System;

    /// <summary>
    /// An interface for the current game time and date information.
    /// </summary>
    internal interface ITimeInfo
    {
        /// <summary>
        /// Gets the current game date and time.
        /// </summary>
        internal DateTime Now { get; }

        /// <summary>
        /// Gets the current daytime hour.
        /// </summary>
        internal float CurrentHour { get; }

        /// <summary>
        /// Gets the sunrise hour of the current day.
        /// </summary>
        internal float SunriseHour { get; }

        /// <summary>
        /// Gets the sunset hour of the current day.
        /// </summary>
        internal float SunsetHour { get; }

        /// <summary>
        /// Gets a value indicating whether the current time represents a night hour.
        /// </summary>
        internal bool IsNightTime { get; }

        /// <summary>
        /// Gets the duration of the current or last day.
        /// </summary>
        internal float DayDuration { get; }

        /// <summary>
        /// Gets the duration of the current or last night.
        /// </summary>
        internal float NightDuration { get; }

        /// <summary>Gets the number of hours that fit into one simulation frame.</summary>
        internal float HoursPerFrame { get; }
    }
}
