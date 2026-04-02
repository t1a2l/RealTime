// IRealTimeBuildingAI.cs

namespace RealTime.CustomAI
{
    using UnityEngine;

    /// <summary>
    /// An interface for the custom logic for the private buildings.
    /// </summary>
    internal interface IRealTimeBuildingAI
    {
        /// <summary>
        /// Determines whether the building with the specified <paramref name="buildingId"/> is noise restricted
        /// (has NIMBY policy that is active on current time).
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <param name="currentBuildingId">The ID of a building where the citizen starts their journey.
        /// Specify 0 if there is no journey in schedule.</param>
        /// <returns>
        ///   <c>true</c> if the building with the specified <paramref name="buildingId"/> has NIMBY policy
        ///   that is active on current time; otherwise, <c>false</c>.
        /// </returns>
        internal bool IsNoiseRestricted(ushort buildingId, ushort currentBuildingId = 0);

        /// <summary>Registers a trouble reaching the building with the specified ID.</summary>
        /// <param name="buildingId">The ID of the building where the citizen will not arrive as planned.</param>
        internal void RegisterReachingTrouble(ushort buildingId);

        /// <summary>
        /// Determines whether the building with the specified <paramref name="buildingId"/> is working or not
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <param name="timeBeforeWork">time before work the citizen can arrive without an issue.</param>
        /// <param name="currentBuildingId">The building ID the citizen is currently in.</param>
        /// <returns>
        ///   <c>true</c> if the building with the specified <paramref name="buildingId"/> is working otherwise, <c>false</c>.
        /// </returns>
        internal bool IsBuildingWorking(ushort buildingId, int timeBeforeWork = 0, ushort currentBuildingId = 0);

        /// <summary>
        /// Get the number of workers currently working in the specified <paramref name="buildingId"/>
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>the number of workers in the specified building</returns>
        internal int GetWorkersInBuilding(ushort buildingId);

        /// <summary>
        /// Get an array of workers that belong to specified <paramref name="buildingId"/>
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>an array of workers that belong to the specified building</returns>
        internal uint[] GetBuildingWorkForce(ushort buildingId);

        /// <summary>Check if the building has units of a specific type</summary>
        /// <param name="buildingID">The ID of the building to check units for.</param>
        /// <param name="flag">The flag type to check units of this type exist.</param>
        /// <returns>
        ///   <c>true</c> if the specified <paramref name="buildingID"/> have those units available; otherwise, <c>false</c>.
        /// </returns>
        internal bool HaveUnits(ushort buildingID, CitizenUnit.Flags flag);

        /// <summary>Finds an active building that matches the specified criteria and can accept visitors.</summary>
        /// <param name="searchAreaCenterBuilding">The building ID that represents the search area center point.</param>
        /// <param name="maxDistance">The maximum distance for search, the search area radius.</param>
        /// <param name="service">The building service type to find.</param>
        /// <param name="subService">The building sub-service type to find.</param>
        /// <param name="buildingType">The commercial building type the citizen is going to visit.</param>
        /// <returns>An ID of the first found building, or 0 if none found.</returns>
        internal ushort FindActiveBuilding(
            ushort searchAreaCenterBuilding,
            float maxDistance,
            ItemClass.Service service,
            ItemClass.SubService subService = ItemClass.SubService.None,
            CommercialBuildingType buildingType = CommercialBuildingType.None);

        /// <summary>Finds an active building that matches the specified criteria and can accept visitors.</summary>
        /// <param name="position">The search area center point.</param>
        /// <param name="maxDistance">The maximum distance for search, the search area radius.</param>
        /// <param name="service">The building service type to find.</param>
        /// <param name="subService">The building sub-service type to find.</param>
        /// <param name="currentBuilding">The current building the citizen is in.</param>
        /// <param name="buildingType">The commercial building type the citizen is going to visit.</param>
        /// <returns>An ID of the first found building, or 0 if none found.</returns>
        internal ushort FindActiveBuilding(
            Vector3 position,
            float maxDistance,
            ItemClass.Service service,
            ItemClass.SubService subService = ItemClass.SubService.None,
            ushort currentBuilding = 0,
            CommercialBuildingType buildingType = CommercialBuildingType.None);

        /// <summary>Finds an active cafeteria building that is in the same campus.</summary>
        /// <param name="searchAreaCenterBuilding">The building ID that represents the search area center point.</param>
        /// <param name="maxDistance">The maximum distance for search, the search area radius.</param>
        /// <returns>An ID of the first found building, or 0 if none found.</returns>
        internal ushort FindActiveCafeteria(ushort searchAreaCenterBuilding, float maxDistance);

        /// <summary>
        /// Determines whether the building with the specified <paramref name="buildingId"/> is going to get closed in one houer or less
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> f the building with the specified <paramref name="buildingId"/> is going to get closed in one houer or less, <c>false</c>.
        /// </returns>
        internal bool IsBuildingClosingSoon(ushort buildingId);
    }
}
