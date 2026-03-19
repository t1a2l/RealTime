// CustomTimeBarClickEventArgs.cs

namespace RealTime.UI
{
    using System;

    /// <summary>Additional information for the time bar click events.</summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="CustomTimeBarClickEventArgs"/> class.
    /// </remarks>
    /// <param name="cityEventBuildingId">The ID of the building where a city event takes place.</param>
    internal sealed class CustomTimeBarClickEventArgs(ushort cityEventBuildingId) : EventArgs
    {

        /// <summary>Gets the ID of the building where a clicked city event takes place.</summary>
        public ushort CityEventBuildingId { get; } = cityEventBuildingId;
    }
}
