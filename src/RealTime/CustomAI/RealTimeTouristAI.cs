// RealTimeTouristAI.cs

namespace RealTime.CustomAI
{
    using System;
    using ColossalFramework;
    using ColossalFramework.Math;
    using RealTime.Config;
    using RealTime.Events;
    using RealTime.GameConnection;
    using RealTime.Managers;
    using SkyTools.Tools;
    using static Constants;

    /// <summary>
    /// A class incorporating the custom logic for the tourists that visit the city.
    /// </summary>
    /// <typeparam name="TAI">The type of the tourist AI.</typeparam>
    /// <typeparam name="TCitizen">The type of the citizen objects.</typeparam>
    /// <seealso cref="RealTimeHumanAIBase{TCitizen}" />
    /// <remarks>
    /// Initializes a new instance of the <see cref="RealTimeTouristAI{TAI, TCitizen}"/> class.
    /// </remarks>
    ///
    /// <param name="config">The configuration to run with.</param>
    /// <param name="connections">A <see cref="GameConnections{T}"/> instance that provides the game connection implementation.</param>
    /// <param name="touristAI">A connection to game's tourist AI.</param>
    /// <param name="eventManager">The custom event manager.</param>
    /// <param name="spareTimeBehavior">A behavior that provides simulation info for the citizens spare time.</param>
    ///
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    internal sealed class RealTimeTouristAI<TAI, TCitizen>(
        RealTimeConfig config,
        GameConnections<TCitizen> connections,
        TouristAIConnection<TAI, TCitizen> touristAI,
        IRealTimeEventManager eventManager,
        ISpareTimeBehavior spareTimeBehavior,
        IRealTimeBuildingAI buildingAI) : RealTimeHumanAIBase<TCitizen>(config, connections, eventManager)
        where TAI : class
        where TCitizen : struct
    {
        private readonly TouristAIConnection<TAI, TCitizen> touristAI = touristAI ?? throw new ArgumentNullException(nameof(touristAI));
        private readonly ISpareTimeBehavior spareTimeBehavior = spareTimeBehavior ?? throw new ArgumentNullException(nameof(spareTimeBehavior));
        private readonly IRealTimeBuildingAI buildingAI = buildingAI ?? throw new ArgumentNullException(nameof(buildingAI));

        private enum TouristTarget
        {
            DoNothing,
            LeaveCity,
            Shopping,
            Relaxing,
            Party,
            Hotel,
            BusinessAppointment,
            VisitNature,
            Food
        }

        /// <summary>
        /// The entry method of the custom AI.
        /// </summary>
        ///
        /// <param name="instance">A reference to an object instance of the original AI.</param>
        /// <param name="citizenId">The ID of the citizen to process.</param>
        /// <param name="citizen">A <typeparamref name="TCitizen"/> reference to process.</param>
        public void UpdateLocation(TAI instance, uint citizenId, ref TCitizen citizen)
        {
            if (!EnsureCitizenCanBeProcessed(citizenId, ref citizen))
            {
                return;
            }

            if (CitizenProxy.IsDead(ref citizen) || CitizenProxy.IsSick(ref citizen))
            {
                CitizenMgr.ReleaseCitizen(citizenId);
                return;
            }

            switch (CitizenProxy.GetLocation(ref citizen))
            {
                case Citizen.Location.Home:
                case Citizen.Location.Work:
                    CitizenMgr.ReleaseCitizen(citizenId);
                    break;

                case Citizen.Location.Visit:
                    ProcessVisit(instance, citizenId, ref citizen);
                    break;

                case Citizen.Location.Hotel:
                    ProcessHotel(instance, citizenId, ref citizen);
                    break;

                case Citizen.Location.Moving:
                    ProcessMoving(instance, citizenId, ref citizen);
                    break;
            }
        }

        private void ProcessMoving(TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort instanceId = CitizenProxy.GetInstance(ref citizen);
            ushort vehicleId = CitizenProxy.GetVehicle(ref citizen);

            if (instanceId == 0)
            {
                if (vehicleId == 0)
                {
                    CitizenMgr.ReleaseCitizen(citizenId);
                }

                return;
            }

            bool isEvacuating = CitizenProxy.HasFlags(ref citizen, Citizen.Flags.Evacuating);
            if (vehicleId == 0 && !isEvacuating && CitizenMgr.IsAreaEvacuating(instanceId))
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} was on the way, but the area evacuates. Searching for a shelter.");
                TransferMgr.AddOutgoingOfferFromCurrentPosition(citizenId, touristAI.GetEvacuationReason(instance, 0));
                return;
            }

