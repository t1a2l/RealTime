// SchoolClass.cs

namespace RealTime.CustomAI
{
    /// <summary>
    /// An enumeration that describes the citizen's school schedule.
    /// </summary>
    internal enum SchoolSchedule : byte
    {
        /// <summary>The citizen has no school schedule.</summary>
        NoSchedule,

        /// <summary>The citizen is currently in class.</summary>
        InClass,

        /// <summary>The citizen is currently on a break.</summary>
        OnBreak,

    }
}
