// RealTimeResidentAI.cs

namespace RealTime.CustomAI
{
    using System;
    using ColossalFramework;
    using RealTime.Config;
    using RealTime.Core;
    using RealTime.Events;
    using RealTime.GameConnection;
    using RealTime.Managers;
    using SkyTools.Storage;
    using SkyTools.Tools;

    /// <summary>A class incorporating the custom logic for a city resident.</summary>
    /// <typeparam name="TAI">The type of the citizen AI.</typeparam>
    /// <typeparam name="TCitizen">The type of the citizen objects.</typeparam>
    /// <seealso cref="RealTimeHumanAIBase{TCitizen}"/>
    internal sealed partial class RealTimeResidentAI<TAI, TCitizen> : RealTimeHumanAIBase<TCitizen>
        where TAI : class
        where TCitizen : struct
    {
        private readonly ResidentAIConnection<TAI, TCitizen> residentAI;
        private readonly IRealTimeBuildingAI buildingAI;
        private readonly IWorkBehavior workBehavior;
        private readonly ISchoolBehavior schoolBehavior;
        private readonly ISpareTimeBehavior spareTimeBehavior;
        private readonly ITravelBehavior travelBehavior;
        private readonly IMealBehavior mealBehavior;

        private readonly CitizenSchedule[] residentSchedules;

        private readonly float abandonCarRideToWorkDurationThreshold;
        private readonly float abandonCarRideDurationThreshold;

        private float simulationCycle;

        /// <summary>Initializes a new instance of the <see cref="RealTimeResidentAI{TAI, TCitizen}"/> class.</summary>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        /// <param name="config">A <see cref="RealTimeConfig"/> instance containing the mod's configuration.</param>
        /// <param name="connections">A <see cref="GameConnections{T}"/> instance that provides the game connection implementation.</param>
        /// <param name="residentAI">A connection to the game's resident AI.</param>
        /// <param name="eventManager">An <see cref="IRealTimeEventManager"/> instance.</param>
        /// <param name="buildingAI">The custom building AI.</param>
        /// <param name="workBehavior">A behavior that provides simulation info for the citizens work time.</param>
        /// <param name="spareTimeBehavior">A behavior that provides simulation info for the citizens spare time.</param>
        /// <param name="travelBehavior">A behavior that provides simulation info for the citizens traveling.</param>
        public RealTimeResidentAI(
            RealTimeConfig config,
            GameConnections<TCitizen> connections,
            ResidentAIConnection<TAI, TCitizen> residentAI,
            IRealTimeEventManager eventManager,
            IRealTimeBuildingAI buildingAI,
            IWorkBehavior workBehavior,
            ISchoolBehavior schoolBehavior,
            ISpareTimeBehavior spareTimeBehavior,
            ITravelBehavior travelBehavior,
            IMealBehavior mealBehavior)
            : base(config, connections, eventManager)
        {
            this.residentAI = residentAI ?? throw new ArgumentNullException(nameof(residentAI));
            this.buildingAI = buildingAI ?? throw new ArgumentNullException(nameof(buildingAI));
            this.workBehavior = workBehavior ?? throw new ArgumentNullException(nameof(workBehavior));
            this.schoolBehavior = schoolBehavior ?? throw new ArgumentNullException(nameof(schoolBehavior));
            this.spareTimeBehavior = spareTimeBehavior ?? throw new ArgumentNullException(nameof(spareTimeBehavior));
            this.travelBehavior = travelBehavior ?? throw new ArgumentNullException(nameof(travelBehavior));
            this.mealBehavior = mealBehavior ?? throw new ArgumentNullException(nameof(mealBehavior));

            residentSchedules = new CitizenSchedule[CitizenMgr.GetMaxCitizensCount()];
            abandonCarRideDurationThreshold = Constants.MaxTravelTime * 0.8f;
            abandonCarRideToWorkDurationThreshold = Constants.MaxTravelTime;
        }

        /// <summary>Gets a value indicating whether the citizens can grow up in the current game time.</summary>
        public bool CanCitizensGrowUp { get; private set; }

        /// <summary>The entry method of the custom AI.</summary>
        /// <param name="instance">A reference to an object instance of the original AI.</param>
        /// <param name="citizenId">The ID of the citizen to process.</param>
        /// <param name="citizen">A <typeparamref name="TCitizen"/> reference to process.</param>
        public void UpdateLocation(TAI instance, uint citizenId, ref TCitizen citizen)
        {
            if (!EnsureCitizenCanBeProcessed(citizenId, ref citizen))
            {
                residentSchedules[citizenId] = default;
                return;
            }

            ref var schedule = ref residentSchedules[citizenId];
            if (CitizenProxy.IsDead(ref citizen))
            {
                ProcessCitizenDead(instance, citizenId, ref citizen);
                schedule.Schedule(ResidentState.Unknown);
                return;
            }

            if (CitizenProxy.IsSick(ref citizen) && ProcessCitizenSick(instance, citizenId, ref citizen)
                || CitizenProxy.IsArrested(ref citizen) && ProcessCitizenArrested(ref citizen))
            {
                schedule.Schedule(ResidentState.Unknown);
                return;
            }

            var scheduleAction = UpdateCitizenState(citizenId, ref citizen, ref schedule);
            Log.Debug(LogCategory.State, TimeInfo.Now, $"UpdateCitizenState - citizenId {citizenId} schedule action is {scheduleAction}");
            switch (scheduleAction)
            {
                case ScheduleAction.Ignore:
                    return;

                case ScheduleAction.ProcessTransition when ProcessCitizenMoving(ref schedule, citizenId, ref citizen):
                    return;
            }

            if (!TryResolveEatMealState(ref schedule, citizenId, ref citizen))
            {
                return;
            }

            string citizenDesc = GetCitizenDesc(citizenId, ref citizen);

            switch (schedule.CurrentState)
            {
                case ResidentState.Unknown:
                    Log.Debug(LogCategory.State, TimeInfo.Now, $"WARNING: {citizenDesc} is in an UNKNOWN state! Changing to 'moving'");
                    CitizenProxy.SetLocation(ref citizen, Citizen.Location.Moving);
                    return;

                case ResidentState.Evacuating:
                    schedule.Schedule(ResidentState.GoToShelter);
                    break;
            }

            if (TimeInfo.Now < schedule.ScheduledStateTime)
            {
                var currentLocation = CitizenProxy.GetLocation(ref citizen);
                string text = $"The Citizen {citizenId} current state is {schedule.CurrentState}";
                if (currentLocation != Citizen.Location.Moving)
                {
                    ushort buildingID = CitizenProxy.GetCurrentBuilding(ref citizen);
                    if (buildingID != 0)
                    {
                        string buildingName = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info.name;
                        text += $" and buildingId is {buildingID} and building name is {buildingName} and current location is {currentLocation}";

                        if (buildingAI.IsBuildingWorking(buildingID))
                        {
                            Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} stay at building because it is open");
                        }
                        else
                        {
                            if (buildingAI.IsBuildingOpeningSoon(buildingID, 1))
                            {
                                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} stay at building because it is opening soon");
                            }
                            else
                            {
                                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} reschedule because the building is currently closed");
                                schedule.Schedule(ResidentState.Unknown);
                            }
                        }
                    }
                }

