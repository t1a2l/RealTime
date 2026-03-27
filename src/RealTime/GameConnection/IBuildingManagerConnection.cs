// IBuildingManagerConnection.cs

namespace RealTime.GameConnection
{
    using System.Collections.Generic;

    /// <summary>An interface for the game specific logic related to the building management.</summary>
    internal interface IBuildingManagerConnection
    {
        /// <summary>Gets the class type of the building with specified ID.</summary>
        /// <param name="buildingId">The ID of the building to get the class type of.</param>
        /// <returns>
        /// The class type of the building with the specified ID, or
        /// <see cref="null"/> if <paramref name="buildingId"/> is 0.
        /// </returns>
        internal ItemClass GetBuildingClass(ushort buildingId);

        /// <summary>Gets the service type of the building with specified ID.</summary>
        /// <param name="buildingId">The ID of the building to get the service type of.</param>
        /// <returns>
        /// The service type of the building with the specified ID, or
        /// <see cref="ItemClass.Service.None"/> if <paramref name="buildingId"/> is 0.
        /// </returns>
        internal ItemClass.Service GetBuildingService(ushort buildingId);

        /// <summary>Gets the sub-service type of the building with specified ID.</summary>
        /// <param name="buildingId">The ID of the building to get the sub-service type of.</param>
        /// <returns>
        /// The sub-service type of the building with the specified ID, or
        /// <see cref="ItemClass.SubService.None"/> if <paramref name="buildingId"/> is 0.
        /// </returns>
        internal ItemClass.SubService GetBuildingSubService(ushort buildingId);

        /// <summary>Gets the level of the building with specified ID.</summary>
        /// <param name="buildingId">The ID of the building to get the level of.</param>
        /// <returns>
        /// The level of the building with the specified ID, or
        /// <see cref="ItemClass.Level.None"/> if <paramref name="buildingId"/> is 0.
        /// </returns>
        internal ItemClass.Level GetBuildingLevel(ushort buildingId);

        /// <summary>Gets the service and sub-service types of the building with specified ID.</summary>
        /// <param name="buildingId">The ID of the building to get the service and sub-service types of.</param>
        /// <param name="service">The service type of the building with the specified ID, or
        /// <see cref="ItemClass.Service.None"/> if <paramref name="buildingId"/> is 0.</param>
        /// <param name="subService">The sub-service type of the building with the specified ID, or
        /// <see cref="ItemClass.SubService.None"/> if <paramref name="buildingId"/> is 0.</param>
        internal void GetBuildingService(ushort buildingId, out ItemClass.Service service, out ItemClass.SubService subService);

        /// <summary>Gets the citizen unit ID for the building with specified ID.</summary>
        /// <param name="buildingId">The building ID to search the citizen unit for.</param>
        /// <returns>The ID of the building's citizen unit, or 0 if none.</returns>
        internal uint GetCitizenUnit(ushort buildingId);

        /// <summary>
        /// Gets a value indicating whether the building with specified ID has particular flags.
        /// The single <see cref="Building.Flags.None"/> value can also be checked for.
        /// </summary>
        /// <param name="buildingId">The ID of the building to check the flags of.</param>
        /// <param name="flags">The building flags to check.</param>
        /// <param name="includeZero"><c>true</c> if a building without any flags can also be considered.</param>
        /// <returns>
        /// <c>true</c> if the building with the specified ID has the specified <paramref name="flags"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        internal bool BuildingHasFlags(ushort buildingId, Building.Flags flags, bool includeZero = false);

        /// <summary>Gets the distance in game units between two buildings with specified IDs.</summary>
        /// <param name="building1">The ID of the first building.</param>
        /// <param name="building2">The ID of the second building.</param>
        /// <returns>
        /// The distance between the buildings with specified IDs, 0 when any of the IDs is 0.
        /// </returns>
        internal float GetDistanceBetweenBuildings(ushort building1, ushort building2);

        /// <summary>Modifies the building's material buffer.</summary>
        /// <param name="buildingId">The ID of the building to modify.</param>
        /// <param name="reason">The reason for modification.</param>
        /// <param name="delta">The amount to modify the buffer by.</param>
        internal void ModifyMaterialBuffer(ushort buildingId, TransferManager.TransferReason reason, int delta);

        /// <summary>Gets the ID of an event that takes place in the building with specified ID.</summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>An ID of an event that takes place in the building, or 0 if none.</returns>
        internal ushort GetEvent(ushort buildingId);

