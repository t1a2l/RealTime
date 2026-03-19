// TimeInfo.cs

namespace RealTime.GameConnection
{
    using System;
    using RealTime.Config;
    using RealTime.Simulation;

    /// <summary>
    /// The default implementation of the <see cref="ITimeInfo"/> interface.
    /// </summary>
    /// <seealso cref="ITimeInfo" />
    /// <remarks>Initializes a new instance of the <see cref="TimeInfo" /> class.</remarks>
    /// <param name="config">The configuration to run with.</param>
    /// <exception cref="ArgumentNullException">Thrown when the argument is null.</exception>
    internal sealed class TimeInfo(RealTimeConfig config) : ITimeInfo
    {
        private readonly RealTimeConfig config = config ?? throw new ArgumentNullException(nameof(config));
        private DateTime currentTime;

        /// <summary>Gets the current game date and time.</summary>
        public DateTime Now => SimulationManager.instance.m_currentGameTime;

        /// <summary>Gets the current daytime hour.</summary>
        public float CurrentHour
        {
            get
            {
                if (SimulationManager.instance.m_currentGameTime != currentTime)
                {
                    currentTime = SimulationManager.instance.m_currentGameTime;
                    field = (float)Now.TimeOfDay.TotalHours;
                }

                return field;
            }
        }

        /// <summary>Gets the sunrise hour of the current day.</summary>
        public float SunriseHour => SimulationManager.SUNRISE_HOUR;

        /// <summary>Gets the sunset hour of the current day.</summary>
        public float SunsetHour => SimulationManager.SUNSET_HOUR;

        /// <summary>Gets a value indicating whether the current time represents a night hour.</summary>
        public bool IsNightTime
        {
            get
            {
                float current = CurrentHour;
                return current >= config.GoToSleepHour || current < config.WakeUpHour;
            }
        }

        /// <summary>Gets the duration of the current or last day.</summary>
        public float DayDuration => SunsetHour - SunriseHour;

        /// <summary>Gets the duration of the current or last night.</summary>
        public float NightDuration => 24f - DayDuration;

        /// <summary>Gets the number of hours that fit into one simulation frame.</summary>
        public float HoursPerFrame => SimulationManager.DAYTIME_FRAME_TO_HOUR;
    }
}