                Log.Debug(LogCategory.Schedule, text);
                Log.Debug(LogCategory.Schedule, TimeInfo.Now, $"The Citizen {citizenId} will excute the next activity in {schedule.ScheduledStateTime:dd.MM.yy HH:mm}");
                return;
            }

            string text2 = $"Citizen {citizenId} is in state {schedule.CurrentState} and the scheduled state is {schedule.ScheduledState} and the last scheduled state is {schedule.LastScheduledState}";

            if (schedule.CurrentState == ResidentState.EatMeal)
            {
                text2 += $" and the meal type is {schedule.CurrentMealType}";
            }

            Log.Debug(LogCategory.Schedule, text2);
            bool updated = schedule.ScheduledState != ResidentState.GoToShelter && schedule.CurrentState != ResidentState.InShelter && UpdateCitizenSchedule(ref schedule, citizenId, ref citizen);
            ExecuteCitizenSchedule(ref schedule, instance, citizenId, ref citizen, updated);
        }

        /// <summary>Notifies that a citizen has arrived at their destination.</summary>
        /// <param name="citizenId">The citizen ID to process.</param>
        /// <param name="citizen">A <typeparamref name="TCitizen"/> reference to process.</param>
        public void RegisterCitizenArrival(uint citizenId, ref TCitizen citizen)
        {
            ref var schedule = ref residentSchedules[citizenId];
            Log.Debug(LogCategory.Movement, TimeInfo.Now, $"The citizen {citizenId} active travel state is {schedule.ActiveTravelState}");

            var currentLocation = CitizenMgr.GetCitizenLocation(citizenId);
            switch (currentLocation)
            {
                case Citizen.Location.Home:
                    schedule.CurrentState = ResidentState.AtHome;
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"The citizen {citizenId} arrived at home");
                    schedule.ClearActiveTravelState();
                    break;

                case Citizen.Location.Work:
                    bool handled = false;
                    if (schedule.SchoolBuilding != 0 && schedule.ActiveTravelState == ResidentState.GoToSchool)
                    {
                        schedule.UpdateTravelTimeToSchool(TimeInfo.Now);
                        schedule.CurrentState = ResidentState.AtSchool;
                        schedule.CurrentMealType = MealType.None;
                        Log.Debug(LogCategory.Movement, $"The citizen {citizenId} arrived at school at {TimeInfo.Now} and needs {schedule.TravelTimeToSchool} hours to get to school");
                        handled = true;
                    }
                    else if (schedule.WorkBuilding != 0 && schedule.ActiveTravelState == ResidentState.GoToWork)
                    {
                        schedule.UpdateTravelTimeToWork(TimeInfo.Now);
                        schedule.CurrentState = ResidentState.AtWork;
                        schedule.CurrentMealType = MealType.None;
                        Log.Debug(LogCategory.Movement, $"The citizen {citizenId} arrived at work at {TimeInfo.Now} and needs {schedule.TravelTimeToWork} hours to get to work");
                        handled = true;
                    }
                    if (handled)
                    {
                        schedule.ClearActiveTravelState();
                    }
                    break;

                case Citizen.Location.Visit:
                    Log.Debug(LogCategory.Movement, $"The citizen {citizenId} arrived at their destination at {TimeInfo.Now} after {schedule.FindVisitPlaceAttempts} attempts to find a visit place");
                    schedule.FindVisitPlaceAttempts = 0;

                    ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
                    var buildingService = BuildingMgr.GetBuildingService(currentBuilding);

                    if(schedule.Hint == ScheduleHint.AttendingEvent && schedule.ActiveTravelState == ResidentState.GoToRelax)
                    {
                        ushort eventBuilding = schedule.EventBuilding;
                        schedule.EventBuilding = 0;
                        var cityEvent = EventMgr.GetCityEvent(eventBuilding);
                        if (cityEvent != null)
                        {
                            schedule.Schedule(ResidentState.Unknown, cityEvent.EndTime);
                            schedule.CurrentState = ResidentState.Relaxing;
                            schedule.CurrentMealType = MealType.None;
                            Log.Debug(LogCategory.Events, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} arrived at event '{eventBuilding}' and will schedule the next activity at {cityEvent.EndTime}");
                        }
                        else
                        {
                            Log.Debug(LogCategory.Events, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} arrived at event building '{eventBuilding}', but the event no longer exists");
                            schedule.Schedule(ResidentState.Unknown);
                        }

                        // Event visit is done; clear travel state and return
                        schedule.ClearActiveTravelState();
                        return;
                    }

                    switch (buildingService)
                    {
                        case ItemClass.Service.Beautification:
                        case ItemClass.Service.Monument:
                        case ItemClass.Service.Tourism:
                        case ItemClass.Service.Commercial when BuildingMgr.GetBuildingSubService(currentBuilding) == ItemClass.SubService.CommercialLeisure:
                            if (schedule.ActiveTravelState == ResidentState.GoToRelax)
                            {
                                schedule.CurrentState = ResidentState.Relaxing;
                                schedule.CurrentMealType = MealType.None;
                                Log.Debug(LogCategory.State, TimeInfo.Now, $"Citizen {citizenId} arrived at leisure building {currentBuilding}, CurrentState = Relaxing");
                            }
                            else if (schedule.ActiveTravelState == ResidentState.GoToMeal && schedule.ScheduledMealType != MealType.None)
                            {
                                schedule.CurrentState = ResidentState.EatMeal;
                                schedule.CurrentMealType = schedule.ScheduledMealType;
                                RegisterCitizenMealStart(citizenId, ref schedule);
                                Log.Debug(LogCategory.State, TimeInfo.Now, $"Citizen {citizenId} arrived at meal building {currentBuilding}, CurrentState = EatMeal, CurrentMealType = {schedule.CurrentMealType}");
                            }
                            break;

                        case ItemClass.Service.Commercial:
                            if (schedule.ActiveTravelState == ResidentState.GoShopping)
                            {
                                schedule.CurrentState = ResidentState.Shopping;
                                schedule.CurrentMealType = MealType.None;
                                Log.Debug(LogCategory.State, TimeInfo.Now, $"Citizen {citizenId} arrived at shopping building {currentBuilding}, CurrentState = Shopping");
                            }
                            else if (schedule.ActiveTravelState == ResidentState.GoToMeal && schedule.ScheduledMealType != MealType.None)
                            {
                                schedule.CurrentState = ResidentState.EatMeal;
                                schedule.CurrentMealType = schedule.ScheduledMealType;
                                RegisterCitizenMealStart(citizenId, ref schedule);
                                Log.Debug(LogCategory.State, TimeInfo.Now, $"Citizen {citizenId} arrived at meal building {currentBuilding}, CurrentState = EatMeal, CurrentMealType = {schedule.CurrentMealType}");
                            }
                            break;

                        case ItemClass.Service.PoliceDepartment when BuildingMgr.GetBuildingSubService(currentBuilding) == ItemClass.SubService.PoliceDepartmentBank:
                            if (schedule.ActiveTravelState == ResidentState.GoToBank)
                            {
                                schedule.CurrentState = ResidentState.AtBank;
                                schedule.CurrentMealType = MealType.None;
                                if(!BankPostOfficeVisitManager.CitizenBankVisitDataExist(citizenId))
                                {
                                    BankPostOfficeVisitManager.CreateBankVisitData(citizenId);
                                }
                                else
                                {
                                    BankPostOfficeVisitManager.SetBankVisitData(citizenId);
                                }
                                Log.Debug(LogCategory.State, TimeInfo.Now, $"Citizen {citizenId} arrived at the bank building {currentBuilding}, CurrentState = AtBank");
                            }
                            break;

                        case ItemClass.Service.PublicTransport when BuildingMgr.GetBuildingSubService(currentBuilding) == ItemClass.SubService.PublicTransportPost:
                            if (schedule.ActiveTravelState == ResidentState.GoToPostOffice)
                            {
                                schedule.CurrentState = ResidentState.AtPostOffice;
                                schedule.CurrentMealType = MealType.None;
                                if (!BankPostOfficeVisitManager.CitizenPostOfficeVisitDataExist(citizenId))
                                {
                                    BankPostOfficeVisitManager.CreatePostOfficeVisitData(citizenId);
                                }
                                else
                                {
                                    BankPostOfficeVisitManager.SetPostOfficeVisitData(citizenId);
                                }
                                Log.Debug(LogCategory.State, TimeInfo.Now, $"Citizen {citizenId} arrived at the post office building {currentBuilding}, CurrentState = AtPostOffice");
                            }
                            break;
                        
                        case ItemClass.Service.Disaster when schedule.ActiveTravelState == ResidentState.GoToShelter:
                            schedule.CurrentState = ResidentState.InShelter;
                            schedule.CurrentMealType = MealType.None;
                            Log.Debug(LogCategory.State, TimeInfo.Now, $"Citizen {citizenId} CurrentState is {schedule.CurrentState}");
                            break;

                        default:
                            schedule.CurrentState = ResidentState.Visiting;
                            schedule.CurrentMealType = MealType.None;
                            Log.Debug(LogCategory.State, TimeInfo.Now, $"Citizen {citizenId} arrived at building {currentBuilding} with service {buildingService}, but no specific action is defined for this service, CurrentState = Visiting");
                            break;
                    }
                    schedule.ClearActiveTravelState();
                    break;

                case Citizen.Location.Moving:
                    return;
            }

            schedule.DepartureTime = default;
        }

        /// <summary>Processes the citizen behavior while waiting for public transport.</summary>
        /// <param name="instance">The game's resident AI class instance.</param>
        /// <param name="citizenId">The citizen ID.</param>
        /// <param name="instanceId">The citizen's instance ID.</param>
        public void ProcessWaitingForTransport(TAI instance, uint citizenId, ushort instanceId)
        {
            const CitizenInstance.Flags flagsMask = CitizenInstance.Flags.BoredOfWaiting | CitizenInstance.Flags.EnteringVehicle;

            if (!Config.CanAbandonJourney
                || CitizenMgr.GetInstanceFlags(instanceId, flagsMask) != CitizenInstance.Flags.BoredOfWaiting
                || (CitizenMgr.GetInstanceWaitCounter(instanceId) & 0x3F) != 1)
            {
                return;
            }

            ref var citizen = ref CitizenMgr.GetCitizen(instanceId);
            ushort targetBuilding = CitizenMgr.GetTargetBuilding(instanceId);

            buildingAI.RegisterReachingTrouble(targetBuilding);
            if (targetBuilding == CitizenProxy.GetHomeBuilding(ref citizen))
            {
                return;
            }

            bool isStudent = CitizenProxy.HasFlags(ref citizen, Citizen.Flags.Student);
            bool child_or_teen = CitizenProxy.GetAge(ref citizen) == Citizen.AgeGroup.Child || CitizenProxy.GetAge(ref citizen) == Citizen.AgeGroup.Teen;
            if (RealTimeCore.IsSchoolBusesModEnabled && targetBuilding == CitizenProxy.GetWorkOrSchoolBuilding(ref citizen) && isStudent && child_or_teen)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} is a student and is waiting for school bus to go to school, so they will not abandon the journey");
                return;
            }

            Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} abandons the public transport journey because of waiting for too long");
            CitizenMgr.StopMoving(instanceId, resetTarget: false);
            DoScheduledHome(ref residentSchedules[citizenId], instance, citizenId, ref citizen);
        }

        /// <summary>Notifies that a citizen has started a journey somewhere.</summary>
        /// <param name="citizenId">The citizen ID to process.</param>
        /// <param name="citizen">A <typeparamref name="TCitizen"/> reference to process.</param>
        /// <param name="targetBuilding">The target building ID (0 if none).</param>
        public void RegisterCitizenDeparture(uint citizenId, ref TCitizen citizen, ushort targetBuilding)
        {
            ref var schedule = ref residentSchedules[citizenId];
            schedule.DepartureTime = TimeInfo.Now;

            // Determine the intended activity from schedule + target, then set ActiveTravelState
            if (targetBuilding == 0)
            {
                Log.Debug(LogCategory.Movement, $"The citizen {citizenId} has no specific building target (e.g., wandering, or some special cases)");
                // No specific building target (e.g., wandering, or some special cases)
                return;
            }

            if(schedule.ScheduledState != ResidentState.Unknown && schedule.ScheduledState != ResidentState.Ignored)
            {
                Log.Debug(LogCategory.Movement, $"The citizen {citizenId} begin travel to {schedule.ScheduledState} and is going to {targetBuilding}");
                schedule.BeginTravel(schedule.ScheduledState);
                schedule.CurrentState = ResidentState.InTransition;
            }
        }

        /// <summary>Performs simulation for starting a day cycle beginning with specified hour.
        /// Enables the logic to perform the 'new cycle processing' for the citizens.</summary>
        /// <param name="hour">The hour of the cycle.</param>
        public void BeginNewHourCycleProcessing(int hour)
        {
            Log.Debug(LogCategory.Generic, TimeInfo.Now, "Starting of the 'new cycle' processing for each citizen...");
            if (hour == 0)
            {
                workBehavior.BeginNewDay();
                schoolBehavior.BeginNewDay();
                todayWakeUp = TimeInfo.Now.Date.AddHours(Config.WakeUpHour);
            }

            CanCitizensGrowUp = true;
        }

        /// <summary>Disables the 'new cycle processing' for the citizens.</summary>
        public void EndHourCycleProcessing()
        {
            if (Config.UseSlowAging)
            {
                CanCitizensGrowUp = false;
            }

            Log.Debug(LogCategory.Generic, TimeInfo.Now, "The 'new cycle' processing for the citizens is now completed.");
        }

        /// <summary>Performs simulation for starting a new day for a citizen with specified ID.</summary>
        /// <param name="citizenId">The citizen ID to process.</param>
        public void BeginNewDayForCitizen(uint citizenId)
        {
            if (citizenId != 0)
            {
                ProcessVacation(citizenId);
            }
        }

        /// <summary>
        /// Determines whether the specified <paramref name="citizen"/> can give life to a new citizen.
        /// </summary>
        /// <param name="citizenId">The ID of the citizen to check.</param>
        /// <param name="citizen">The citizen to check.</param>
        /// <returns>
        ///   <c>true</c> if the specified <paramref name="citizen"/> can make babies; otherwise, <c>false</c>.
        /// </returns>
        public bool CanMakeBabies(uint citizenId, ref TCitizen citizen)
        {
            uint idFlag = citizenId % 3;
            uint timeFlag = (uint)TimeInfo.CurrentHour % 3;
            if (!Config.UseSlowAging)
            {
                idFlag = 0;
                timeFlag = 0;
            }

            if (timeFlag != idFlag || CitizenProxy.IsDead(ref citizen) || CitizenProxy.HasFlags(ref citizen, Citizen.Flags.MovingIn))
            {
                return false;
            }

            switch (CitizenProxy.GetAge(ref citizen))
            {
                case Citizen.AgeGroup.Young:
                    return CitizenProxy.GetGender(citizenId) == Citizen.Gender.Male || Random.ShouldOccur(Constants.YoungFemalePregnancyChance);

                case Citizen.AgeGroup.Adult:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>Sets the duration (in hours) of a full simulation cycle for all citizens.
        /// The game calls the simulation methods for a particular citizen with this period.</summary>
        /// <param name="cyclePeriod">The citizens simulation cycle period, in game hours.</param>
        public void SetSimulationCyclePeriod(float cyclePeriod)
        {
            simulationCycle = cyclePeriod;
            Log.Debug(LogCategory.Simulation, $"SIMULATION CYCLE PERIOD: {cyclePeriod} hours, abandon car ride thresholds: {abandonCarRideDurationThreshold} / {abandonCarRideToWorkDurationThreshold}");
        }

        /// <summary>Gets an instance of the storage service that can read and write the custom schedule data.</summary>
        /// <param name="serviceFactory">A method accepting an array of citizen schedules and returning an instance
        /// of the <see cref="IStorageData"/> service.</param>
        /// <returns>An object that implements the <see cref="IStorageData"/> interface.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the argument is null.</exception>
        public IStorageData GetStorageService(Func<CitizenSchedule[], IStorageData> serviceFactory) => serviceFactory == null ? throw new ArgumentNullException(nameof(serviceFactory)) : serviceFactory(residentSchedules);

        public CitizenSchedule[] GetResidentSchedules() => residentSchedules ?? throw new InvalidOperationException("residentSchedules is not initialized.");

        /// <summary>Apply loadedSchedules to the residentSchedules array.</summary>
        public void ApplyLoadedSchedules(CitizenSchedule[] loadedSchedules)
        {
            int count = Math.Min(loadedSchedules.Length, residentSchedules.Length);
            Array.Copy(loadedSchedules, residentSchedules, count);
        }

        /// <summary>Gets the citizen schedule. Note that the method returns the reference
        /// and thus doesn't prevent changing the schedule.</summary>
        /// <param name="citizenId">The ID of the citizen to get the schedule for.</param>
        /// <returns>The original schedule of the citizen.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="citizenId"/> is 0.</exception>
        public ref CitizenSchedule GetCitizenSchedule(uint citizenId)
        {
            if (citizenId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(citizenId), citizenId, "The citizen ID cannot be 0");
            }

            return ref residentSchedules[citizenId];
        }

        /// <summary>Clear the citizen schedule.</summary>
        /// <param name="citizenId">The ID of the citizen to get the schedule for.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="citizenId"/> is 0.</exception>
        public void ClearCitizenSchedule(uint citizenId)
        {
            if (citizenId == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(citizenId), citizenId, "The citizen ID cannot be 0");
            }

            ref var schedule = ref residentSchedules[citizenId];

            schedule.CurrentState = ResidentState.Unknown;
            schedule.UpdateWorkShift(WorkShift.Unemployed, -1, 0, 0);
            schedule.UpdateSchoolClass(SchoolClass.NoSchool, 0, 0);
            schedule.UpdateTravelTimeToWork((DateTime)default);
            schedule.WorkBuilding = 0;
            schedule.SchoolBuilding = 0;
            schedule.WorkStatus = 0;
            schedule.SchoolStatus = 0;
            schedule.FindVisitPlaceAttempts = 0;
            schedule.MealsEatenOutToday = 0;
            schedule.VacationDaysLeft = 0;
            schedule.DepartureTime = default;
            schedule.Schedule(ResidentState.Unknown);
        }

        /// <summary>Clear all the stuck citizens schedule.</summary>
        public void ClearStuckCitizensSchedule()
        {
            var citizens = CitizenManager.instance.m_citizens.m_buffer;
            for (uint i = 0; i < citizens.Length; ++i)
            {
                var flags = citizens[i].m_flags;

                if ((flags & Citizen.Flags.Created) == 0 || (flags & Citizen.Flags.DummyTraffic) != 0 || (flags & Citizen.Flags.Tourist) != 0)
                {
                    continue;
                }
                ref var schedule = ref GetCitizenSchedule(i);
                if(schedule.ScheduledStateTime > TimeInfo.Now.FutureHour(48))
                {
                    ClearCitizenSchedule(i);
                }
            }
        }

        /// <summary>Clear all the stuck tourists in hotels.</summary>
        public void ClearStuckTouristsInHotels()
        {
            var buildings = BuildingManager.instance.m_buildings.m_buffer;
            for (ushort buildingId = 0; buildingId < buildings.Length; ++buildingId)
            {
                ref var buildingData = ref buildings[buildingId];
                if (BuildingManagerConnection.IsHotel(buildingId))
                {
                    var instance = Singleton<CitizenManager>.instance;
                    uint num = buildingData.m_citizenUnits;
                    int num2 = 0;
                    while (num != 0)
                    {
                        if ((instance.m_units.m_buffer[num].m_flags & CitizenUnit.Flags.Hotel) != 0)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                uint citizen = instance.m_units.m_buffer[num].GetCitizen(i);
                                if (Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizen].m_hotelBuilding == 0)
                                {
                                    instance.m_citizens.m_buffer[citizen].RemoveFromUnit(citizen, ref instance.m_units.m_buffer[num]);
                                }
                                else if (Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizen].CurrentLocation == Citizen.Location.Home)
                                {
                                    instance.m_citizens.m_buffer[citizen].RemoveFromUnit(citizen, ref instance.m_units.m_buffer[num]);
                                }
                            }
                        }
                        num = instance.m_units.m_buffer[num].m_nextUnit;
                        if (++num2 > Singleton<CitizenManager>.instance.m_units.m_size)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>Clear all the stuck citizens in closed buildings.</summary>
        public void ClearStuckCitizensInClosedBuildings()
        {
            var buildings = BuildingManager.instance.m_buildings.m_buffer;
            for (ushort buildingId = 0; buildingId < buildings.Length; ++buildingId)
            {
                ref var buildingData = ref buildings[buildingId];
                if (!buildingAI.IsBuildingWorking(buildingId))
                {
                    var instance = Singleton<CitizenManager>.instance;
                    uint num = buildingData.m_citizenUnits;
                    int num2 = 0;
                    while (num != 0)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            uint citizenId = instance.m_units.m_buffer[num].GetCitizen(i);
                            ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenId];
                            if (citizen.CurrentLocation != Citizen.Location.Home && (citizen.m_flags & Citizen.Flags.Tourist) == 0)
                            {
                                citizen.CurrentLocation = Citizen.Location.Home;
                            }
                        }
                        num = instance.m_units.m_buffer[num].m_nextUnit;
                        if (++num2 > Singleton<CitizenManager>.instance.m_units.m_size)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>Try Resolve citizen meal state.</summary>
        /// <returns><c>true</c> if UpdateLocation should CONTINUE with normal processing;
        /// <c>false</c>if we handled everything and want to skip further update this tick.</returns>
        public bool TryResolveEatMealState(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen)
        {
            if (schedule.CurrentState != ResidentState.EatMeal)
            {
                // Not eating → nothing special to do.
                return true;
            }

            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);

            // No building → cannot be eating; stop meal.
            if (currentBuilding == 0)
            {
                StopEatingAndScheduleNextState(ref schedule, citizenId);
                return true; // let normal update run so they move
            }

            bool buildingIsWorking = buildingAI.IsBuildingWorking(currentBuilding);

            // Building closed → stop eating immediately.
            if (!buildingIsWorking)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} is eating {schedule.CurrentMealType} at building {currentBuilding}, but the building is now closed");

                StopEatingAndScheduleNextState(ref schedule, citizenId);
                return true; // continue update so they leave
            }

            // No scheduled end time → treat as “no active meal”; stop eating.
            if (schedule.ScheduledMealEndTime == default)
            {
                StopEatingAndScheduleNextState(ref schedule, citizenId);
                return true;
            }

            // Still within meal time?
            if (TimeInfo.Now < schedule.ScheduledMealEndTime)
            {
                if (schedule.Hint == ScheduleHint.WorkOrSchoolRelatedMeal)
                {
                    if (TryGetMealTravelTime(ref schedule, currentBuilding, out _, out float returnTravel))
                    {
                        Log.Debug(LogCategory.State, TimeInfo.Now, $"Citizen is still eating {schedule.CurrentMealType} until {schedule.ScheduledMealEndTime:dd.MM.yy HH:mm} and needs {returnTravel} hours to get back to work or school");

                        if (CanCompleteWorkOrSchoolMeal(ref schedule, schedule.ScheduledMealEndTime, returnTravel))
                        {
                            // Stay in EatMeal; skip normal movement this tick.
                            return false;
                        }
                    }

                    // Can't make it back in time → stop eating and go to work/school/unknown.
                    StopEatingAndScheduleNextState(ref schedule, citizenId);
                    return true; // continue update so they move
                }
                else
                {
                    // Non-work/school meal, still in time → stay in building.
                    return false;
                }
            }

            // Meal time expired → stop eating.
            StopEatingAndScheduleNextState(ref schedule, citizenId);
            return true; // continue update so they leave
        }

        private void StopEatingAndScheduleNextState(ref CitizenSchedule schedule, uint citizenId)
        {
            if (schedule.Hint == ScheduleHint.WorkOrSchoolRelatedMeal)
            {
                if (schedule.SchoolStatus == SchoolStatus.Studying)
                {
                    schedule.Schedule(ResidentState.GoToSchool);
                }
                else if (schedule.WorkStatus == WorkStatus.Working)
                {
                    schedule.Schedule(ResidentState.GoToWork);
                }
                else
                {
                    schedule.Schedule(ResidentState.Unknown);
                }

                schedule.Hint = ScheduleHint.None;
            }
            else
            {
                schedule.Schedule(ResidentState.Unknown);
            }

            Log.Debug(LogCategory.State, TimeInfo.Now, $"Citizen {citizenId} stopped eating {schedule.CurrentMealType} and switched to {schedule.CurrentState}");
        }

        private void RegisterCitizenMealStart(uint citizenId, ref CitizenSchedule schedule)
        {
            Log.Debug(LogCategory.Movement, $"Citizen {citizenId} arrived at their destination at {TimeInfo.Now:dd.MM.yy HH:mm} and will start eating {schedule.ScheduledMealType}");
            MarkScheduledMealStarted(ref schedule);

            float mealDuration = mealBehavior.GetMealDuration(schedule.ScheduledMealType);

            var mealEnd = TimeInfo.Now.AddHours(mealDuration);
            schedule.UpdateMealEndTime(mealEnd);

            if (schedule.Hint == ScheduleHint.WorkOrSchoolRelatedMeal)
            {
                if (schedule.SchoolStatus == SchoolStatus.Studying)
                {
                    schedule.Schedule(ResidentState.GoToSchool, mealEnd);
                }
                else if (schedule.WorkStatus == WorkStatus.Working)
                {
                    schedule.Schedule(ResidentState.GoToWork, mealEnd);
                }
                schedule.Hint = ScheduleHint.None;
                Log.Debug(LogCategory.Movement, $"Citizen {citizenId} started eating {schedule.LastScheduledMealType} at {TimeInfo.Now:dd.MM.yy HH:mm}, and will finish eating at {mealEnd:dd.MM.yy HH:mm} and then will {schedule.ScheduledState}");
            }
            else
            {
                schedule.Schedule(ResidentState.Unknown, mealEnd);
                Log.Debug(LogCategory.Movement, $"Citizen {citizenId} started eating {schedule.LastScheduledMealType} at {TimeInfo.Now:dd.MM.yy HH:mm}, and will finish eating at {mealEnd:dd.MM.yy HH:mm} and then will schedule Unknown");
            }
        }
    }
}
