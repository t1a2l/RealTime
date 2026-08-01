// DailyMealFlags.cs

namespace RealTime.CustomAI
{
    using System;

    /// <summary>
    /// Daily meal flags for tracking which meals a citizen has consumed.
    /// </summary>
    [Flags]
    internal enum DailyMealFlags : byte
    {
        /// <summary>No meals have been consumed.</summary>
        None = 0,

        /// <summary>The citizen has consumed breakfast.</summary>
        Breakfast = 1 << 0,

        /// <summary>The citizen has consumed lunch.</summary>
        Lunch = 1 << 1,

        /// <summary>The citizen has consumed supper.</summary>
        Supper = 1 << 2,
    }
}
