// SchoolClass.cs

namespace RealTime.CustomAI
{
    /// <summary>
    /// An enumeration that describes the citizen's school class.
    /// </summary>
    internal enum SchoolClass : byte
    {
        /// <summary>The citizen will not go to school.</summary>
        NoSchool,

        /// <summary>The citizen will study during the day.</summary>
        DayClass,

        /// <summary>The citizen will study during the night.</summary>
        NightClass,

    }
}
