// WorkStatus.cs

namespace RealTime.CustomAI
{
    /// <summary>
    /// Describes the work status of a citizen.
    /// </summary>
    internal enum WorkStatus : byte
    {
        /// <summary>No special handling.</summary>
        None,

        /// <summary>The citizen has working hours.</summary>
        Working,

        /// <summary>The citizen is on vacation.</summary>
        OnVacation,
    }
}
