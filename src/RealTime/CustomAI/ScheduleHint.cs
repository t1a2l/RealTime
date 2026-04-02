// ScheduleHint.cs

namespace RealTime.CustomAI
{
    /// <summary>Describes various citizen schedule hints.</summary>
    internal enum ScheduleHint : byte
    {
        /// <summary>No hint.</summary>
        None,

        /// <summary>The citizen can shop only locally.</summary>
        LocalShoppingOnly,

        /// <summary>The citizen can shop only locally before work.</summary>
        LocalShoppingOnlyBeforeWork,

        /// <summary>The citizen can shop only locally before university.</summary>
        LocalShoppingOnlyBeforeUniversity,

        /// <summary>The citizen will not go shopping one more time right away.</summary>
        NoShoppingAnyMore,

        /// <summary>The citizen should find a leisure building.</summary>
        RelaxAtLeisureBuilding,

        /// <summary>The citizen is on a guided tour.</summary>
        OnTour,

        /// <summary>The citizen is attending an event.</summary>
        AttendingEvent,

        /// <summary>The citizen can search for entertainment nearby only.</summary>
        RelaxNearbyOnly,

        /// <summary>The citizen can go to eat only locally.</summary>
        LocalMealOnly,

        /// <summary>The citizen will not go  to eat one more time right away.</summary>
        NoMealAnyMore
    }
}
