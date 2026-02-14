// RealTimeBuildingAI.cs

namespace RealTime.CustomAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ColossalFramework;
    using RealTime.Config;
    using RealTime.GameConnection;
    using RealTime.Managers;
    using RealTime.Simulation;
    using SkyTools.Tools;
    using UnityEngine;
    using static Constants;

    /// <summary>
    /// A class that incorporates the custom logic for the private buildings.
    /// </summary>
    internal sealed class RealTimeBuildingAI : IRealTimeBuildingAI
    {
        private const int ConstructionSpeedPaused = 10880;
        private const int ConstructionSpeedMinimum = 1088;
        private const int StepMask = 0xFF;
        private const int BuildingStepSize = 192;
        private const int ConstructionRestrictionThreshold1 = 100;
        private const int ConstructionRestrictionThreshold2 = 1_000;
        private const int ConstructionRestrictionThreshold3 = 10_000;
        private const int ConstructionRestrictionStep1 = MaximumBuildingsInConstruction / 10;
        private const int ConstructionRestrictionStep2 = MaximumBuildingsInConstruction / 5;
        private const int ConstructionRestrictionScale2 = ConstructionRestrictionThreshold2 / (ConstructionRestrictionStep2 - ConstructionRestrictionStep1);
        private const int ConstructionRestrictionScale3 = ConstructionRestrictionThreshold3 / (MaximumBuildingsInConstruction - ConstructionRestrictionStep2);

        private static readonly string[] CarParkingBuildings = ["parking", "garage", "car park", "Parking", "Car Port", "Garage", "Car Park"];
        private readonly TimeSpan lightStateCheckInterval = TimeSpan.FromSeconds(15);

        private readonly RealTimeConfig config;
        private readonly ITimeInfo timeInfo;
        private readonly IBuildingManagerConnection buildingManager;
        private readonly IToolManagerConnection toolManager;
        private readonly ITravelBehavior travelBehavior;
        private readonly IRandomizer randomizer;

        private readonly bool[] lightStates;
        private readonly byte[] reachingTroubles;
        private readonly HashSet<ushort>[] buildingsInConstruction;

        private const int MaxBuildingGridIndex = BuildingManager.BUILDINGGRID_RESOLUTION - 1;
        private const int BuildingGridMiddle = BuildingManager.BUILDINGGRID_RESOLUTION / 2;

        private int lastProcessedMinute = -1;
        private bool freezeProblemTimers;

        private uint lastConfigConstructionSpeedValue;
        private double constructionSpeedValue;

        private int lightStateCheckFramesInterval;
        private int lightStateCheckCounter;
        private ushort lightCheckStep;

        /// <summary>
        /// Initializes a new instance of the <see cref="RealTimeBuildingAI"/> class.
        /// </summary>
        ///
        /// <param name="config">The configuration to run with.</param>
        /// <param name="timeInfo">The time information source.</param>
        /// <param name="buildingManager">A proxy object that provides a way to call the game-specific methods of the <see cref="BuildingManager"/> class.</param>
        /// <param name="toolManager">A proxy object that provides a way to call the game-specific methods of the <see cref="ToolManager"/> class.</param>
        /// <param name="travelBehavior">A behavior that provides simulation info for the citizens' traveling.</param>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        public RealTimeBuildingAI(
            RealTimeConfig config,
            ITimeInfo timeInfo,
            IBuildingManagerConnection buildingManager,
            IToolManagerConnection toolManager,
            ITravelBehavior travelBehavior,
            IRandomizer randomizer)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.timeInfo = timeInfo ?? throw new ArgumentNullException(nameof(timeInfo));
            this.buildingManager = buildingManager ?? throw new ArgumentNullException(nameof(buildingManager));
            this.toolManager = toolManager ?? throw new ArgumentNullException(nameof(toolManager));
            this.travelBehavior = travelBehavior ?? throw new ArgumentNullException(nameof(travelBehavior));
            this.randomizer = randomizer ?? throw new ArgumentNullException(nameof(randomizer));

            lightStates = new bool[buildingManager.GetMaxBuildingsCount()];
            for (int i = 0; i < lightStates.Length; ++i)
            {
                lightStates[i] = true;
            }

            reachingTroubles = new byte[lightStates.Length];

            // This is to preallocate the hash sets to a large capacity, .NET 3.5 doesn't provide a proper way.
            var preallocated = Enumerable.Range(0, MaximumBuildingsInConstruction * 2).Select(v => (ushort)v).ToList();
            buildingsInConstruction =
            [
                new HashSet<ushort>(preallocated),
                new HashSet<ushort>(preallocated),
                new HashSet<ushort>(preallocated),
                new HashSet<ushort>(preallocated),
            ];

            for (int i = 0; i < buildingsInConstruction.Length; ++i)
            {
                // Calling Clear() doesn't trim the capacity, we're using this trick for preallocating the hash sets
                buildingsInConstruction[i].Clear();
            }
        }

        /// <summary>
        /// Gets the building construction time taking into account the current day time.
        /// </summary>
        ///
        /// <returns>The building construction time in game-specific units (0..10880).</returns>
        public int GetConstructionTime()
        {
            if ((toolManager.GetCurrentMode() & ItemClass.Availability.AssetEditor) != 0)
            {
                return 0;
            }

            if (config.ConstructionSpeed != lastConfigConstructionSpeedValue)
            {
                lastConfigConstructionSpeedValue = config.ConstructionSpeed;
                double inverted = 101d - lastConfigConstructionSpeedValue;
                constructionSpeedValue = inverted * inverted * inverted / 1_000_000d;
            }

            // This causes the construction to not advance in the night time
            return timeInfo.IsNightTime && config.StopConstructionAtNight
                ? ConstructionSpeedPaused
                : (int)(ConstructionSpeedMinimum * constructionSpeedValue);
        }

        /// <summary>
        /// Determines whether a building can be constructed or upgraded in the specified building zone.
        /// </summary>
        /// <param name="buildingZone">The building zone to check.</param>
        /// <param name="buildingId">The building ID. Can be 0 if we're about to construct a new building.</param>
        /// <returns>
        ///   <c>true</c> if a building can be constructed or upgraded; otherwise, <c>false</c>.
        /// </returns>
        public bool CanBuildOrUpgrade(ItemClass.Service buildingZone, ushort buildingId = 0)
        {
            int index;
            switch (buildingZone)
            {
                case ItemClass.Service.Residential:
                    index = 0;
                    break;

                case ItemClass.Service.Commercial:
                    index = 1;
                    break;

                case ItemClass.Service.Industrial:
                    index = 2;
                    break;

                case ItemClass.Service.Office:
                    index = 3;
                    break;

                default:
                    return true;
            }

            var buildings = buildingsInConstruction[index];
            buildings.RemoveWhere(IsBuildingCompletedOrMissing);

            int allowedCount = GetAllowedConstructingUpradingCount(buildingManager.GeBuildingsCount());
            bool result = buildings.Count < allowedCount;
            if (result && buildingId != 0)
            {
                buildings.Add(buildingId);
            }

            return result;
        }

        /// <summary>Registers the building with specified <paramref name="buildingId"/> as being constructed or
        /// upgraded.</summary>
        /// <param name="buildingId">The building ID to register.</param>
        /// <param name="buildingZone">The building zone.</param>
        public void RegisterConstructingBuilding(ushort buildingId, ItemClass.Service buildingZone)
        {
            switch (buildingZone)
            {
                case ItemClass.Service.Residential:
                    buildingsInConstruction[0].Add(buildingId);
                    return;

                case ItemClass.Service.Commercial:
                    buildingsInConstruction[1].Add(buildingId);
                    return;

                case ItemClass.Service.Industrial:
                    buildingsInConstruction[2].Add(buildingId);
                    return;

                case ItemClass.Service.Office:
                    buildingsInConstruction[3].Add(buildingId);
                    return;
            }
        }

        /// <summary>
        /// Performs the custom processing of the outgoing problem timer.
        /// </summary>
        /// <param name="buildingId">The ID of the building to process.</param>
        /// <param name="outgoingProblemTimer">The previous value of the outgoing problem timer.</param>
        public void ProcessBuildingProblems(ushort buildingId, byte outgoingProblemTimer)
        {
            // We have only few customers at night - that's an intended behavior.
            // To avoid commercial buildings from collapsing due to lack of customers,
            // we force the problem timer to pause at night time.
            // In the daytime, the timer is running slower.
            // ignore closed buildings.
            if (timeInfo.IsNightTime || timeInfo.Now.Minute % ProblemTimersInterval != 0 || freezeProblemTimers || !IsBuildingWorking(buildingId))
            {
                buildingManager.SetOutgoingProblemTimer(buildingId, outgoingProblemTimer);
            }
        }

        /// <summary>
        /// Performs the custom processing of the worker problem timer.
        /// </summary>
        /// <param name="buildingId">The ID of the building to process.</param>
        /// <param name="oldValue">The old value of the worker problem timer.</param>
        public void ProcessWorkerProblems(ushort buildingId, byte oldValue)
        {
            // We force the problem timer to pause at night time.
            // In the daytime, the timer is running slower.
            // ignore closed buildings.
            if (timeInfo.IsNightTime || timeInfo.Now.Minute % ProblemTimersInterval != 0 || freezeProblemTimers || !IsBuildingWorking(buildingId))
            {
                buildingManager.SetWorkersProblemTimer(buildingId, oldValue);
            }
        }

        /// <summary>Initializes the state of the all building lights.</summary>
        public void InitializeLightState()
        {
            for (ushort i = 0; i <= StepMask; i++)
            {
                UpdateLightState(i, updateBuilding: false);
            }
        }

        /// <summary>Re-calculates the duration of a simulation frame.</summary>
        public void UpdateFrameDuration()
        {
            lightStateCheckFramesInterval = (int)(lightStateCheckInterval.TotalHours / timeInfo.HoursPerFrame);
            if (lightStateCheckFramesInterval == 0)
            {
                ++lightStateCheckFramesInterval;
            }
        }

        /// <summary>Notifies this simulation object that a new simulation frame is started.
        /// The buildings will be processed again from the beginning of the list.</summary>
        /// <param name="frameIndex">The simulation frame index to process.</param>
        public void ProcessFrame(uint frameIndex)
        {
            UpdateReachingTroubles(frameIndex & StepMask);
            UpdateLightState();

            if ((frameIndex & StepMask) != 0)
            {
                return;
            }

            int currentMinute = timeInfo.Now.Minute;
            if (lastProcessedMinute != currentMinute)
            {
                lastProcessedMinute = currentMinute;
                freezeProblemTimers = false;
            }
            else
            {
                freezeProblemTimers = true;
            }
        }

        /// <summary>
        /// Determines whether the lights should be switched off in the specified building.
        /// </summary>
        /// <param name="buildingId">The ID of the building to check.</param>
        /// <returns>
        ///   <c>true</c> if the lights should be switched off in the specified building; otherwise, <c>false</c>.
        /// </returns>
        public bool ShouldSwitchBuildingLightsOff(ushort buildingId)
        {
            if(config != null && config.SwitchOffLightsAtNight)
            {
                if(lightStates != null && lightStates.Length > 0)
                {
                    return !lightStates[buildingId];
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether the building with the specified ID is an entertainment target.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building is an entertainment target; otherwise, <c>false</c>.
        /// </returns>
        public bool IsEntertainmentTarget(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return true;
            }

            // A building still can post outgoing offers while inactive.
            // This is to prevent those offers from being dispatched.
            if (!buildingManager.BuildingHasFlags(buildingId, Building.Flags.Active))
            {
                return false;
            }

            // ignore closed buildings
            if (!IsBuildingWorking(buildingId))
            {
                return false;
            }

            var buildingService = buildingManager.GetBuildingService(buildingId);
            if (buildingService == ItemClass.Service.VarsitySports)
            {
                // Do not visit varsity sport arenas for entertainment when no active events
                return false;
            }
            else if (buildingService == ItemClass.Service.Monument)
            {
                return buildingManager.IsRealUniqueBuilding(buildingId);
            }

            string className = buildingManager.GetBuildingClassName(buildingId);
            if (string.IsNullOrEmpty(className))
            {
                return true;
            }

            for (int i = 0; i < CarParkingBuildings.Length; ++i)
            {
                if (className.IndexOf(CarParkingBuildings[i], 0, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the building with the specified ID is a shopping target.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building is a shopping target; otherwise, <c>false</c>.
        /// </returns>
        public bool IsShoppingTarget(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return true;
            }

            // A building still can post outgoing offers while inactive.
            // This is to prevent those offers from being dispatched.
            if (!buildingManager.BuildingHasFlags(buildingId, Building.Flags.Active))
            {
                return false;
            }

            // ignore closed buildings
            if (!IsBuildingWorking(buildingId))
            {
                return false;
            }

            var buildingService = buildingManager.GetBuildingService(buildingId);
            if (buildingService == ItemClass.Service.VarsitySports)
            {
                return false;
            }
            else if (buildingService == ItemClass.Service.Monument)
            {
                return buildingManager.IsRealUniqueBuilding(buildingId);
            }
            
            return true;
        }

        /// <summary>
        /// Determines whether the building with the specified ID is allowed to accept garbage services in this time of day.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building is allowed to accept garbage services in this time of day; otherwise, <c>false</c>.
        /// </returns>
        public bool IsGarbageHours(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return true;
            }

            // ignore garbage facilities
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            if (building.Info.GetAI() is LandfillSiteAI)
            {
                return true;
            }

            // A building still can post outgoing offers while inactive.
            // This is to prevent those offers from being dispatched.
            if (!buildingManager.BuildingHasFlags(buildingId, Building.Flags.Active))
            {
                return false;
            }

            float currentHour = timeInfo.CurrentHour;

            switch (buildingManager.GetBuildingService(buildingId))
            {
                case ItemClass.Service.Residential:
                    if (config.GarbageResidentialStartHour == config.GarbageResidentialEndHour)
                    {
                        return true;
                    }
                    if(config.GarbageResidentialStartHour < config.GarbageResidentialEndHour)
                    {
                        if(currentHour >= config.GarbageResidentialStartHour && currentHour <= config.GarbageResidentialEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if(config.GarbageResidentialStartHour <= currentHour || currentHour <= config.GarbageResidentialEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case ItemClass.Service.Commercial:
                    if (config.GarbageCommercialStartHour == config.GarbageCommercialEndHour)
                    {
                        return true;
                    }
                    if (config.GarbageCommercialStartHour < config.GarbageCommercialEndHour)
                    {
                        if (currentHour >= config.GarbageCommercialStartHour && currentHour <= config.GarbageCommercialEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.GarbageCommercialStartHour <= currentHour || currentHour <= config.GarbageCommercialEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case ItemClass.Service.Industrial:
                case ItemClass.Service.PlayerIndustry:
                    if (config.GarbageIndustrialStartHour == config.GarbageIndustrialEndHour)
                    {
                        return true;
                    }
                    if (config.GarbageIndustrialStartHour < config.GarbageIndustrialEndHour)
                    {
                        if (currentHour >= config.GarbageIndustrialStartHour && currentHour <= config.GarbageIndustrialEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.GarbageIndustrialStartHour <= currentHour || currentHour <= config.GarbageIndustrialEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case ItemClass.Service.Office:
                    if (config.GarbageOfficeStartHour == config.GarbageOfficeEndHour)
                    {
                        return true;
                    }
                    if (config.GarbageOfficeStartHour < config.GarbageOfficeEndHour)
                    {
                        if (currentHour >= config.GarbageOfficeStartHour && currentHour <= config.GarbageOfficeEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.GarbageOfficeStartHour <= currentHour || currentHour <= config.GarbageOfficeEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                default:
                    if (config.GarbageOtherStartHour == config.GarbageOtherEndHour)
                    {
                        return true;
                    }
                    if (config.GarbageOtherStartHour < config.GarbageOtherEndHour)
                    {
                        if (currentHour >= config.GarbageOtherStartHour && currentHour <= config.GarbageOtherEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.GarbageOtherStartHour <= currentHour || currentHour <= config.GarbageOtherEndHour)
                        {
                            return true;
                        }
                    }
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the building with the specified ID is allowed to accept mail services in this time of day.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building is allowed to accept mail services in this time of day; otherwise, <c>false</c>.
        /// </returns>
        public bool IsMailHours(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return true;
            }

            // A building still can post outgoing offers while inactive.
            // This is to prevent those offers from being dispatched.
            if (!buildingManager.BuildingHasFlags(buildingId, Building.Flags.Active))
            {
                return false;
            }

            // ignore post sorting facility 
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            if(building.Info.GetAI() is PostOfficeAI postOfficeAI)
            {
                if(postOfficeAI.m_postVanCount == 0 && postOfficeAI.m_postTruckCount > 0)
                {
                    return true;
                }
            }

            float currentHour = timeInfo.CurrentHour;

            switch (buildingManager.GetBuildingService(buildingId))
            {
                case ItemClass.Service.Residential:
                    if (config.MailResidentialStartHour == config.MailResidentialEndHour)
                    {
                        return true;
                    }
                    if (config.MailResidentialStartHour < config.MailResidentialEndHour)
                    {
                        if (currentHour >= config.MailResidentialStartHour && currentHour <= config.MailResidentialEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MailResidentialStartHour <= currentHour || currentHour <= config.MailResidentialEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case ItemClass.Service.Commercial:
                    if (config.MailCommercialStartHour == config.MailCommercialEndHour)
                    {
                        return true;
                    }
                    if (config.MailCommercialStartHour < config.MailCommercialEndHour)
                    {
                        if (currentHour >= config.MailCommercialStartHour && currentHour <= config.MailCommercialEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MailCommercialStartHour <= currentHour || currentHour <= config.MailCommercialEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case ItemClass.Service.Industrial:
                case ItemClass.Service.PlayerIndustry:
                    if (config.MailIndustrialStartHour == config.MailIndustrialEndHour)
                    {
                        return true;
                    }
                    if (config.MailIndustrialStartHour < config.MailIndustrialEndHour)
                    {
                        if (currentHour >= config.MailIndustrialStartHour && currentHour <= config.MailIndustrialEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MailIndustrialStartHour <= currentHour || currentHour <= config.MailIndustrialEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case ItemClass.Service.Office:
                    if (config.MailOfficeStartHour == config.MailOfficeEndHour)
                    {
                        return true;
                    }
                    if (config.MailOfficeStartHour < config.MailOfficeEndHour)
                    {
                        if (currentHour >= config.MailOfficeStartHour && currentHour <= config.MailOfficeEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MailOfficeStartHour <= currentHour || currentHour <= config.MailOfficeEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                default:
                    if (config.MailOtherStartHour == config.MailOtherEndHour)
                    {
                        return true;
                    }
                    if (config.MailOtherStartHour < config.MailOtherEndHour)
                    {
                        if (currentHour >= config.MailOtherStartHour && currentHour <= config.MailOtherEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MailOtherStartHour <= currentHour || currentHour <= config.MailOtherEndHour)
                        {
                            return true;
                        }
                    }
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the park with the specified ID is allowed to accept park maintenance services in this time of day.
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the park is allowed to accept park maintenance services in this time of day; otherwise, <c>false</c>.
        /// </returns>
        public bool IsParkMaintenanceHours(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return true;
            }

            // A building still can post outgoing offers while inactive.
            // This is to prevent those offers from being dispatched.
            if (!buildingManager.BuildingHasFlags(buildingId, Building.Flags.Active))
            {
                return false;
            }

            float currentHour = timeInfo.CurrentHour;

            switch (buildingManager.GetBuildingService(buildingId))
            {
                case ItemClass.Service.Beautification:
                default:
                    if (config.ParkMaintenanceStartHour == config.ParkMaintenanceEndHour)
                    {
                        return true;
                    }
                    if (config.ParkMaintenanceStartHour < config.ParkMaintenanceEndHour)
                    {
                        if (currentHour >= config.ParkMaintenanceStartHour && currentHour <= config.ParkMaintenanceEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.ParkMaintenanceStartHour <= currentHour || currentHour <= config.ParkMaintenanceEndHour)
                        {
                            return true;
                        }
                    }
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the segment with the specified ID is allowed to accept maintenance and snow services in this time of day.
        /// </summary>
        /// <param name="segmentId">The segment ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the segment is allowed to accept maintenance and snow services in this time of day; otherwise, <c>false</c>.
        /// </returns>
        public bool IsMaintenanceSnowRoadServiceHours(ushort segmentId)
        {
            if (segmentId == 0)
            {
                return true;
            }

            float currentHour = timeInfo.CurrentHour;

            var road_info = Singleton<NetManager>.instance.m_segments.m_buffer[segmentId].Info;

            switch (road_info.category)
            {
                case "RoadsSmall":
                    if (config.MaintenanceSnowRoadsSmallStartHour == config.MaintenanceSnowRoadsSmallEndHour)
                    {
                        return true;
                    }
                    if (config.MaintenanceSnowRoadsSmallStartHour < config.MaintenanceSnowRoadsSmallEndHour)
                    {
                        if (currentHour >= config.MaintenanceSnowRoadsSmallStartHour && currentHour <= config.MaintenanceSnowRoadsSmallEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MaintenanceSnowRoadsSmallStartHour <= currentHour || currentHour <= config.MaintenanceSnowRoadsSmallEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case "RoadsMedium":
                    if (config.MaintenanceSnowRoadsMediumStartHour == config.MaintenanceSnowRoadsMediumEndHour)
                    {
                        return true;
                    }
                    if (config.MaintenanceSnowRoadsMediumStartHour < config.MaintenanceSnowRoadsMediumEndHour)
                    {
                        if (currentHour >= config.MaintenanceSnowRoadsMediumStartHour && currentHour <= config.MaintenanceSnowRoadsMediumEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MaintenanceSnowRoadsMediumStartHour <= currentHour || currentHour <= config.MaintenanceSnowRoadsMediumEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case "RoadsLarge":
                    if (config.MaintenanceSnowRoadsLargeStartHour == config.MaintenanceSnowRoadsLargeEndHour)
                    {
                        return true;
                    }
                    if (config.MaintenanceSnowRoadsLargeStartHour < config.MaintenanceSnowRoadsLargeEndHour)
                    {
                        if (currentHour >= config.MaintenanceSnowRoadsLargeStartHour && currentHour <= config.MaintenanceSnowRoadsLargeEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MaintenanceSnowRoadsLargeStartHour <= currentHour || currentHour <= config.MaintenanceSnowRoadsLargeEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                case "RoadsHighway":
                    if (config.MaintenanceSnowRoadsHighwayStartHour == config.MaintenanceSnowRoadsHighwayEndHour)
                    {
                        return true;
                    }
                    if (config.MaintenanceSnowRoadsHighwayStartHour < config.MaintenanceSnowRoadsHighwayEndHour)
                    {
                        if (currentHour >= config.MaintenanceSnowRoadsHighwayStartHour && currentHour <= config.MaintenanceSnowRoadsHighwayEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MaintenanceSnowRoadsHighwayStartHour <= currentHour || currentHour <= config.MaintenanceSnowRoadsHighwayEndHour)
                        {
                            return true;
                        }
                    }
                    return false;

                default:
                    if (config.MaintenanceSnowRoadsOtherStartHour == config.MaintenanceSnowRoadsOtherEndHour)
                    {
                        return true;
                    }
                    if (config.MaintenanceSnowRoadsOtherStartHour < config.MaintenanceSnowRoadsOtherEndHour)
                    {
                        if (currentHour >= config.MaintenanceSnowRoadsOtherStartHour && currentHour <= config.MaintenanceSnowRoadsOtherEndHour)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (config.MaintenanceSnowRoadsOtherStartHour <= currentHour || currentHour <= config.MaintenanceSnowRoadsOtherEndHour)
                        {
                            return true;
                        }
                    }
                    return false;
            }
        }

        /// <summary>Determines whether a building with specified ID is currently active.</summary>
        /// <param name="buildingId">The ID of the building to check.</param>
        /// <returns>
        ///   <c>true</c> if the building with specified ID is currently active; otherwise, <c>false</c>.
        /// </returns>
        public bool IsBuildingActive(ushort buildingId) => buildingManager.BuildingHasFlags(buildingId, Building.Flags.Active);

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
        public bool IsNoiseRestricted(ushort buildingId, ushort currentBuildingId = 0)
        {
            float currentHour = timeInfo.CurrentHour;
            if (currentHour >= config.GoToSleepHour || currentHour <= config.WakeUpHour)
            {
                return BuildingManagerConnection.IsBuildingNoiseRestricted(buildingId);
            }

            if (currentBuildingId == 0)
            {
                return false;
            }

            float travelTime = travelBehavior.GetEstimatedTravelTime(currentBuildingId, buildingId);
            if (travelTime == 0)
            {
                return false;
            }

            float arriveHour = (float)timeInfo.Now.AddHours(travelTime).TimeOfDay.TotalHours;
            if (arriveHour >= config.GoToSleepHour || arriveHour <= config.WakeUpHour)
            {
                return BuildingManagerConnection.IsBuildingNoiseRestricted(buildingId);
            }

            return false;
        }

        /// <summary>Registers a trouble reaching the building with the specified ID.</summary>
        /// <param name="buildingId">The ID of the building where the citizen will not arrive as planned.</param>
        public void RegisterReachingTrouble(ushort buildingId)
        {
            ref byte trouble = ref reachingTroubles[buildingId];
            if (trouble < 255)
            {
                trouble = (byte)Math.Min(255, trouble + 10);
                buildingManager.UpdateBuildingColors(buildingId);
            }
        }

        /// <summary>Gets the reaching trouble factor for a building with specified ID.</summary>
        /// <param name="buildingId">The ID of the building to get the reaching trouble factor of.</param>
        /// <returns>A value in range 0 to 1 that describes how many troubles have citizens while trying to reach
        /// the building.</returns>
        public float GetBuildingReachingTroubleFactor(ushort buildingId) => reachingTroubles[buildingId] / 255f;

        /// <summary>
        /// Creates a building burning time for the specified <paramref name="buildingId"/>
        /// </summary>
        /// <param name="buildingId">The building ID to created a burning time for.</param>
        public void CreateBuildingFire(ushort buildingID)
        {
            if (!FireBurnTimeManager.BuildingBurnTimeExist(buildingID))
            {
                FireBurnTimeManager.CreateBuildingBurnTime(buildingID, timeInfo);
            }
        }

        /// <summary>
        /// remove burning time from the building with the specified <paramref name="buildingId"/>
        /// <param name="buildingId">The building ID to remove burning time from.</param>
        public void RemoveBuildingFire(ushort buildingID) => FireBurnTimeManager.RemoveBuildingBurnTime(buildingID);

        /// <summary>
        /// Determines whether the building with the specified <paramref name="buildingId"/> has burned
        /// enough time for the fire to be put out
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building with the specified <paramref name="buildingId"/> has been burned
        ///   enough time for the fire to be put out; otherwise, <c>false</c>.
        /// </returns>
        public bool ShouldExtinguishFire(ushort buildingID)
        {
            if (!config.RealisticFires)
            {
                return true;
            }
            if (!FireBurnTimeManager.BuildingBurnTimeExist(buildingID))
            {
                return false;
            }

            var burnTime = FireBurnTimeManager.GetBuildingBurnTime(buildingID);

            if (burnTime.StartDate == timeInfo.Now.Date)
            {
                return timeInfo.CurrentHour >= burnTime.StartTime + burnTime.Duration;
            }
            else if (burnTime.StartDate < timeInfo.Now.Date)
            {
                if (burnTime.StartTime + burnTime.Duration >= 24f)
                {
                    float nextDayTime = burnTime.StartTime + burnTime.Duration - 24f;
                    return timeInfo.CurrentHour >= nextDayTime;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines whether an event is within operation hours
        /// </summary>
        /// <param name="data">The EventData to check.</param>
        /// <returns>
        ///   <c>true</c> if the event with the specified <paramref name="data"/> is currently within operation hours otherwise, <c>false</c>.
        /// </returns>
        public bool IsEventWithinOperationHours(ref EventData data)
        {
            var event_start_time = Singleton<SimulationManager>.instance.FrameToTime(data.m_startFrame);
            var event_end_time = data.StartTime.AddHours(data.Info.m_eventAI.m_eventDuration);
            if (event_start_time.Hour >= config.WorkBegin && event_end_time.Hour <= config.GoToSleepHour)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the building with the specified <paramref name="buildingId"/> is a school building
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> if the building with the specified <paramref name="buildingId"/> is a school building otherwise, <c>false</c>.
        /// </returns>
        public bool IsSchoolBuilding(ushort buildingId)
        {
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            if (building.Info.GetAI() is SchoolAI)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the building with the specified <paramref name="buildingId"/> is going to get closed in two hours or less
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>
        ///   <c>true</c> f the building with the specified <paramref name="buildingId"/> is going to get closed in two hours or less, <c>false</c>.
        /// </returns>
        public bool IsBuildingClosingSoon(ushort buildingId)
        {
            var workTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingId);
            if(!IsBuildingWorking(buildingId))
            {
                return false;
            }
            if(workTime.WorkAtNight)
            {
                return false;
            }
            if (workTime.HasContinuousWorkShift)
            {
                if (workTime.WorkShifts == 2)
                {
                    return false;
                }
                if (workTime.WorkShifts == 1 && timeInfo.CurrentHour < 18f)
                {
                    return false;
                }

                return true;
            }
            else
            {
                var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
                if (building.Info.m_class.m_service == ItemClass.Service.Education || building.Info.m_class.m_service == ItemClass.Service.PlayerEducation)
                {
                    if (workTime.WorkShifts == 1 && timeInfo.CurrentHour < (config.SchoolEnd - 2f))
                    {
                        return false;
                    }
                    if (workTime.WorkShifts == 2 && timeInfo.CurrentHour < 20f)
                    {
                        return false;
                    }
                    return true;
                }
                else
                {
                    if (workTime.WorkShifts == 1 && timeInfo.CurrentHour < (config.WorkEnd - 2f))
                    {
                        return false;
                    }
                    if (workTime.WorkShifts == 2 && timeInfo.CurrentHour < (config.GoToSleepHour - 2f))
                    {
                        return false;
                    }
                    return true;
                }
            }
        }

        /// <summary>
        /// Determines whether the building with the specified <paramref name="buildingId"/> is currently working
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <param name="timeBeforeWork">time before work the citizen can arrive without an issue.</param>
        /// <param name="currentBuildingId">the building ID the citizen is currently in.</param>
        /// <returns>
        ///   <c>true</c> if the building with the specified <paramref name="buildingId"/> is currently working otherwise, <c>false</c>.
        /// </returns>
        public bool IsBuildingWorking(ushort buildingId, int timeBeforeWork = 0, ushort currentBuildingId = 0)
        {
            if (buildingId == 0)
            {
                return true;
            }

            var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            BuildingWorkTimeManager.WorkTime workTime;

            if (!BuildingWorkTimeManager.ShouldHaveBuildingWorkTime(buildingId))
            {
                return true;
            }

            if((building.m_flags & Building.Flags.Abandoned) != 0 || (building.m_flags & Building.Flags.Collapsed) != 0)
            {
                return true;
            }

            if (!BuildingWorkTimeManager.BuildingWorkTimeExist(buildingId))
            {
                workTime = BuildingWorkTimeManager.CreateBuildingWorkTime(buildingId, building.Info);
            }
            else
            {
                workTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingId);
            }

            if(building.Info.m_class.m_subService == ItemClass.SubService.CommercialLeisure && !workTime.IgnorePolicy && workTime.IsDefault)
            {
                bool isNoiseRestricted = IsNoiseRestricted(buildingId, currentBuildingId);
                bool updated = false;
                if (isNoiseRestricted)
                {
                    if (workTime.HasContinuousWorkShift)
                    {
                        if (workTime.WorkShifts == 2)
                        {
                            workTime.WorkShifts = 1;
                            workTime.WorkAtNight = false;
                            updated = true;
                        }
                    }
                    else
                    {
                        if (workTime.WorkShifts == 3)
                        {
                            workTime.WorkShifts = 2;
                            workTime.WorkAtNight = false;
                            updated = true;
                        }
                    }
                }
                else
                {
                    if (workTime.HasContinuousWorkShift)
                    {
                        if (workTime.WorkShifts == 1)
                        {
                            workTime.WorkShifts = 2;
                            workTime.WorkAtNight = true;
                            updated = true;
                        }
                    }
                    else
                    {
                        if (workTime.WorkShifts == 2)
                        {
                            workTime.WorkShifts = 3;
                            workTime.WorkAtNight = true;
                            updated = true;
                        }
                    }
                }
                if (updated)
                {
                    BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                }
            }

            if ((building.Info.m_class.m_subService == ItemClass.SubService.PlayerIndustryFarming
                || building.Info.m_class.m_subService == ItemClass.SubService.PlayerIndustryForestry) && !workTime.IgnorePolicy && workTime.IsDefault)
            {
                bool IsEssential = BuildingManagerConnection.IsEssentialIndustryBuilding(buildingId);
                bool updated = false;
                if (IsEssential && !workTime.WorkAtNight)
                {
                    workTime.WorkShifts = 3;
                    workTime.WorkAtNight = true;
                    updated = true;
                }
                else if (!IsEssential && workTime.WorkAtNight)
                {
                    workTime.WorkShifts = 2;
                    workTime.WorkAtNight = false;
                    updated = true;
                }
                if (updated)
                {
                    BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                }
            }

            if (building.Info.m_class.m_service == ItemClass.Service.Beautification &&
                building.Info.m_class.m_subService == ItemClass.SubService.BeautificationParks
                && !workTime.IgnorePolicy && workTime.IsDefault)
            {
                var position = BuildingManager.instance.m_buildings.m_buffer[buildingId].m_position;
                byte parkId = DistrictManager.instance.GetPark(position);
                bool need_update = false;
                if (parkId != 0)
                {
                    var park = DistrictManager.instance.m_parks.m_buffer[parkId];
                    if ((park.m_parkPolicies & DistrictPolicies.Park.NightTours) != 0)
                    {
                        if (workTime.WorkShifts != 3)
                        {
                            workTime.WorkShifts = 3;
                            workTime.WorkAtNight = true;
                            workTime.WorkAtWeekands = true;
                            workTime.HasExtendedWorkShift = true;
                            workTime.HasContinuousWorkShift = false;
                            workTime.IsDefault = true;
                            need_update = true;
                        }
                    }
                    else
                    {
                        if (workTime.WorkShifts != 2)
                        {
                            workTime.WorkShifts = 2;
                            workTime.WorkAtNight = false;
                            workTime.WorkAtWeekands = true;
                            workTime.HasExtendedWorkShift = true;
                            workTime.HasContinuousWorkShift = false;
                            workTime.IsDefault = true;
                            need_update = true;
                        }
                    }
                }
                if (need_update)
                {
                    BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                }
            }

            // WorkForceMatters setting is enabled and no one at work - building will not work
            if (config.WorkForceMatters && GetWorkersInBuilding(buildingId) == 0)
            {
                return false;
            }

            float currentHour = timeInfo.CurrentHour;
            if (workTime.HasExtendedWorkShift)
            {
                float extendedShiftBegin = Math.Min(config.SchoolBegin, config.WakeUpHour);

                if (building.Info.m_class.m_service == ItemClass.Service.Education || building.Info.m_class.m_service == ItemClass.Service.PlayerEducation)
                {
                    if (config.IsWeekendEnabled && timeInfo.Now.IsWeekend() && !workTime.WorkAtWeekands)
                    {
                        return false;
                    }

                    if (timeInfo.IsNightTime && !workTime.WorkAtNight)
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
                    if (config.IsWeekendEnabled && timeInfo.Now.IsWeekend() && !workTime.WorkAtWeekands)
                    {
                        return false;
                    }

                    if (timeInfo.IsNightTime && !workTime.WorkAtNight)
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
                        return currentHour >= startHour - timeBeforeWork && currentHour < config.GoToSleepHour;
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
                if (config.IsWeekendEnabled && timeInfo.Now.IsWeekend() && !workTime.WorkAtWeekands)
                {
                    return false;
                }

                if (timeInfo.IsNightTime && !workTime.WorkAtNight)
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
                if (config.IsWeekendEnabled && timeInfo.Now.IsWeekend() && !workTime.WorkAtWeekands)
                {
                    return false;
                }

                if (timeInfo.IsNightTime && !workTime.WorkAtNight)
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
        /// Get the number of workers currently working in the specified <paramref name="buildingId"/>
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>the number of workers in the specified building</returns>
        public int GetWorkersInBuilding(ushort buildingId) => buildingManager.GetWorkersInBuilding(buildingId);

        /// <summary>
        /// Get an array of workers that belong to specified <paramref name="buildingId"/>
        /// </summary>
        /// <param name="buildingId">The building ID to check.</param>
        /// <returns>an array of workers that belong to the specified building</returns>
        public uint[] GetBuildingWorkForce(ushort buildingId) => buildingManager.GetBuildingWorkForce(buildingId);

        /// <summary>Check if the building has units of a specific type</summary>
        /// <param name="buildingID">The ID of the building to check units for.</param>
        /// <param name="flag">The flag type to check units of this type exist.</param>
        /// <returns>
        ///   <c>true</c> if the specified <paramref name="buildingID"/> have those units available; otherwise, <c>false</c>.
        /// </returns>
        public bool HaveUnits(ushort buildingID, CitizenUnit.Flags flag)
        {
            var instance = Singleton<CitizenManager>.instance;
            var instance1 = Singleton<BuildingManager>.instance;
            uint units = instance1.m_buildings.m_buffer[buildingID].m_citizenUnits;
            int num = 0;
            while (units != 0)
            {
                uint nextUnit = instance.m_units.m_buffer[units].m_nextUnit;
                if ((instance.m_units.m_buffer[units].m_flags & flag) != 0)
                {
                    return true;
                }
                units = nextUnit;
                if (++num > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
            return false;
        }

        /// <summary>Finds an active building that matches the specified criteria and can accept visitors.</summary>
        /// <param name="searchAreaCenterBuilding">The building ID that represents the search area center point.</param>
        /// <param name="maxDistance">The maximum distance for search, the search area radius.</param>
        /// <param name="service">The building service type to find.</param>
        /// <param name="subService">The building sub-service type to find.</param>
        /// <param name="isShopping">The building sub-service includes leisure if true.</param>
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
            return FindActiveBuilding(currentPosition, maxDistance, service, subService, isShopping, searchAreaCenterBuilding);
        }

        /// <summary>Finds an active building that matches the specified criteria and can accept visitors.</summary>
        /// <param name="position">The search area center point.</param>
        /// <param name="maxDistance">The maximum distance for search, the search area radius.</param>
        /// <param name="service">The building service type to find.</param>
        /// <param name="subService">The building sub-service type to find.</param>
        /// <param name="isShopping">The building sub-service includes leisure if true.</param>
        /// <param name="currentBuilding">The current building the citizen is in.</param>
        /// <returns>An ID of the first found building, or 0 if none found.</returns>
        public ushort FindActiveBuilding(
            Vector3 position,
            float maxDistance,
            ItemClass.Service service,
            ItemClass.SubService subService = ItemClass.SubService.None,
            bool isShopping = true,
            ushort currentBuilding = 0)
        {
            if (position == Vector3.zero)
            {
                return 0;
            }

            const Building.Flags restrictedFlags = Building.Flags.Deleted | Building.Flags.Evacuating | Building.Flags.Flooded | Building.Flags.Collapsed
                | Building.Flags.BurnedDown | Building.Flags.RoadAccessFailed;

            const Building.Flags requiredFlags = Building.Flags.Created | Building.Flags.Completed | Building.Flags.Active;
            const Building.Flags combinedFlags = requiredFlags | restrictedFlags;

            float searchBuffer = 0.5f;
            int gridXFrom = Mathf.Max((int)((position.x - maxDistance - searchBuffer) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), 0);
            int gridZFrom = Mathf.Max((int)((position.z - maxDistance - searchBuffer) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), 0);
            int gridXTo = Mathf.Min((int)((position.x + maxDistance + searchBuffer) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), MaxBuildingGridIndex);
            int gridZTo = Mathf.Min((int)((position.z + maxDistance + searchBuffer) / BuildingManager.BUILDINGGRID_CELL_SIZE + BuildingGridMiddle), MaxBuildingGridIndex);

            float sqrMaxDistance = maxDistance * maxDistance;
            var manager = BuildingManager.instance;

            for (int z = gridZFrom; z <= gridZTo; ++z)
            {
                for (int x = gridXFrom; x <= gridXTo; ++x)
                {
                    ushort buildingId = manager.m_buildingGrid[z * BuildingManager.BUILDINGGRID_RESOLUTION + x];
                    uint safetyCounter = 0;

                    while (buildingId != 0)
                    {
                        ref var building = ref BuildingManager.instance.m_buildings.m_buffer[buildingId];
                        var info = building.Info;

                        if (info?.m_class != null && (building.m_flags & combinedFlags) == requiredFlags)
                        {
                            var buildingService = building.Info.m_class.m_service;
                            var buildingSubService = building.Info.m_class.m_subService;

                            if (buildingService == service && (subService == ItemClass.SubService.None || buildingSubService == subService))
                            {
                                // Check shopping/leisure restriction
                                bool notAllowed = !isShopping && buildingService == ItemClass.Service.Commercial && buildingSubService == ItemClass.SubService.CommercialLeisure;

                                if (!notAllowed)
                                {
                                    if (IsBuildingWorking(buildingId, 0, currentBuilding))
                                    {
                                        float sqrDistance = Vector3.SqrMagnitude(position - building.m_position);
                                        if (sqrDistance < sqrMaxDistance)
                                        {
                                            if (BuildingManagerConnection.BuildingCanBeVisited(buildingId))
                                            {
                                                return buildingId;
                                            }
                                            else
                                            {
                                                Log.Debug(LogCategory.Advanced, timeInfo.Now, $"Building {buildingId} rejected: Full capacity.");
                                            }
                                        }
                                        else
                                        {
                                            Log.Debug(LogCategory.Advanced, timeInfo.Now, $"Building {buildingId} rejected: Too far ({Mathf.Sqrt(sqrDistance)}m).");
                                        }
                                    }
                                    else
                                    {
                                        Log.Debug(LogCategory.Advanced, timeInfo.Now, $"Building {buildingId} rejected: Not working.");
                                    }
                                }
                                else
                                {
                                    Log.Debug(LogCategory.Advanced, timeInfo.Now, $"Building {buildingId} rejected: Leisure restriction.");
                                }
                            }
                        }

                        buildingId = building.m_nextGridBuilding;
                        if (++safetyCounter >= BuildingManager.MAX_BUILDING_COUNT)
                        {
                            break;
                        }
                    }
                }
            }

            return 0;
        }

        /// <summary>Finds an active cafeteria building that matches the specified criteria.</summary>
        /// <param name="searchAreaCenterBuilding">The building ID that represents the search area center point.</param>
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
                            && BuildingManagerConnection.CheckSameCampusArea(searchAreaCenterBuilding, buildingId)
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

        private static int GetAllowedConstructingUpradingCount(int currentBuildingCount)
        {
            if (currentBuildingCount < ConstructionRestrictionThreshold1)
            {
                return ConstructionRestrictionStep1;
            }

            if (currentBuildingCount < ConstructionRestrictionThreshold2)
            {
                return ConstructionRestrictionStep1 + currentBuildingCount / ConstructionRestrictionScale2;
            }

            if (currentBuildingCount < ConstructionRestrictionThreshold3)
            {
                return ConstructionRestrictionStep2 + currentBuildingCount / ConstructionRestrictionScale3;
            }

            return MaximumBuildingsInConstruction;
        }

        private bool IsBuildingCompletedOrMissing(ushort buildingId) => buildingManager.BuildingHasFlags(buildingId, Building.Flags.Completed | Building.Flags.Deleted, includeZero: true);

        private void UpdateLightState()
        {
            if (lightStateCheckCounter > 0)
            {
                --lightStateCheckCounter;
                return;
            }

            ushort step = lightCheckStep;
            lightCheckStep = (ushort)((step + 1) & StepMask);
            lightStateCheckCounter = lightStateCheckFramesInterval;

            UpdateLightState(step, updateBuilding: true);
        }

        private void UpdateReachingTroubles(uint step)
        {
            ushort first = (ushort)(step * BuildingStepSize);
            ushort last = (ushort)((step + 1) * BuildingStepSize - 1);

            for (ushort i = first; i <= last; ++i)
            {
                ref byte trouble = ref reachingTroubles[i];
                if (trouble > 0)
                {
                    --trouble;
                    buildingManager.UpdateBuildingColors(i);
                }
            }
        }

        private void UpdateLightState(ushort step, bool updateBuilding)
        {
            ushort first = (ushort)(step * BuildingStepSize);
            ushort last = (ushort)((step + 1) * BuildingStepSize - 1);

            for (ushort i = first; i <= last; ++i)
            {
                if (!buildingManager.BuildingHasFlags(i, Building.Flags.Created))
                {
                    continue;
                }

                buildingManager.GetBuildingService(i, out var service, out var subService);
                bool lightsOn = !ShouldSwitchBuildingLightsOff(i, service, subService);
                if (lightsOn == lightStates[i])
                {
                    continue;
                }

                lightStates[i] = lightsOn;
                if (updateBuilding)
                {
                    buildingManager.UpdateBuildingColors(i);
                    if (!lightsOn && service != ItemClass.Service.Residential)
                    {
                        buildingManager.DeactivateVisually(i);
                    }
                }
            }
        }

        private bool ShouldSwitchBuildingLightsOff(ushort buildingId, ItemClass.Service service, ItemClass.SubService subService)
        {
            switch (service)
            {
                case ItemClass.Service.None:
                    return false;

                case ItemClass.Service.Residential:
                case ItemClass.Service.HealthCare when BuildingManagerConnection.IsCimCareBuilding(buildingId):
                    if (buildingManager.GetBuildingHeight(buildingId) > config.SwitchOffLightsMaxHeight)
                    {
                        return false;
                    }
                    float currentHour = timeInfo.CurrentHour;
                    return currentHour < Math.Min(config.WakeUpHour, EarliestWakeUp) || currentHour >= config.GoToSleepHour;

                case ItemClass.Service.Office:
                case ItemClass.Service.Commercial:
                case ItemClass.Service.Monument:
                    if (buildingManager.GetBuildingHeight(buildingId) > config.SwitchOffLightsMaxHeight)
                    {
                        return false;
                    }

                    goto default;

                case ItemClass.Service.ServicePoint:
                    return false;

                default:
                    return !IsBuildingWorking(buildingId);
            }
        }

        /// <summary>
        /// Determines whether a commerical building will receive goods delivery once a week or not
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the building will receive goods delivery once a week;
        ///   otherwise, <c>false</c>.
        /// </returns>
        public bool WeeklyCommericalDeliveries() => config.WeeklyCommericalDeliveries;


    }
}
