// ResidentState.cs

namespace RealTime.CustomAI
{
    /// <summary>
    /// Possible citizen's target states.
    /// </summary>
    internal enum ResidentState : byte
    {
        /// <summary>The state is not defined. A good time to make a decision.</summary>
        Unknown,

        /// <summary>The citizen should be ignored, just dummy traffic.</summary>
        Ignored,

        /// <summary>The citizen is going to the home building.</summary>
        GoHome,

        /// <summary>The citizen is in the home building.</summary>
        AtHome,

        /// <summary>The citizen is going to the school building.</summary>
        GoToSchool,

        /// <summary>The citizen is in the school building.</summary>
        AtSchool,

        /// <summary>The citizen is going to the work building.</summary>
        GoToWork,

        /// <summary>The citizen is in the work building.</summary>
        AtWork,

        /// <summary>The citizen is going to shop in a commercial building.</summary>
        GoShopping,

        /// <summary>The citizen is shopping in a commercial building.</summary>
        Shopping,

        /// ------------------------------------ remove in next version ----------------------------------------------------------

        /// <summary>The citizen is going to have breakfast in a commercial building or university cafeteria.</summary>
        GoToBreakfast,

        /// <summary>The citizen is going to have lunch in a commercial building or university cafeteria.</summary>
        GoToLunch,

        /// <summary>The citizen is having breakfast in a commercial building or university cafeteria.</summary>
        Breakfast,

        /// <summary>The citizen is having lunch in a commercial building or university cafeteria.</summary>
        Lunch,

        /// ----------------------------------------------------------------------------------------------------------------------

        /// <summary>The citizen is having a meal in a commercial building or university cafeteria.</summary>
        GoToMeal,

        /// <summary>The citizen is going to have a meal in a commercial building or university cafeteria.</summary>
        EatMeal,

        /// <summary>The citizen is going to a leisure building or a beautification building.</summary>
        GoToRelax,

        /// <summary>The citizen is in a leisure building or in a beautification building.</summary>
        Relaxing,

        /// <summary>The citizen is going to visit a building.</summary>
        GoToVisit,

        /// <summary>The citizen visits a building.</summary>
        Visiting,

        /// <summary>The citizen has to evacuate the current building (or area).</summary>
        Evacuating,

        /// <summary>The citizen is going to a shelter building.</summary>
        GoToShelter,

        /// <summary>The citizen is in a shelter building.</summary>
        InShelter,

        /// <summary>The citizen was in transition from one state to another.</summary>
        InTransition,
    }
}