            if (isEvacuating)
            {
                return;
            }

            if (CitizenMgr.InstanceHasFlags(instanceId, CitizenInstance.Flags.TargetIsNode | CitizenInstance.Flags.OnTour, all: true))
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} exits the guided tour.");
                FindRandomVisitPlace(instance, citizenId, ref citizen, TouristDoNothingProbability, 0);
                return;
            }

            ushort targetBuildingId = CitizenProxy.GetVisitBuilding(ref citizen);

            TouristTarget target;
            if (CitizenMgr.InstanceHasFlags(instanceId, CitizenInstance.Flags.TargetIsNode))
            {
                if (CitizenMgr.GetTargetNode(instanceId) != 0)
                {
                    target = TouristTarget.Relaxing;
                }
                else
                {
                    return;
                }
            }
            else
            {
                if (targetBuildingId == 0)
                {
                    targetBuildingId = CitizenMgr.GetTargetBuilding(instanceId);
                }

                if (BuildingManagerConnection.IsHotel(targetBuildingId))
                {
                    return;
                }

                if (!buildingAI.IsBuildingWorking(targetBuildingId))
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} was on the way, but the building got closed. find new building to visit");
                    FindRandomVisitPlace(instance, citizenId, ref citizen, TouristDoNothingProbability, 0);
                    return;
                }

                BuildingMgr.GetBuildingService(targetBuildingId, out var targetService, out var targetSubService);

                switch (targetService)
                {
                    case ItemClass.Service.Tourism:
                    case ItemClass.Service.Monument:
                        target = TouristTarget.Relaxing;
                        break;

                    case ItemClass.Service.Beautification:
                        target = TouristTarget.VisitNature;
                        break;

                    case ItemClass.Service.Office:
                        target = TouristTarget.BusinessAppointment;
                        break;

                    case ItemClass.Service.Commercial:
                        if (!CommercialBuildingTypesManager.CommercialBuildingTypeExist(targetBuildingId))
                        {
                            return;
                        }

                        var type = CommercialBuildingTypesManager.GetCommercialBuildingType(targetBuildingId);
                        if ((type & (CommercialBuildingType.Shopping | CommercialBuildingType.Entertainment | CommercialBuildingType.Food)) == 0)
                        {
                            return;
                        }

                        // Random commercial intent, weighted by time/mood
                        target = PickRandomCommercialIntent(citizenId, ref citizen, TimeInfo.IsNightTime);

                        // Validate building supports this intent
                        if (!BuildingSupportsIntent(target, type))
                        {
                            // Fallback to next-best match or hotel
                            target = ResolveFallbackTargetFromBuilding(targetSubService, type);
                        }
                        break;

                    default:
                        return;
                }
            }

            if (GetTouristGoingOutChance(ref citizen, target) > 0)
            {
                return;
            }

            HotelCheck(instance, citizenId, ref citizen, targetBuildingId);
        }

        private void ProcessVisit(TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort visitBuilding = CitizenProxy.GetVisitBuilding(ref citizen);
            if (visitBuilding == 0)
            {
                CitizenMgr.ReleaseCitizen(citizenId);
                return;
            }

            if (BuildingMgr.BuildingHasFlags(visitBuilding, Building.Flags.Evacuating))
            {
                touristAI.FindEvacuationPlace(instance, citizenId, visitBuilding, touristAI.GetEvacuationReason(instance, visitBuilding));
                return;
            }

            if (!buildingAI.IsBuildingWorking(visitBuilding))
            {
                HotelCheck(instance, citizenId, ref citizen, visitBuilding);
                return;
            }

            switch (BuildingMgr.GetBuildingService(visitBuilding))
            {
                case ItemClass.Service.Disaster:
                    if (BuildingMgr.BuildingHasFlags(visitBuilding, Building.Flags.Downgrading))
                    {
                        CitizenProxy.RemoveFlags(ref citizen, Citizen.Flags.Evacuating);
                        FindRandomVisitPlace(instance, citizenId, ref citizen, 0, visitBuilding);
                    }

                    return;
            }

            var currentEvent = EventMgr.GetCityEvent(visitBuilding);
            if (currentEvent != null && currentEvent.StartTime < TimeInfo.Now)
            {
                if (Random.ShouldOccur(TouristShoppingChance))
                {
                    BuildingMgr.ModifyMaterialBuffer(visitBuilding, TransferManager.TransferReason.Shopping, -ShoppingGoodsAmount);
                }

                return;
            }

            if (Random.ShouldOccur(TouristEventChance) && !WeatherInfo.IsBadWeather)
            {
                var cityEvent = GetEventToAttend(citizenId, ref citizen);
                if (cityEvent != null)
                {
                    ushort eventBuildingId = cityEvent.BuildingId;
                    ushort eventStandId = GetEventStand(eventBuildingId);
                    if(StartMovingToVisitBuilding(instance, citizenId, ref citizen, CitizenProxy.GetCurrentBuilding(ref citizen), eventStandId))
                    {
                        Log.Debug(LogCategory.Events, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} attending an event at {eventStandId}");
                        return;
                    }
                }
            }

            FindRandomVisitPlace(instance, citizenId, ref citizen, 0, visitBuilding);
        }

        private void ProcessHotel(TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort hotelBuilding = CitizenProxy.GetHotelBuilding(ref citizen);
            if (hotelBuilding == 0)
            {
                CitizenMgr.ReleaseCitizen(citizenId);
                return;
            }

            if (BuildingMgr.BuildingHasFlags(hotelBuilding, Building.Flags.Evacuating))
            {
                touristAI.FindEvacuationPlace(instance, citizenId, hotelBuilding, touristAI.GetEvacuationReason(instance, hotelBuilding));
                return;
            }

            if(!Random.ShouldOccur(GetHotelLeaveChance()))
            {
                // Tourist is sleeping in a hotel
                return;
            }

            FindRandomVisitPlace(instance, citizenId, ref citizen, 0, hotelBuilding);
        }

        private void FindRandomVisitPlace(TAI instance, uint citizenId, ref TCitizen citizen, int doNothingProbability, ushort currentBuilding)
        {
            var target = touristAI.GetRandomTargetType(instance, doNothingProbability, ref citizen);
            var tourist_target = ConvertToTouristTarget(target);
            tourist_target = AdjustTargetToTimeAndWeather(ref citizen, tourist_target);

            if (CurrentBuildingSupportsTarget(currentBuilding, tourist_target))
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} stays in building {currentBuilding} for {tourist_target}");
                return;
            }

            switch (tourist_target)
            {
                case TouristTarget.LeaveCity:
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} decides to leave the city");
                    touristAI.FindVisitPlace(instance, citizenId, currentBuilding, touristAI.GetLeavingReason(instance, citizenId, ref citizen));
                    break;

                case TouristTarget.Shopping:
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} stays in the city, goes shopping");
                    touristAI.FindVisitPlace(instance, citizenId, currentBuilding, touristAI.GetShoppingReason(instance));
                    break;

                case TouristTarget.Relaxing:
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} stays in the city, goes relaxing");
                    touristAI.FindVisitPlace(instance, citizenId, currentBuilding, touristAI.GetEntertainmentReason(instance));
                    break;

                case TouristTarget.BusinessAppointment:
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} stays in the city, goes to a business appointment");
                    touristAI.FindVisitPlace(instance, citizenId, currentBuilding, touristAI.GetBusinessReason(instance));
                    break;

                case TouristTarget.VisitNature:
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} stays in the city, goes to enjoy nature");
                    touristAI.FindVisitPlace(instance, citizenId, currentBuilding, touristAI.GetNatureReason(instance));
                    break;

                case TouristTarget.Party:
                    ushort leisureBuilding = buildingAI.FindActiveBuilding(
                        currentBuilding,
                        LeisureSearchDistance,
                        ItemClass.Service.Commercial,
                        ItemClass.SubService.CommercialLeisure,
                        CommercialBuildingType.Entertainment);
                    if (leisureBuilding == 0)
                    {
                        goto case TouristTarget.Hotel;
                    }

                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} want to party in {leisureBuilding}");
                    StartMovingToVisitBuilding(instance, citizenId, ref citizen, currentBuilding, leisureBuilding);
                    break;

                case TouristTarget.Hotel:
                    HotelCheck(instance, citizenId, ref citizen, currentBuilding);
                    break;

                case TouristTarget.Food:
                    ushort mealBuilding = buildingAI.FindActiveBuilding(
                        currentBuilding,
                        LeisureSearchDistance,
                        ItemClass.Service.Commercial,
                        ItemClass.SubService.None,
                        CommercialBuildingType.Food);
                    if (mealBuilding == 0)
                    {
                        goto case TouristTarget.Hotel;
                    }

                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} want to eat in {mealBuilding}");
                    StartMovingToVisitBuilding(instance, citizenId, ref citizen, currentBuilding, mealBuilding);
                    break;
            }
        }

        private TouristTarget ConvertToTouristTarget(TouristAI.Target target)
        {
            switch (target)
            {
                case TouristAI.Target.Shopping:
                    return TouristTarget.Shopping;
                case TouristAI.Target.Entertainment:
                    return TouristTarget.Relaxing;
                case TouristAI.Target.Business:
                    return TouristTarget.BusinessAppointment;
                case TouristAI.Target.Nature:
                    return TouristTarget.VisitNature;
                case TouristAI.Target.Nothing:
                    return TouristTarget.DoNothing;
                case TouristAI.Target.Leaving:
                    return TouristTarget.LeaveCity;
                case TouristAI.Target.Hotel:
                    return TouristTarget.Hotel;
                default:
                    return TouristTarget.DoNothing;
            }
        }

        private TouristTarget AdjustTargetToTimeAndWeather(ref TCitizen citizen, TouristTarget target)
        {
            switch (target)
            {
                case TouristTarget.Relaxing:
                {
                    var age = CitizenProxy.GetAge(ref citizen);

                    if (Random.ShouldOccur(spareTimeBehavior.GetEatingOutChance(age)))
                    {
                        target = TouristTarget.Food;
                    }
                    else if (TimeInfo.IsNightTime)
                    {
                        target = TouristTarget.Party;
                    }

                    break;
                }

                case TouristTarget.Shopping:
                case TouristTarget.BusinessAppointment:
                case TouristTarget.VisitNature:
                case TouristTarget.Party:
                case TouristTarget.Food:
                    break;

                default:
                    return target;
            }

            uint goingOutChance = GetTouristGoingOutChance(ref citizen, target);
            return Random.ShouldOccur(goingOutChance) ? target : TouristTarget.Hotel;
        }

        private uint GetTouristGoingOutChance(ref TCitizen citizen, TouristTarget target)
        {
            var age = CitizenProxy.GetAge(ref citizen);
            switch (target)
            {
                case TouristTarget.Shopping:
                    return spareTimeBehavior.GetShoppingChance(age);

                case TouristTarget.Relaxing when WeatherInfo.IsBadWeather:
                    return 0u;

                case TouristTarget.VisitNature when WeatherInfo.IsBadWeather:
                    return 0u;

                case TouristTarget.Party:
                case TouristTarget.Relaxing:
                case TouristTarget.VisitNature:
                    return spareTimeBehavior.GetRelaxingChance(age);

                case TouristTarget.BusinessAppointment:
                    return spareTimeBehavior.GetBusinessAppointmentChance(age);

                case TouristTarget.Food:
                    return spareTimeBehavior.GetEatingOutChance(age);

                default:
                    return 100u;
            }
        }

        private bool TryTofindHotel(uint citizenId, ref TCitizen citizen)
        {
            ushort hotelBuilding = HotelManager.FindRandomHotel();
            if (hotelBuilding != 0)
            {
                CitizenProxy.SetHotel(ref citizen, citizenId, hotelBuilding, 0);
                return true;
            }
            return false;
        }

        private void HotelCheck(TAI instance, uint citizenId, ref TCitizen citizen, ushort currentBuilding)
        {
            ushort hotelBuilding = CitizenProxy.GetHotelBuilding(ref citizen);
            if (hotelBuilding != 0)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} has an hotel {hotelBuilding} and moves there because of time or weather");
                StartMovingToHotelBuilding(instance, citizenId, ref citizen, 0, hotelBuilding);
            }
            else
            {
                bool findHotel = TryTofindHotel(citizenId, ref citizen);
                if (findHotel)
                {
                    hotelBuilding = CitizenProxy.GetHotelBuilding(ref citizen);
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} found a hotel {hotelBuilding} and moves there because of time or weather");
                    StartMovingToHotelBuilding(instance, citizenId, ref citizen, currentBuilding, hotelBuilding);
                }
                else
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} leaves the city because of time or weather and no hotel was found");
                    touristAI.FindVisitPlace(instance, citizenId, 0, touristAI.GetLeavingReason(instance, citizenId, ref citizen));
                }
            }
        }

        private bool StartMovingToVisitBuilding(TAI instance, uint citizenId, ref TCitizen citizen, ushort currentBuilding, ushort visitBuilding)
        {
            CitizenProxy.SetVisitPlace(ref citizen, citizenId, visitBuilding);
            if (CitizenProxy.GetVisitBuilding(ref citizen) == 0)
            {
                // Building is full and doesn't accept visitors anymore
                return false;
            }

            if (!touristAI.StartMoving(instance, citizenId, ref citizen, currentBuilding, visitBuilding))
            {
                CitizenProxy.SetVisitPlace(ref citizen, citizenId, 0);
                return false;
            }

            return true;
        }

        private void StartMovingToHotelBuilding(TAI instance, uint citizenId, ref TCitizen citizen, ushort currentBuilding, ushort hotelBuilding)
        {
            if (!touristAI.StartMoving(instance, citizenId, ref citizen, currentBuilding, hotelBuilding))
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} is unable to move from building {currentBuilding} to the hotel {hotelBuilding}");
                return;
            }

            Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Tourist {GetCitizenDesc(citizenId, ref citizen)} started moving from building {currentBuilding} to the hotel {hotelBuilding}");
        }

        private uint GetHotelLeaveChance() => TimeInfo.IsNightTime ? 0u : (uint)((TimeInfo.CurrentHour - Config.WakeUpHour) / 0.03f);

        private TouristTarget PickRandomCommercialIntent(uint citizenId, ref TCitizen citizen, bool isNightTime)
        {
            uint seed = citizenId ^ ((uint)TimeInfo.CurrentHour << 8) ^ (isNightTime ? 1u : 0u);
            var age = CitizenProxy.GetAge(ref citizen);
            var rand = new Randomizer(seed);

            // Daytime: Shopping 50%, Food 30%, Entertainment 20%
            // Nighttime: Food 35%, Party 35%, Shopping 30%
            int shoppingWeight = (int)spareTimeBehavior.GetShoppingChance(age);
            int foodWeight = (int)spareTimeBehavior.GetEatingOutChance(age);
            int entWeight = (int)spareTimeBehavior.GetRelaxingChance(age);

            if (isNightTime)
            {
                entWeight = Math.Max(entWeight, foodWeight); // Party gets boosted
            }

            int total = shoppingWeight + foodWeight + entWeight;
            int pick = rand.Int32((uint)total);

            if (pick < shoppingWeight)
            {
                return TouristTarget.Shopping;
            }
            else if (pick < shoppingWeight + foodWeight)
            {
                return TouristTarget.Food;
            }
            else
            {
                return isNightTime ? TouristTarget.Party : TouristTarget.Relaxing;
            }
        }

        private bool BuildingSupportsIntent(TouristTarget intent, CommercialBuildingType type) => intent switch
        {
            TouristTarget.Shopping => type.IsFlagSet(CommercialBuildingType.Shopping),
            TouristTarget.Food => type.IsFlagSet(CommercialBuildingType.Food),
            TouristTarget.Party or TouristTarget.Relaxing => type.IsFlagSet(CommercialBuildingType.Entertainment),
            _ => false
        };

        private TouristTarget ResolveFallbackTargetFromBuilding(ItemClass.SubService subService, CommercialBuildingType type)
        {
            if (subService == ItemClass.SubService.CommercialLeisure && type.IsFlagSet(CommercialBuildingType.Entertainment))
            {
                return TouristTarget.Party;
            }

            if (type.IsFlagSet(CommercialBuildingType.Food))
            {
                return TouristTarget.Food;
            }

            if (type.IsFlagSet(CommercialBuildingType.Shopping))
            {
                return TouristTarget.Shopping;
            }

            if (type.IsFlagSet(CommercialBuildingType.Entertainment))
            {
                return TouristTarget.Relaxing;
            }

            return TouristTarget.DoNothing;
        }

        private bool CurrentBuildingSupportsTarget(ushort buildingId, TouristTarget target)
        {
            if (buildingId == 0 || !CommercialBuildingTypesManager.CommercialBuildingTypeExist(buildingId))
            {
                return false;
            }

            var type = CommercialBuildingTypesManager.GetCommercialBuildingType(buildingId);

            return target switch
            {
                TouristTarget.Shopping => type.IsFlagSet(CommercialBuildingType.Shopping),
                TouristTarget.Food => type.IsFlagSet(CommercialBuildingType.Food),
                TouristTarget.Relaxing => type.IsFlagSet(CommercialBuildingType.Entertainment),
                TouristTarget.Party =>
                    BuildingMgr.GetBuildingSubService(buildingId) == ItemClass.SubService.CommercialLeisure &&
                    type.IsFlagSet(CommercialBuildingType.Entertainment),
                _ => false
            };
        }
    }
}