        /// <summary>
        /// Gets an ID of a random building in the city that belongs to any of the specified <paramref name="services"/>.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Thrown when the argument is null.</exception>
        /// <param name="services">
        /// A collection of <see cref="ItemClass.Service"/> that specifies in which services to
        /// search the random building in.
        /// </param>
        /// <returns>An ID of a building; or 0 if none found.</returns>
        /// <remarks>
        /// NOTE: this method creates objects on the heap. To avoid memory pressure, don't call it on
        /// every simulation step.
        /// </remarks>
        internal ushort GetRandomBuilding(IEnumerable<ItemClass.Service> services);

        /// <summary>
        /// Sets the outgoing problem timer value for the building with specified ID.
        /// </summary>
        /// <param name="buildingId">The ID of building to set the problem timer for.</param>
        /// <param name="value">The outgoing problem timer value to set.</param>
        internal void SetOutgoingProblemTimer(ushort buildingId, byte value);

        /// <summary>
        /// Sets the workers problem timer value for the building with specified ID.
        /// </summary>
        /// <param name="buildingId">The ID of building to set the problem timer for.</param>
        /// <param name="value">The workers problem timer value to set.</param>
        internal void SetWorkersProblemTimer(ushort buildingId, byte value);

        /// <summary>Gets the class name of the building with specified ID.</summary>
        /// <param name="buildingId">The building ID to get the class name of.</param>
        /// <returns>A string representation of the building class, or null if none found.</returns>
        internal string GetBuildingClassName(ushort buildingId);

        /// <summary>Gets the localized name of a building with specified ID.</summary>
        /// <param name="buildingId">The building ID to get the name of.</param>
        /// <returns>A localized building name string, or null if none found.</returns>
        internal string GetBuildingName(ushort buildingId);

        /// <summary>Gets the maximum possible buildings count.</summary>
        /// <returns>The maximum possible buildings count.</returns>
        internal int GetMaxBuildingsCount();

        /// <summary>Gets the current buildings count in the city.</summary>
        /// <returns>The current buildings count.</returns>
        internal int GeBuildingsCount();

        /// <summary>Updates the building colors in the game by re-rendering the building.</summary>
        /// <param name="buildingId">The ID of the building to update.</param>
        internal void UpdateBuildingColors(ushort buildingId);

        /// <summary>Gets the building's height in game units.</summary>
        /// <param name="buildingId">The ID of the building.</param>
        /// <returns>The height of the building with the specified ID.</returns>
        internal float GetBuildingHeight(ushort buildingId);

        /// <summary>Visually deactivates the building with specified ID without affecting its production or coverage.</summary>
        /// <param name="buildingId">The building ID.</param>
        internal void DeactivateVisually(ushort buildingId);

        /// <summary>Gets the ID of the park area where the building with specified ID is located. Returns 0 if the building
        /// is not in a park.</summary>
        /// <param name="buildingId">The ID of the building to get the park ID of.</param>
        /// <returns>An ID of the park where the building is located, or 0.</returns>
        internal byte GetParkId(ushort buildingId);

        /// <summary>Gets the policies for a park with specified ID. Returns <see cref="DistrictPolicies.Park.None"/>
        /// if the specified park ID is 0 or invalid.</summary>
        /// <param name="parkId">The ID of the park to get policies of.</param>
        /// <returns>The policies of the park.</returns>
        internal DistrictPolicies.Park GetParkPolicies(byte parkId);

        /// <summary>
        /// Determines whether the area around the building with specified ID is currently being evacuated.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the area around the building with specified ID is currently being evacuated; otherwise, <c>false</c>.
        /// </returns>
        internal bool IsAreaEvacuating(ushort buildingId);

        /// <summary>
        /// Determines whether the building with specified ID is a real unique building (not a stadium, not a concert area).
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building with the specified ID is a real unique building; otherwise, <c>false</c>.
        /// </returns>
        internal bool IsRealUniqueBuilding(ushort buildingId);

        /// <summary>
        /// Determines whether the AI class of the building with specified ID is of the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the building AI to check for. It must be a <see cref="BuildingAI"/>.</typeparam>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the AI class of the building with the specified ID is of the type <typeparamref name="T"/>;
        ///   otherwise, <c>false</c>.
        /// </returns>
        internal bool IsBuildingAIOfType<T>(ushort buildingId) where T : BuildingAI;

        /// <summary>
        /// Determines whether the building with specified ID is of the specified service type and of the specified level.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <param name="buildingService">The service type to check the building for.</param>
        /// <param name="buildingLevel">The building level to check the building for.</param>
        /// <returns>
        ///   <c>true</c> if the building is of the specified service type and of the specified level;
        ///   otherwise, <c>false</c>.
        /// </returns>
        internal bool IsBuildingServiceLevel(ushort buildingId, ItemClass.Service buildingService, ItemClass.Level buildingLevel);

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

    }
}
