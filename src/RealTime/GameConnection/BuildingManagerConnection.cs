// BuildingManagerConnection.cs

namespace RealTime.GameConnection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ColossalFramework;
    using RealTime.Config;
    using RealTime.CustomAI;
    using RealTime.Simulation;
    using SkyTools.Tools;
    using UnityEngine;
    using static CustomAI.Constants;

    /// <summary>
    /// A default implementation of the <see cref="IBuildingManagerConnection"/> interface.
    /// </summary>
    /// <seealso cref="IBuildingManagerConnection" />
    internal sealed class BuildingManagerConnection : IBuildingManagerConnection
    {
        private const int MaxBuildingGridIndex = BuildingManager.BUILDINGGRID_RESOLUTION - 1;
        private const int BuildingGridMiddle = BuildingManager.BUILDINGGRID_RESOLUTION / 2;


        private static readonly string[] Hotel_Names =
        [
            "Hotel",
            "hotel",
            "Crescent",
            "Obsidian",
            "Yggdrasil",
            "K207",
            "Rental",
            "Inn",
            "Babylon"
        ];

        private readonly RealTimeConfig config;
        public ITimeInfo TimeInfo;

        /// <summary>Initializes a new instance of the <see cref="TimeInfo" /> class.</summary>
        /// <param name="config">The configuration to run with.</param>
        /// <exception cref="ArgumentNullException">Thrown when the argument is null.</exception>
        public BuildingManagerConnection(RealTimeConfig config, ITimeInfo timeInfo)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            TimeInfo = timeInfo ?? throw new ArgumentNullException(nameof(timeInfo));
        }

        /// <summary>Gets the service type of the building with specified ID.</summary>
        /// <param name="buildingId">The ID of the building to get the service type of.</param>
        /// <returns>
        /// The service type of the building with the specified ID, or
        /// <see cref="ItemClass.Service.None" /> if <paramref name="buildingId" /> is 0.
        /// </returns>
        public ItemClass.Service GetBuildingService(ushort buildingId) =>
            buildingId == 0
                ? ItemClass.Service.None
                : BuildingManager.instance.m_buildings.m_buffer[buildingId].Info?.m_class?.m_service ?? ItemClass.Service.None;

        /// <summary>Gets the sub-service type of the building with specified ID.</summary>
        /// <param name="buildingId">The ID of the building to get the sub-service type of.</param>
        /// <returns>
        /// The sub-service type of the building with the specified ID, or
        /// <see cref="ItemClass.SubService.None" /> if <paramref name="buildingId" /> is 0.
        /// </returns>
        public ItemClass.SubService GetBuildingSubService(ushort buildingId) =>
            buildingId == 0
                ? ItemClass.SubService.None
                : BuildingManager.instance.m_buildings.m_buffer[buildingId].Info?.m_class?.m_subService ?? ItemClass.SubService.None;

        /// <summary>Gets the service and sub-service types of the building with specified ID.</summary>
        /// <param name="buildingId">The ID of the building to get the service and sub-service types of.</param>
        /// <param name="service">The service type of the building with the specified ID, or
        /// <see cref="ItemClass.Service.None"/> if <paramref name="buildingId"/> is 0.</param>
        /// <param name="subService">The sub-service type of the building with the specified ID, or
        /// <see cref="ItemClass.SubService.None"/> if <paramref name="buildingId"/> is 0.</param>
        public void GetBuildingService(ushort buildingId, out ItemClass.Service service, out ItemClass.SubService subService)
        {
            if (buildingId == 0)
            {
                service = ItemClass.Service.None;
                subService = ItemClass.SubService.None;
                return;
            }

            var itemClass = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info?.m_class;
            if (itemClass == null)
            {
                service = ItemClass.Service.None;
                subService = ItemClass.SubService.None;
                return;
            }

            service = itemClass.m_service;
            subService = itemClass.m_subService;
            
        }

        /// <summary>Gets the citizen unit ID for the building with specified ID.</summary>
        /// <param name="buildingId">The building ID to search the citizen unit for.</param>
        /// <returns>The ID of the building's citizen unit, or 0 if none.</returns>
        public uint GetCitizenUnit(ushort buildingId) =>
            buildingId == 0
                ? 0
                : BuildingManager.instance.m_buildings.m_buffer[buildingId].m_citizenUnits;

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
        public bool BuildingHasFlags(ushort buildingId, Building.Flags flags, bool includeZero = false)
        {
            if (buildingId == 0)
            {
                return false;
            }

            var buildingFlags = BuildingManager.instance.m_buildings.m_buffer[buildingId].m_flags;
            return (buildingFlags & flags) != 0 || includeZero && buildingFlags == Building.Flags.None;
        }

        /// <summary>
        /// Gets the distance in game units between two buildings with specified IDs.
        /// </summary>
        /// <param name="building1">The ID of the first building.</param>
        /// <param name="building2">The ID of the second building.</param>
        /// <returns>
        /// A distance between the buildings with specified IDs, 0 when any of the IDs is 0.
        /// </returns>
        public float GetDistanceBetweenBuildings(ushort building1, ushort building2)
        {
            if (building1 == 0 || building2 == 0)
            {
                return 0;
            }

            var buildings = BuildingManager.instance.m_buildings.m_buffer;
            return Vector3.Distance(buildings[building1].m_position, buildings[building2].m_position);
        }

        /// <summary>Modifies the building's material buffer.</summary>
        /// <param name="buildingId">The ID of the building to modify.</param>
        /// <param name="reason">The reason for modification.</param>
        /// <param name="delta">The amount to modify the buffer by.</param>
        public void ModifyMaterialBuffer(ushort buildingId, TransferManager.TransferReason reason, int delta)
        {
            if (buildingId == 0 || delta == 0)
            {
                return;
            }

            ref var building = ref BuildingManager.instance.m_buildings.m_buffer[buildingId];
            building.Info?.m_buildingAI.ModifyMaterialBuffer(buildingId, ref building, reason, ref delta);
        }

        /// <summary>Finds an active building that matches the specified criteria and can accept visitors.</summary>
        /// <param name="searchAreaCenterBuilding">The building ID that represents the search area center point.</param>
        /// <param name="maxDistance">The maximum distance for search, the search area radius.</param>
        /// <param name="service">The building service type to find.</param>
        /// <param name="subService">The building sub-service type to find.</param>
        /// <param name="textArr">The text the building name must include - array of names could be any of them.</param>
        /// <param name="IgnoreSubServices">The building sub-service array types to ignore when searching for a building to find.</param>
        /// <returns>An ID of the first found building, or 0 if none found.</returns>
        public ushort FindActiveBuilding(
            ushort searchAreaCenterBuilding,
            float maxDistance,
            ItemClass.Service service,
            ItemClass.SubService subService = ItemClass.SubService.None,
            bool isShopping = true)
        {
            if (searchAreaCenterBuilding == 0)
            {
                return 0;
            }

            var currentPosition = BuildingManager.instance.m_buildings.m_buffer[searchAreaCenterBuilding].m_position;
            return FindActiveBuilding(currentPosition, maxDistance, service, subService, isShopping);
        }

        /// <summary>Finds an active building that matches the specified criteria and can accept visitors.</summary>
        /// <param name="position">The search area center point.</param>
        /// <param name="maxDistance">The maximum distance for search, the search area radius.</param>
        /// <param name="service">The building service type to find.</param>
        /// <param name="subService">The building sub-service type to find.</param>
        /// <param name="textArr">The text the building name must include - array of names could be any of them.</param>
        /// <param name="IgnoreSubServices">The building sub-service array types to ignore when searching for a building to find.</param>
        /// <returns>An ID of the first found building, or 0 if none found.</returns>
        public ushort FindActiveBuilding(
            Vector3 position,
            float maxDistance,
            ItemClass.Service service,
            ItemClass.SubService subService = ItemClass.SubService.None,
            bool isShopping = true)
        {
            if (position == Vector3.zero)
            {
                return 0;
            }

            const Building.Flags restrictedFlags = Building.Flags.Deleted | Building.Flags.Evacuating | Building.Flags.Flooded | Building.Flags.Collapsed
                | Building.Flags.BurnedDown | Building.Flags.RoadAccessFailed;

            const Building.Flags requiredFlags = Building.Flags.Created | Building.Flags.Completed | Building.Flags.Active;
            const Building.Flags combinedFlags = requiredFlags | restrictedFlags;

            int gridXFrom = Mathf.Max((int)((position.x - maxDistance) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), 0);
            int gridZFrom = Mathf.Max((int)((position.z - maxDistance) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), 0);
            int gridXTo = Mathf.Min((int)((position.x + maxDistance) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), MaxBuildingGridIndex);
            int gridZTo = Mathf.Min((int)((position.z + maxDistance) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), MaxBuildingGridIndex);

            float sqrMaxDistance = maxDistance * maxDistance;
            for (int z = gridZFrom; z <= gridZTo; ++z)
            {
                for (int x = gridXFrom; x <= gridXTo; ++x)
                {
                    ushort buildingId = BuildingManager.instance.m_buildingGrid[z * BuildingManager.BUILDINGGRID_RESOLUTION + x];
                    uint counter = 0;
                    while (buildingId != 0)
                    {
                        ref var building = ref BuildingManager.instance.m_buildings.m_buffer[buildingId];
                        var building_service = building.Info.m_class.m_service;
                        var building_subService = building.Info.m_class.m_subService;
                        bool allowed = true;
                        if (building.Info?.m_class != null
                            && building_service == service
                            && (subService == ItemClass.SubService.None || building_subService == subService)
                            && IsBuildingWorking(buildingId)
                            && (building.m_flags & combinedFlags) == requiredFlags)
                        {
                            if(!isShopping && building_service == ItemClass.Service.Commercial && building_subService == ItemClass.SubService.CommercialLeisure)
                            {
                                allowed = false;
                            }

                            if (building_service == ItemClass.Service.Commercial && building_subService == ItemClass.SubService.CommercialTourist)
                            {
                                allowed = false;
                            }

                            float sqrDistance = Vector3.SqrMagnitude(position - building.m_position);
                            if (sqrDistance < sqrMaxDistance && BuildingCanBeVisited(buildingId) && allowed)
                            {
                                return buildingId;
                            }
                        }

                        buildingId = building.m_nextGridBuilding;
                        if (++counter >= BuildingManager.MAX_BUILDING_COUNT)
                        {
                            break;
                        }
                    }
                }
            }

            return 0;
        }

        /// <summary>Finds an active hotel building that matches the specified criteria.</summary>
        /// <param name="searchAreaCenterBuilding">The building ID that represents the search area center point.</param>
        /// <param name="maxDistance">The maximum distance for search, the search area radius.</param>
        /// <returns>An ID of the first found building, or 0 if none found.</returns>
        public ushort FindActiveHotel(ushort searchAreaCenterBuilding, float maxDistance)
        {
            if (searchAreaCenterBuilding == 0)
            {
                return 0;
            }

            var currentPosition = BuildingManager.instance.m_buildings.m_buffer[searchAreaCenterBuilding].m_position;
            return FindActiveHotel(currentPosition, maxDistance);
        }

        /// <summary>Finds an active hotel building that matches the specified criteria.</summary>
        /// <param name="position">The search area center point.</param>
        /// <param name="maxDistance">The maximum distance for search, the search area radius.</param>
        /// <returns>An ID of the first found building, or 0 if none found.</returns>
        public ushort FindActiveHotel(Vector3 position, float maxDistance)
        {
            if (position == Vector3.zero)
            {
                return 0;
            }

            const Building.Flags restrictedFlags = Building.Flags.Deleted | Building.Flags.Evacuating | Building.Flags.Flooded | Building.Flags.Collapsed
                | Building.Flags.BurnedDown | Building.Flags.RoadAccessFailed;

            const Building.Flags requiredFlags = Building.Flags.Created | Building.Flags.Completed | Building.Flags.Active;
            const Building.Flags combinedFlags = requiredFlags | restrictedFlags;

            int gridXFrom = Mathf.Max((int)((position.x - maxDistance) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), 0);
            int gridZFrom = Mathf.Max((int)((position.z - maxDistance) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), 0);
            int gridXTo = Mathf.Min((int)((position.x + maxDistance) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), MaxBuildingGridIndex);
            int gridZTo = Mathf.Min((int)((position.z + maxDistance) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), MaxBuildingGridIndex);

            float sqrMaxDistance = maxDistance * maxDistance;
            for (int z = gridZFrom; z <= gridZTo; ++z)
            {
                for (int x = gridXFrom; x <= gridXTo; ++x)
                {
                    ushort buildingId = BuildingManager.instance.m_buildingGrid[z * BuildingManager.BUILDINGGRID_RESOLUTION + x];
                    uint counter = 0;
                    while (buildingId != 0)
                    {
                        var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
                        if (IsHotel(buildingId) && building.m_roomUsed < building.m_roomMax && IsBuildingWorking(buildingId) && (building.m_flags & combinedFlags) == requiredFlags)
                        {
                            float sqrDistance = Vector3.SqrMagnitude(position - building.m_position);
                            if (sqrDistance < sqrMaxDistance && HotelCanBeCheckedInTo(buildingId))
                            {
                                return buildingId;
                            }
                        }

                        buildingId = building.m_nextGridBuilding;
                        if (++counter >= BuildingManager.MAX_BUILDING_COUNT)
                        {
                            break;
                        }
                    }
                }
            }

            return 0;
        }

        /// <summary>Finds an active cafeteria building that matches the specified criteria.</summary>
        /// <param name="position">The search area center point.</param>
        /// <param name="maxDistance">The maximum distance for search, the search area radius.</param>
        /// <returns>An ID of the first found building, or 0 if none found.</returns>
        public ushort FindActiveCafeteria(ushort searchAreaCenterBuilding, float maxDistance)
        {
            if (searchAreaCenterBuilding == 0)
            {
                return 0;
            }
            var currentBuilding = BuildingManager.instance.m_buildings.m_buffer[searchAreaCenterBuilding];
            var position = currentBuilding.m_position;
            if (position == Vector3.zero)
            {
                return 0;
            }

            const Building.Flags restrictedFlags = Building.Flags.Deleted | Building.Flags.Evacuating | Building.Flags.Flooded | Building.Flags.Collapsed
                | Building.Flags.BurnedDown;

            const Building.Flags requiredFlags = Building.Flags.Created | Building.Flags.Completed | Building.Flags.Active;
            const Building.Flags combinedFlags = requiredFlags | restrictedFlags;

            int gridXFrom = Mathf.Max((int)((position.x - maxDistance) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), 0);
            int gridZFrom = Mathf.Max((int)((position.z - maxDistance) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), 0);
            int gridXTo = Mathf.Min((int)((position.x + maxDistance) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), MaxBuildingGridIndex);
            int gridZTo = Mathf.Min((int)((position.z + maxDistance) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), MaxBuildingGridIndex);

            float sqrMaxDistance = maxDistance * maxDistance;
            for (int z = gridZFrom; z <= gridZTo; ++z)
            {
                for (int x = gridXFrom; x <= gridXTo; ++x)
                {
                    ushort buildingId = BuildingManager.instance.m_buildingGrid[z * BuildingManager.BUILDINGGRID_RESOLUTION + x];
                    uint counter = 0;
                    while (buildingId != 0)
                    {
                        var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
                        if (building.Info.GetAI() is CampusBuildingAI && building.Info.name.Contains("Cafeteria")
                            && CheckSameCampusArea(searchAreaCenterBuilding, buildingId)
                            && IsBuildingWorking(buildingId) &&
                            (building.m_flags & combinedFlags) == requiredFlags)
                        {
                            float sqrDistance = Vector3.SqrMagnitude(position - building.m_position);
                            if (sqrDistance < sqrMaxDistance)
                            {
                                return buildingId;
                            }
                        }

                        buildingId = building.m_nextGridBuilding;
                        if (++counter >= BuildingManager.MAX_BUILDING_COUNT)
                        {
                            break;
                        }
                    }
                }
            }

            return 0;
        }

        /// <summary>Gets the ID of an event that takes place in the building with specified ID.</summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>An ID of an event that takes place in the building, or 0 if none.</returns>
        public ushort GetEvent(ushort buildingId) =>
            buildingId == 0
                ? (ushort)0
                : BuildingManager.instance.m_buildings.m_buffer[buildingId].m_eventIndex;

        /// <summary>
        /// Gets an ID of a random building in the city that belongs to any of the specified <paramref name="services" />.
        /// </summary>
        /// <param name="services">A collection of <see cref="ItemClass.Service" /> that specifies in which services to
        /// search the random building in.</param>
        /// <returns>An ID of a building; or 0 if none found.</returns>
        /// <remarks>
        /// NOTE: this method creates objects on the heap. To avoid memory pressure, don't call it on
        /// every simulation step.
        /// </remarks>
        public ushort GetRandomBuilding(IEnumerable<ItemClass.Service> services)
        {
            // No memory pressure here because this method will not be called on each simulation step
            var buildings = new List<FastList<ushort>>();

            int totalCount = 0;
            foreach (var serviceBuildings in services
                .Select(s => BuildingManager.instance.GetServiceBuildings(s))
                .Where(b => b != null))
            {
                totalCount += serviceBuildings.m_size;
                buildings.Add(serviceBuildings);
            }

            if (totalCount == 0)
            {
                return 0;
            }

            int buildingNumber = SimulationManager.instance.m_randomizer.Int32((uint)totalCount);
            totalCount = 0;
            foreach (var serviceBuildings in buildings)
            {
                if (buildingNumber < totalCount + serviceBuildings.m_size)
                {
                    return serviceBuildings[buildingNumber - totalCount];
                }

                totalCount += serviceBuildings.m_size;
            }

            return 0;
        }

        /// <summary>
        /// Sets the outgoing problem timer value for the building with specified ID.
        /// </summary>
        /// <param name="buildingId">The ID of building to set the problem timer for.</param>
        /// <param name="value">The outgoing problem timer value to set.</param>
        public void SetOutgoingProblemTimer(ushort buildingId, byte value)
        {
            if (buildingId != 0)
            {
                BuildingManager.instance.m_buildings.m_buffer[buildingId].m_outgoingProblemTimer = value;
            }
        }

        /// <summary>
        /// Sets the workers problem timer value for the building with specified ID.
        /// </summary>
        /// <param name="buildingId">The ID of building to set the problem timer for.</param>
        /// <param name="value">The workers problem timer value to set.</param>
        public void SetWorkersProblemTimer(ushort buildingId, byte value)
        {
            if (buildingId != 0)
            {
                BuildingManager.instance.m_buildings.m_buffer[buildingId].m_workerProblemTimer = value;
            }
        }

        /// <summary>Gets the class name of the building with specified ID.</summary>
        /// <param name="buildingId">The building ID to get the class name of.</param>
        /// <returns>
        /// A string representation of the building class, or null if none found.
        /// </returns>
        public string GetBuildingClassName(ushort buildingId) =>
            buildingId == 0
                ? string.Empty
                : BuildingManager.instance.m_buildings.m_buffer[buildingId].Info?.name ?? string.Empty;

        /// <summary>Gets the localized name of a building with specified ID.</summary>
        /// <param name="buildingId">The building ID to get the name of.</param>
        /// <returns>A localized building name string, or null if none found.</returns>
        public string GetBuildingName(ushort buildingId) =>
            buildingId == 0
                ? string.Empty
                : BuildingManager.instance.GetBuildingName(buildingId, InstanceID.Empty);

        /// <summary>
        /// Determines whether the building with specified ID is located in a noise restricted district.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building with specified ID is located in a noise restricted district;
        ///   otherwise, <c>false</c>.
        /// </returns>
        public bool IsBuildingNoiseRestricted(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return false;
            }

            var location = BuildingManager.instance.m_buildings.m_buffer[buildingId].m_position;
            byte district = DistrictManager.instance.GetDistrict(location);
            var policies = DistrictManager.instance.m_districts.m_buffer[district].m_cityPlanningPolicies;
            return (policies & DistrictPolicies.CityPlanning.NoLoudNoises) != 0;
        }

        /// <summary>Gets the maximum possible buildings count.</summary>
        /// <returns>The maximum possible buildings count.</returns>
        public int GetMaxBuildingsCount() => BuildingManager.instance.m_buildings.m_buffer.Length;

        /// <summary>Gets the current buildings count in the city.</summary>
        /// <returns>The current buildings count.</returns>
        public int GeBuildingsCount() => (int)BuildingManager.instance.m_buildings.ItemCount();

        /// <summary>Updates the building colors in the game by re-rendering the building.</summary>
        /// <param name="buildingId">The ID of the building to update.</param>
        public void UpdateBuildingColors(ushort buildingId)
        {
            if (buildingId > 0
                && (BuildingManager.instance.m_buildings.m_buffer[buildingId].m_flags & Building.Flags.Created) != 0)
            {
                BuildingManager.instance.UpdateBuildingColors(buildingId);
            }
        }

        /// <summary>Gets the building's height in game units.</summary>
        /// <param name="buildingId">The ID of the building.</param>
        /// <returns>The height of the building with the specified ID.</returns>
        public float GetBuildingHeight(ushort buildingId) =>
            buildingId == 0
                ? 0f
                : BuildingManager.instance.m_buildings.m_buffer[buildingId].Info?.m_size.y ?? 0f;

        /// <summary>
        /// Visually deactivates the building with specified ID without affecting its production or coverage.
        /// </summary>
        /// <param name="buildingId">The building ID.</param>
        public void DeactivateVisually(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return;
            }

            ref var building = ref BuildingManager.instance.m_buildings.m_buffer[buildingId];
            building.Info?.m_buildingAI.BuildingDeactivated(buildingId, ref building);
        }

        /// <summary>Gets the ID of the park area where the building with specified ID is located. Returns 0 if the building
        /// is not in a park.</summary>
        /// <param name="buildingId">The ID of the building to get the park ID of.</param>
        /// <returns>An ID of the park where the building is located, or 0.</returns>
        public byte GetParkId(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return 0;
            }

            var position = BuildingManager.instance.m_buildings.m_buffer[buildingId].m_position;
            return DistrictManager.instance.GetPark(position);
        }

        /// <summary>Gets the policies for a park with specified ID. Returns <see cref="DistrictPolicies.Park.None"/>
        /// if the specified park ID is 0 or invalid.</summary>
        /// <param name="parkId">The ID of the park to get policies of.</param>
        /// <returns>The policies of the park.</returns>
        public DistrictPolicies.Park GetParkPolicies(byte parkId) =>
            parkId == 0
                ? DistrictPolicies.Park.None
                : DistrictManager.instance.m_parks.m_buffer[parkId].m_parkPolicies;

        /// <summary>
        /// Determines whether the area around the building with specified ID is currently being evacuated.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the area around the building with specified ID is currently being evacuated.; otherwise, <c>false</c>.
        /// </returns>
        public bool IsAreaEvacuating(ushort buildingId)
            => buildingId != 0 && DisasterManager.instance.IsEvacuating(BuildingManager.instance.m_buildings.m_buffer[buildingId].m_position);

        /// <summary>
        /// Determines whether the building with specified ID is a real unique building (not a stadium, not a concert area).
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building with the specified ID is a real unique building; otherwise, <c>false</c>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("General", "RCS1130", Justification = "The EventType enum has no [Flags] attribute but has values of power of 2")]
        public bool IsRealUniqueBuilding(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return false;
            }

            var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
            if (buildingInfo?.m_class?.m_service != ItemClass.Service.Monument)
            {
                return false;
            }

            var monumentAI = buildingInfo.m_buildingAI as MonumentAI;
            return monumentAI != null
                && (monumentAI.m_supportEvents & (EventManager.EventType.Football | EventManager.EventType.Concert)) == 0;
        }

        /// <summary>
        /// Determines whether the AI class of the building with specified ID is of the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the building AI to check for. It must be a <see cref="BuildingAI"/>.</typeparam>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the AI class of the building with the specified ID is of the type <typeparamref name="T"/>;
        ///   otherwise, <c>false</c>.
        /// </returns>
        public bool IsBuildingAIOfType<T>(ushort buildingId)
            where T : BuildingAI
        {
            if (buildingId == 0)
            {
                return false;
            }

            var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
            return buildingInfo?.m_buildingAI is T;
        }

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
        public bool IsBuildingServiceLevel(ushort buildingId, ItemClass.Service buildingService, ItemClass.Level buildingLevel)
        {
            if (buildingId == 0)
            {
                return false;
            }

            var buildingInfo = BuildingManager.instance.m_buildings.m_buffer[buildingId].Info;
            if (buildingInfo == null)
            {
                return false;
            }

            return buildingInfo.GetService() == buildingService && buildingInfo.GetClassLevel() == buildingLevel;
        }

        /// <summary>
        /// Determines whether the building with specified ID is a hotel or not.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building is a hotel;
        ///   otherwise, <c>false</c>.
        /// </returns>
        public static bool IsHotel(ushort buildingId)
        {
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];

            if (building.Info.m_class.m_service == ItemClass.Service.Hotel)
            {
                return true;
            }

            if (building.Info.m_class.m_service == ItemClass.Service.Commercial && building.Info.m_class.m_subService == ItemClass.SubService.CommercialTourist
                && Hotel_Names.Any(name => building.Info.name.Contains(name)))
            {
                return true;
            }

            if (building.Info.m_buildingAI.GetType().Name.Equals("AirportHotelAI") || building.Info.m_buildingAI.GetType().Name.Equals("ParkHotelAI"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the building with the specified <paramref name="buildingId"/> is currently working
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <param name="timeBeforeWork">time before work the citizen can arrive without an issue.</param>
        /// <returns>
        ///   <c>true</c> if the building with the specified <paramref name="buildingId"/> is currently working otherwise, <c>false</c>.
        /// </returns>
        public bool IsBuildingWorking(ushort buildingId, int timeBeforeWork = 0)
        {
            if(buildingId == 0)
            {
                return true;
            }
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            BuildingWorkTimeManager.WorkTime workTime;

            if (!BuildingWorkTimeManager.BuildingWorkTimeExist(buildingId))
            {
                if (!BuildingWorkTimeManager.ShouldHaveBuildingWorkTime(buildingId))
                {
                    return true;
                }
                workTime = BuildingWorkTimeManager.CreateBuildingWorkTime(buildingId, building.Info);
            }
            else
            {
                workTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingId);
            }

            // WorkForceMatters setting is enabled and no one at work - building will not work
            if (config.WorkForceMatters && GetWorkersInBuilding(buildingId) == 0)
            {
                return false;
            }

            float currentHour = TimeInfo.CurrentHour;
            if (workTime.HasExtendedWorkShift)
            {
                float extendedShiftBegin = Math.Min(config.SchoolBegin, config.WakeUpHour);

                if (building.Info.m_class.m_service == ItemClass.Service.Education || building.Info.m_class.m_service == ItemClass.Service.PlayerEducation)
                {
                    if (config.IsWeekendEnabled && TimeInfo.Now.IsWeekend() && !workTime.WorkAtWeekands)
                    {
                        return false;
                    }

                    if (TimeInfo.IsNightTime && !workTime.WorkAtNight)
                    {
                        return false;
                    }

                    float startHour = Math.Min(EarliestWakeUp, extendedShiftBegin);
                    if (workTime.WorkShifts == 1)
                    {
                        return currentHour >= startHour - timeBeforeWork && currentHour < config.SchoolEnd;
                    }
                    else if (workTime.WorkShifts == 2)
                    {
                        // universities - might have night classes closes at 10 pm
                        return currentHour >= startHour - timeBeforeWork && currentHour < 22f;
                    }
                    else if (workTime.WorkShifts == 3)
                    {
                        return true;
                    }
                    else
                    {
                        return false; // should never get here
                    }
                }
                else
                {
                    if (config.IsWeekendEnabled && TimeInfo.Now.IsWeekend() && !workTime.WorkAtWeekands)
                    {
                        return false;
                    }

                    if (TimeInfo.IsNightTime && !workTime.WorkAtNight)
                    {
                        return false;
                    }

                    extendedShiftBegin = config.WakeUpHour;
                    float startHour = Math.Min(EarliestWakeUp, extendedShiftBegin);
                    if (workTime.WorkShifts == 1)
                    {
                        return currentHour >= startHour - timeBeforeWork && currentHour < config.WorkEnd;
                    }
                    else if (workTime.WorkShifts == 2)
                    {
                        // universities - might have night classes closes at 10 pm
                        return currentHour >= startHour - timeBeforeWork && currentHour < 22f;
                    }
                    else if (workTime.WorkShifts == 3)
                    {
                        return true;
                    }
                    else
                    {
                        return false; // should never get here
                    }
                }
            }
            else if (workTime.HasContinuousWorkShift)
            {
                if (config.IsWeekendEnabled && TimeInfo.Now.IsWeekend() && !workTime.WorkAtWeekands)
                {
                    return false;
                }

                if (TimeInfo.IsNightTime && !workTime.WorkAtNight)
                {
                    return false;
                }

                if (workTime.WorkShifts == 1)
                {
                    return currentHour >= 8f - timeBeforeWork && currentHour < 20f;
                }
                else if (workTime.WorkShifts == 2)
                {
                    return true; // two work shifts
                }
                else
                {
                    return false; // should never get here
                }
            }
            else
            {
                if (config.IsWeekendEnabled && TimeInfo.Now.IsWeekend() && !workTime.WorkAtWeekands)
                {
                    return false;
                }

                if (TimeInfo.IsNightTime && !workTime.WorkAtNight)
                {
                    return false;
                }

                if (workTime.WorkShifts == 1)
                {
                    return currentHour >= config.WorkBegin - timeBeforeWork && currentHour < config.WorkEnd;
                }
                else if (workTime.WorkShifts == 2)
                {
                    return currentHour >= config.WorkBegin - timeBeforeWork && currentHour < config.GoToSleepHour;
                }
                else if (workTime.WorkShifts == 3)
                {
                    return true; // three work shifts
                }
                else
                {
                    return false; // should never get here
                }
            }
        }

        /// <summary>
        /// Determines whether building A and building B belong to the same campus.
        /// </summary>
        /// <param name="currentBuilding">Building A.</param>
        /// <param name="visitBuilding">Building B.</param>
        /// <returns>
        ///   <c>true</c> if the two buildings belong to the same campus;
        ///   otherwise, <c>false</c>.
        /// </returns>
        public static bool CheckSameCampusArea(ushort currentBuildingId, ushort visitBuildingId)
        {
            if(currentBuildingId == 0 || visitBuildingId == 0)
            {
                return false;
            }
            var currentBuilding = BuildingManager.instance.m_buildings.m_buffer[currentBuildingId];
            var visitBuilding = BuildingManager.instance.m_buildings.m_buffer[visitBuildingId];
            if (currentBuilding.Info == null || visitBuilding.Info == null)
            {
                return false;
            }

            var currentBuildingAI = currentBuilding.Info.m_buildingAI as CampusBuildingAI;
            var visitBuildingAI = visitBuilding.Info.m_buildingAI as CampusBuildingAI;

            if (currentBuildingAI == null || visitBuildingAI == null)
            {
                return false;
            }

            if (currentBuilding.m_position == Vector3.zero || visitBuilding.m_position == Vector3.zero)
            {
                return false;
            }

            var instance = Singleton<DistrictManager>.instance;

            byte b1 = instance.GetPark(currentBuilding.m_position);
            byte b2 = instance.GetPark(visitBuilding.m_position);
            if (b1 != 0)
            {
                if (!instance.m_parks.m_buffer[b1].IsCampus)
                {
                    b1 = 0;
                }
                else if (currentBuildingAI.m_campusType == DistrictPark.ParkType.GenericCampus || currentBuildingAI.m_campusType != instance.m_parks.m_buffer[b1].m_parkType)
                {
                    b1 = 0;
                }
            }
            if (b2 != 0)
            {
                if (!instance.m_parks.m_buffer[b2].IsCampus)
                {
                    b2 = 0;
                }
                else if (visitBuildingAI.m_campusType == DistrictPark.ParkType.GenericCampus || visitBuildingAI.m_campusType != instance.m_parks.m_buffer[b2].m_parkType)
                {
                    b2 = 0;
                }
            }
            if (b1 != 0 && b2 != 0 && b1 == b2)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get the number of workers currently working in the specified <paramref name="buildingId"/>
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>the number of workers in the specified building</returns>
        public int GetWorkersInBuilding(ushort buildingId)
        {
            int count = 0;
            uint[] workforce = GetBuildingWorkForce(buildingId);
            for (int i = 0; i < workforce.Length; i++)
            {
                var citizen = CitizenManager.instance.m_citizens.m_buffer[workforce[i]];

                // check if student
                bool isStudent = (citizen.m_flags & Citizen.Flags.Student) != 0 || Citizen.GetAgeGroup(citizen.m_age) == Citizen.AgeGroup.Child || Citizen.GetAgeGroup(citizen.m_age) == Citizen.AgeGroup.Teen;

                // if at work and not a student and current building is the work building
                if (citizen.CurrentLocation == Citizen.Location.Work && citizen.m_workBuilding == buildingId && !isStudent)
                {
                    count++;
                }
            }
            // support buildings that does not have workers at all
            if (workforce.Length == 0)
            {
                return 1;
            }
            return count;
        }

        /// <summary>
        /// Get an array of workers that belong to specified <paramref name="buildingId"/>
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>an array of workers that belong to the specified building</returns>
        public uint[] GetBuildingWorkForce(ushort buildingId)
        {
            var workforce = new List<uint>();
            var buildingData = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            var instance = Singleton<CitizenManager>.instance;
            uint num = buildingData.m_citizenUnits;
            int num2 = 0;
            while (num != 0)
            {
                if ((instance.m_units.m_buffer[num].m_flags & CitizenUnit.Flags.Work) != 0)
                {
                    if (instance.m_units.m_buffer[num].m_citizen0 != 0)
                    {
                        workforce.Add(instance.m_units.m_buffer[num].m_citizen0);
                    }
                    if (instance.m_units.m_buffer[num].m_citizen1 != 0)
                    {
                        workforce.Add(instance.m_units.m_buffer[num].m_citizen1);
                    }
                    if (instance.m_units.m_buffer[num].m_citizen2 != 0)
                    {
                        workforce.Add(instance.m_units.m_buffer[num].m_citizen2);
                    }
                    if (instance.m_units.m_buffer[num].m_citizen3 != 0)
                    {
                        workforce.Add(instance.m_units.m_buffer[num].m_citizen3);
                    }
                    if (instance.m_units.m_buffer[num].m_citizen4 != 0)
                    {
                        workforce.Add(instance.m_units.m_buffer[num].m_citizen4);
                    }
                }
                num = instance.m_units.m_buffer[num].m_nextUnit;
                if (++num2 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
            return workforce.ToArray();
        }

        private static bool BuildingCanBeVisited(ushort buildingId)
        {
            var citizenUnitBuffer = Singleton<CitizenManager>.instance.m_units.m_buffer;
            uint currentUnitId = BuildingManager.instance.m_buildings.m_buffer[buildingId].m_citizenUnits;
            int unitBufferSize = citizenUnitBuffer.Length;

            uint counter = 0;
            while (currentUnitId != 0)
            {
                ref var currentUnit = ref citizenUnitBuffer[currentUnitId];
                if ((currentUnit.m_flags & CitizenUnit.Flags.Visit) != 0
                    && (currentUnit.m_citizen0 == 0
                        || currentUnit.m_citizen1 == 0
                        || currentUnit.m_citizen2 == 0
                        || currentUnit.m_citizen3 == 0
                        || currentUnit.m_citizen4 == 0))
                {
                    return true;
                }

                currentUnitId = currentUnit.m_nextUnit;
                if (++counter >= unitBufferSize)
                {
                    break;
                }
            }

            return false;
        }

        private static bool HotelCanBeCheckedInTo(ushort buildingId)
        {
            var citizenUnitBuffer = Singleton<CitizenManager>.instance.m_units.m_buffer;
            uint currentUnitId = BuildingManager.instance.m_buildings.m_buffer[buildingId].m_citizenUnits;
            int unitBufferSize = citizenUnitBuffer.Length;

            uint counter = 0;
            while (currentUnitId != 0)
            {
                ref var currentUnit = ref citizenUnitBuffer[currentUnitId];
                if ((currentUnit.m_flags & CitizenUnit.Flags.Hotel) != 0
                    && (currentUnit.m_citizen0 == 0
                        || currentUnit.m_citizen1 == 0
                        || currentUnit.m_citizen2 == 0
                        || currentUnit.m_citizen3 == 0
                        || currentUnit.m_citizen4 == 0))
                {
                    return true;
                }

                currentUnitId = currentUnit.m_nextUnit;
                if (++counter >= unitBufferSize)
                {
                    break;
                }
            }

            return false;
        }

    }
}
