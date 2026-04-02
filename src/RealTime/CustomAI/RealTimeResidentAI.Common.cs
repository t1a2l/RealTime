// RealTimeResidentAI.Common.cs

namespace RealTime.CustomAI
{
    using System;
    using RealTime.Managers;
    using SkyTools.Tools;
    using static Constants;

    internal sealed partial class RealTimeResidentAI<TAI, TCitizen>
    {
        private DateTime todayWakeUp;

        private readonly uint[] familyBuffer = new uint[4];

        private enum ScheduleAction
        {
            Ignore,
            ProcessTransition,
            ProcessState,
        }

        private void ProcessCitizenDead(TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            switch (CitizenProxy.GetLocation(ref citizen))
            {
                case Citizen.Location.Home when currentBuilding != 0:
                    CitizenProxy.SetWorkplace(ref citizen, citizenId, 0);
                    CitizenProxy.SetVisitPlace(ref citizen, citizenId, 0);
                    break;

                case Citizen.Location.Work when currentBuilding != 0:
                    CitizenProxy.SetHome(ref citizen, citizenId, 0);
                    CitizenProxy.SetVisitPlace(ref citizen, citizenId, 0);
                    break;

                case Citizen.Location.Visit when currentBuilding != 0:
                    CitizenProxy.SetHome(ref citizen, citizenId, 0);
                    CitizenProxy.SetWorkplace(ref citizen, citizenId, 0);

                    if (BuildingMgr.GetBuildingService(CitizenProxy.GetVisitBuilding(ref citizen)) == ItemClass.Service.HealthCare)
                    {
                        return;
                    }

                    break;

                case Citizen.Location.Moving when CitizenProxy.GetVehicle(ref citizen) != 0:
                    CitizenProxy.SetHome(ref citizen, citizenId, 0);
                    CitizenProxy.SetWorkplace(ref citizen, citizenId, 0);
                    CitizenProxy.SetVisitPlace(ref citizen, citizenId, 0);
                    return;

                default:
                    Log.Debug(LogCategory.State, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} is released because of death");
                    residentSchedules[citizenId] = default;
                    CitizenMgr.ReleaseCitizen(citizenId);
                    return;
            }

            residentAI.FindHospital(instance, citizenId, currentBuilding, TransferManager.TransferReason.Dead);
            Log.Debug(LogCategory.State, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} is dead, body should get serviced");
        }

        private bool ProcessCitizenArrested(ref TCitizen citizen)
        {
            switch (CitizenProxy.GetLocation(ref citizen))
            {
                case Citizen.Location.Moving:
                    return false;
                case Citizen.Location.Visit
                    when BuildingMgr.GetBuildingService(CitizenProxy.GetVisitBuilding(ref citizen)) == ItemClass.Service.PoliceDepartment
                    && BuildingMgr.GetBuildingSubService(CitizenProxy.GetVisitBuilding(ref citizen)) != ItemClass.SubService.PoliceDepartmentBank:
                    return true;
            }

            CitizenProxy.SetArrested(ref citizen, isArrested: false);
            return false;
        }

        private bool ProcessCitizenSick(TAI instance, uint citizenId, ref TCitizen citizen)
        {
            var currentLocation = CitizenProxy.GetLocation(ref citizen);
            if (currentLocation == Citizen.Location.Moving)
            {
                return false;
            }

            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);

            if (currentLocation != Citizen.Location.Home && currentBuilding == 0)
            {
                Log.Debug(LogCategory.State, $"Teleporting {GetCitizenDesc(citizenId, ref citizen)} back home because they are sick but no building is specified");
                CitizenProxy.SetLocation(ref citizen, Citizen.Location.Home);
                return true;
            }

            if (currentLocation != Citizen.Location.Home && CitizenProxy.GetVehicle(ref citizen) != 0)
            {
                return true;
            }

            if (currentLocation == Citizen.Location.Visit)
            {
                ushort visitBuilding = CitizenProxy.GetVisitBuilding(ref citizen);
                switch (BuildingMgr.GetBuildingService(visitBuilding))
                {
                    case ItemClass.Service.HealthCare:
                        UpdateSickStateOnVisitingHealthcare(citizenId, visitBuilding, ref citizen);
                        return true;

                    case ItemClass.Service.Disaster when !BuildingMgr.BuildingHasFlags(visitBuilding, Building.Flags.Downgrading):
                        return true;
                }
            }

            Log.Debug(LogCategory.State, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} is sick, trying to get to a hospital");
            residentAI.FindHospital(instance, citizenId, currentBuilding, TransferManager.TransferReason.Sick);
            return true;
        }

        private void DoScheduledEvacuation(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            schedule.Schedule(ResidentState.Unknown);
            ushort building = CitizenProxy.GetCurrentBuilding(ref citizen);
            if (building == 0)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} is trying to find a shelter from current position");
                TransferMgr.AddOutgoingOfferFromCurrentPosition(citizenId, residentAI.GetEvacuationReason(instance, 0));
            }
            else
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} is trying to find a shelter from {building}");
                residentAI.FindEvacuationPlace(instance, citizenId, building, residentAI.GetEvacuationReason(instance, building));
            }
        }

        private bool ProcessCitizenInShelter(ref CitizenSchedule schedule, ref TCitizen citizen, bool noReschedule)
        {
            ushort shelter = CitizenProxy.GetVisitBuilding(ref citizen);
            if (BuildingMgr.BuildingHasFlags(shelter, Building.Flags.Downgrading))
            {
                CitizenProxy.RemoveFlags(ref citizen, Citizen.Flags.Evacuating);
                return true;
            }

            if (schedule.ScheduledState != ResidentState.Unknown && !noReschedule)
            {
                schedule.Schedule(ResidentState.Unknown);
            }

            return false;
        }

        private ScheduleAction UpdateCitizenState(uint citizenId, ref TCitizen citizen, ref CitizenSchedule schedule)
        {
            if (schedule.CurrentState == ResidentState.Ignored)
            {
                return ScheduleAction.Ignore;
            }

            if (CitizenProxy.HasFlags(ref citizen, Citizen.Flags.DummyTraffic))
            {
                schedule.CurrentState = ResidentState.Ignored;
                return ScheduleAction.Ignore;
            }

            var location = CitizenProxy.GetLocation(ref citizen);
            Log.Debug(LogCategory.State, TimeInfo.Now, $"Citizen {citizenId} location is {location}");
            if (location == Citizen.Location.Moving)
            {
                if (CitizenMgr.InstanceHasFlags(
                    CitizenProxy.GetInstance(ref citizen),
                    CitizenInstance.Flags.OnTour | CitizenInstance.Flags.TargetIsNode,
                    all: true))
                {
                    schedule.Hint = ScheduleHint.OnTour;
                }

                schedule.CurrentState = ResidentState.InTransition;
                return ScheduleAction.ProcessTransition;
            }

            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            if (currentBuilding == 0)
            {
                schedule.CurrentState = ResidentState.Unknown;
                return ScheduleAction.ProcessState;
            }

            if (BuildingMgr.BuildingHasFlags(currentBuilding, Building.Flags.Evacuating))
            {
                schedule.CurrentState = ResidentState.Evacuating;
                return ScheduleAction.ProcessState;
            }

            var buildingService = BuildingMgr.GetBuildingService(currentBuilding);
            Log.Debug(LogCategory.State, TimeInfo.Now, $"Citizen {citizenId} current building is {currentBuilding} with service {buildingService}");
            Log.Debug(LogCategory.State, TimeInfo.Now, $"Citizen {citizenId} last scheduled state is {schedule.LastScheduledState}");
            switch (location)
            {
                case Citizen.Location.Home:
                    schedule.CurrentState = ResidentState.AtHome;
                    return ScheduleAction.ProcessState;

                case Citizen.Location.Work:
                    if (CitizenProxy.GetVisitBuilding(ref citizen) == currentBuilding && schedule.WorkStatus != WorkStatus.Working)
                    {
                        // A citizen may visit their own work building (e.g. shopping),
                        // but the game sets the location to 'work' even if the citizen visits the building.
                        goto case Citizen.Location.Visit;
                    }

                    switch (buildingService)
                    {
                        case ItemClass.Service.Electricity:
                        case ItemClass.Service.Water:
                        case ItemClass.Service.HealthCare:
                        case ItemClass.Service.PoliceDepartment:
                        case ItemClass.Service.FireDepartment:
                        case ItemClass.Service.Disaster:
                            if (BuildingMgr.IsAreaEvacuating(currentBuilding))
                            {
                                schedule.CurrentState = ResidentState.InShelter;
                                return ScheduleAction.ProcessState;
                            }

                            break;
                    }

                    if (CitizenProxy.HasFlags(ref citizen, Citizen.Flags.Student))
                    {
                        schedule.CurrentState = ResidentState.AtSchool;
                    }
                    else
                    {
                        schedule.CurrentState = ResidentState.AtWork;
                    }
                    return ScheduleAction.ProcessState;

                case Citizen.Location.Visit:
                    switch (buildingService)
                    {
                        case ItemClass.Service.Beautification:
                        case ItemClass.Service.Monument:
                        case ItemClass.Service.Tourism:
                        case ItemClass.Service.Commercial
                            when BuildingMgr.GetBuildingSubService(currentBuilding) == ItemClass.SubService.CommercialLeisure
                                && schedule.WorkStatus != WorkStatus.Working:          
                            if(schedule.LastScheduledState == ResidentState.GoToRelax)
                            {
                                schedule.CurrentState = ResidentState.Relaxing;
                            }
                            else if (schedule.LastScheduledState == ResidentState.GoToMeal)
                            {
                                schedule.CurrentState = ResidentState.EatMeal;
                            }
                            return ScheduleAction.ProcessState;

                        case ItemClass.Service.Commercial:
                            if(schedule.WorkStatus == WorkStatus.Working && schedule.LastScheduledState == ResidentState.GoToMeal)
                            {
                                schedule.CurrentState = ResidentState.EatMeal;
                            }
                            else
                            {
                                if (schedule.LastScheduledState == ResidentState.GoShopping)
                                {
                                    schedule.CurrentState = ResidentState.Shopping;
                                }
                                else if (schedule.LastScheduledState == ResidentState.GoToMeal)
                                {
                                    schedule.CurrentState = ResidentState.EatMeal;
                                }
                            }
                            return ScheduleAction.ProcessState;

                        case ItemClass.Service.Disaster when schedule.LastScheduledState == ResidentState.GoToShelter:
                            schedule.CurrentState = ResidentState.InShelter;
                            return ScheduleAction.ProcessState;
                    }

                    schedule.CurrentState = ResidentState.Visiting;
                    return ScheduleAction.ProcessState;
            }

            return ScheduleAction.Ignore;
        }

        private bool UpdateCitizenSchedule(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen)
        {
            ushort workBuilding = 0, schoolBuilding = 0;
            // If the game changed the work/school building, we have to update the work shifts class time first
            if (CitizenProxy.HasFlags(ref citizen, Citizen.Flags.Student))
            {
                schoolBuilding = CitizenProxy.GetWorkOrSchoolBuilding(ref citizen);
                if (schedule.SchoolBuilding != schoolBuilding)
                {
                    schedule.SchoolBuilding = schoolBuilding;
                    schoolBehavior.UpdateSchoolClass(ref schedule, CitizenProxy.GetAge(ref citizen));
                    if (schedule.CurrentState == ResidentState.AtSchool && schedule.ScheduledStateTime == default)
                    {
                        // When enabling for an existing game, the citizens that are studying have no schedule yet
                        schedule.Schedule(ResidentState.Unknown, TimeInfo.Now.FutureHour(schedule.SchoolClassEndHour));
                    }
                    else if (schedule.SchoolBuilding == 0 && (schedule.ScheduledState == ResidentState.GoToSchool || schedule.SchoolStatus == SchoolStatus.Studying))
                    {
                        // This is for the case when the citizen stop studying while in school
                        schedule.Schedule(ResidentState.Unknown);
                    }
                    else if (schedule.SchoolBuilding != 0 && schedule.ScheduledState == ResidentState.GoToSchool)
                    {
                        // This is for the case when the school was updated but the citizen is still going to school according to the old schedule
                        schedule.Schedule(ResidentState.Unknown);
                    }

                    Log.Debug(LogCategory.Schedule, $"Updated school class for citizen {citizenId}: school class {schedule.SchoolClass}, {schedule.SchoolClassStartHour} - {schedule.SchoolClassEndHour}");
                }
            }
            else
            {
                workBuilding = CitizenProxy.GetWorkOrSchoolBuilding(ref citizen);
                // workplace was changed or removed while working
                if (schedule.WorkBuilding != workBuilding || workBuilding == 0 && schedule.WorkShift != WorkShift.Unemployed)
                {
                    schedule.WorkBuilding = workBuilding;
                    // for essential buildings
                    var chosenWorkShift = SetWorkShift(workBuilding);
                    workBehavior.UpdateWorkShift(ref schedule, CitizenProxy.GetAge(ref citizen), chosenWorkShift);   
                    if (schedule.CurrentState == ResidentState.AtWork && schedule.ScheduledStateTime == default)
                    {
                        // When enabling for an existing game, the citizens that are working have no schedule yet
                        schedule.Schedule(ResidentState.Unknown, TimeInfo.Now.FutureHour(schedule.WorkShiftEndHour));
                    }
                    else if (schedule.WorkBuilding == 0 && (schedule.ScheduledState == ResidentState.GoToWork || schedule.WorkStatus == WorkStatus.Working))
                    {
                        // This is for the case when the citizen becomes unemployed while at work
                        schedule.Schedule(ResidentState.Unknown);
                    }
                    else if (schedule.WorkBuilding != 0 && schedule.ScheduledState == ResidentState.GoToWork)
                    {
                        // This is for the case when the workplace was updated but the citizen is still going to work according to the old schedule
                        schedule.Schedule(ResidentState.Unknown);
                    }

                    Log.Debug(LogCategory.Schedule, $"Updated work shifts for citizen {citizenId}: work shift {schedule.WorkShift}, {schedule.WorkShiftStartHour} - {schedule.WorkShiftEndHour}, weekends: {schedule.WorksOnWeekends}");
                }
            }

            // citizen was an event worker and event has finished, fire worker
            if(schedule.WorkBuilding != 0 && schedule.WorkShift == WorkShift.Event && schedule.ScheduledState != ResidentState.GoToWork)
            {
                var buildingEvent = EventMgr.GetCityEvent(schedule.WorkBuilding);
                if(buildingEvent != null && TimeInfo.CurrentHour > schedule.WorkShiftEndHour)
                {
                    CitizenProxy.SetWorkplace(ref citizen, citizenId, 0);
                }
            }


            // nobody working or on the way to work, and building is essential service
            if (workBuilding != 0 && IsEssentialService(workBuilding) && GetCitizensInWorkPlaceByShift(workBuilding, schedule.WorkShift) == 0 && Config.WorkForceMatters)
            {
                schedule.WorkStatus = WorkStatus.None;
            }

            if (schedule.ScheduledState != ResidentState.Unknown)
            {
                return false;
            }

            if (CitizenProxy.HasFlags(ref citizen, Citizen.Flags.Student))
            {
                if (schedule.SchoolStatus == SchoolStatus.Studying)
                {
                    schedule.SchoolStatus = SchoolStatus.None;
                }
            }
            else
            {
                if (schedule.WorkStatus == WorkStatus.Working)
                {
                    schedule.WorkStatus = WorkStatus.None;
                }
            }

            Log.Debug(LogCategory.Schedule, TimeInfo.Now, $"Scheduling for {GetCitizenDesc(citizenId, ref citizen)}...");

            var nextActivityTime = todayWakeUp;
            if(CitizenProxy.HasFlags(ref citizen, Citizen.Flags.Student))
            {
                schedule.WorkStatus = WorkStatus.None;
                if (schedule.CurrentState != ResidentState.AtSchool && schoolBuilding != 0 && schedule.SchoolStatus != SchoolStatus.OnVacation)
                {
                    if (ScheduleSchool(ref schedule, ref citizen))
                    {
                        return true;
                    }

                    if (schedule.ScheduledStateTime > nextActivityTime)
                    {
                        nextActivityTime = schedule.ScheduledStateTime;
                    }
                }
            }
            else
            {
                schedule.SchoolStatus = SchoolStatus.None;
                if (schedule.CurrentState != ResidentState.AtWork && workBuilding != 0 && schedule.WorkStatus != WorkStatus.OnVacation)
                {
                    if (ScheduleWork(ref schedule, ref citizen))
                    {
                        return true;
                    }

                    if (schedule.ScheduledStateTime > nextActivityTime)
                    {
                        nextActivityTime = schedule.ScheduledStateTime;
                    }
                }
            }

            if(schedule.FindVisitPlaceAttempts < 3)
            {
                if (ScheduleShopping(ref schedule, ref citizen, localOnly: false))
                {
                    Log.Debug(LogCategory.Schedule, $"  - Schedule shopping, visit attempt number {schedule.FindVisitPlaceAttempts + 1}");
                    return true;
                }

                if (ScheduleRelaxing(ref schedule, citizenId, ref citizen))
                {
                    Log.Debug(LogCategory.Schedule, $"  - Schedule relaxing, visit attempt number {schedule.FindVisitPlaceAttempts + 1}");
                    return true;
                }

                if (ScheduleMeal(ref schedule, ref citizen, localOnly: false))
                {
                    Log.Debug(LogCategory.Schedule, $"  - Schedule meal, visit attempt number {schedule.FindVisitPlaceAttempts + 1}");
                    return true;
                }

                if (ScheduleVisiting(ref schedule, ref citizen))
                {
                    Log.Debug(LogCategory.Schedule, $"  - Schedule visiting, visit attempt number {schedule.FindVisitPlaceAttempts + 1}");
                    return true;
                }
            }

            if (schedule.CurrentState == ResidentState.AtHome)
            {
                if (Random.ShouldOccur(StayHomeAllDayChance))
                {
                    if (nextActivityTime < TimeInfo.Now)
                    {
                        nextActivityTime = todayWakeUp.FutureHour(Config.WakeUpHour);
                    }
                }
                else
                {
                    nextActivityTime = default;
                }

#if DEBUG
                if (nextActivityTime <= TimeInfo.Now)
                {
                    Log.Debug(LogCategory.Schedule, "  - Schedule idle until next scheduling run");
                }
                else
                {
                    Log.Debug(LogCategory.Schedule, $"  - Schedule idle until {nextActivityTime}");
                }
#endif
                schedule.Schedule(ResidentState.Unknown, nextActivityTime);
            }
            else
            {
                Log.Debug(LogCategory.Schedule, "  - Schedule moving home");
                schedule.Schedule(ResidentState.GoHome);
            }

            return true;
        }

        private void ExecuteCitizenSchedule(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen, bool noReschedule)
        {
            if (ProcessCurrentState(ref schedule, instance, citizenId, ref citizen, noReschedule)
                && schedule.ScheduledState == ResidentState.Unknown
                && !noReschedule)
            {
                Log.Debug(LogCategory.Schedule, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} will re-schedule now");

                // If the state processing changed the schedule, we need to update it
                UpdateCitizenSchedule(ref schedule, citizenId, ref citizen);
            }

            if (TimeInfo.Now < schedule.ScheduledStateTime)
            {
                Log.Debug(LogCategory.Schedule, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} ScheduledStateTime is {schedule.ScheduledStateTime:dd.MM.yy HH:mm}");
                return;
            }

            if (schedule.CurrentState == ResidentState.AtHome
                && schedule.ScheduledState != ResidentState.GoToShelter
                && IsCitizenVirtual(instance, ref citizen, ShouldRealizeCitizen))
            {
                Log.Debug(LogCategory.Simulation, $" *** Citizen {citizenId} is virtual this time");
                schedule.Schedule(ResidentState.Unknown);
                return;
            }           

            bool executed = false;
            switch (schedule.ScheduledState)
            {
                case ResidentState.GoHome when schedule.CurrentState != ResidentState.AtHome:
                    DoScheduledHome(ref schedule, instance, citizenId, ref citizen);
                    return;

                case ResidentState.GoToWork when schedule.CurrentState != ResidentState.AtWork:
                    DoScheduledWork(ref schedule, instance, citizenId, ref citizen);
                    return;

                case ResidentState.GoToSchool when schedule.CurrentState != ResidentState.AtSchool:
                    DoScheduledSchool(ref schedule, instance, citizenId, ref citizen);
                    return;

                case ResidentState.GoToMeal when schedule.CurrentState != ResidentState.EatMeal:
                    if (schedule.WorkStatus == WorkStatus.None)
                    {
                        DoScheduledWorkMeal(ref schedule, instance, citizenId, ref citizen);
                        executed = true;
                    }
                    else if (schedule.SchoolStatus == SchoolStatus.None)
                    {
                        DoScheduledSchoolMeal(ref schedule, instance, citizenId, ref citizen);
                        executed = true;
                    }
                    else
                    {
                        executed = DoScheduledMeal(ref schedule, instance, citizenId, ref citizen);
                    }
                    break;

                case ResidentState.GoShopping:
                    executed = DoScheduledShopping(ref schedule, instance, citizenId, ref citizen);
                    break;

                case ResidentState.GoToRelax:
                    executed = DoScheduledRelaxing(ref schedule, instance, citizenId, ref citizen);
                    break;

                case ResidentState.GoToShelter when schedule.CurrentState != ResidentState.InShelter:
                    DoScheduledEvacuation(ref schedule, instance, citizenId, ref citizen);
                    return;

                case ResidentState.GoToVisit:
                    executed = DoScheduledVisiting(ref schedule, instance, citizenId, ref citizen);
                    return;

                default:
                    return;
            }

            if (!executed && (schedule.CurrentState == ResidentState.AtSchool || schedule.CurrentState == ResidentState.AtWork || schedule.CurrentState == ResidentState.InShelter))
            {
                schedule.Schedule(ResidentState.Unknown);
                DoScheduledHome(ref schedule, instance, citizenId, ref citizen);
            }
        }

        private bool ProcessCurrentState(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen, bool noReschedule)
        {
            switch (schedule.CurrentState)
            {
                case ResidentState.AtHome:
                    return RescheduleAtHome(ref schedule, citizenId, ref citizen, noReschedule);

                case ResidentState.Shopping:
                    return ProcessCitizenShopping(ref schedule, citizenId, ref citizen, noReschedule);

                case ResidentState.EatMeal:
                    return ProcessCitizenEatingMeal(ref schedule, citizenId, ref citizen, noReschedule);

                case ResidentState.Relaxing:
                    return ProcessCitizenRelaxing(ref schedule, citizenId, ref citizen, noReschedule);

                case ResidentState.Visiting:
                    return ProcessCitizenVisit(ref schedule, instance, citizenId, ref citizen, noReschedule);

                case ResidentState.InShelter:
                    return ProcessCitizenInShelter(ref schedule, ref citizen, noReschedule);

                case ResidentState.AtWork:
                    return ProcessCitizenWork(ref schedule, citizenId, ref citizen);
            }

            return false;
        }

        private bool ShouldRealizeCitizen(TAI ai) => residentAI.DoRandomMove(ai);

        private void UpdateSickStateOnVisitingHealthcare(uint citizenId, ushort buildingId, ref TCitizen citizen)
        {
            var citizenAge = CitizenProxy.GetAge(ref citizen);
            if ((citizenAge == Citizen.AgeGroup.Child || citizenAge == Citizen.AgeGroup.Teen) && BuildingMgr.IsBuildingAIOfType<ChildcareAI>(buildingId)
                || citizenAge == Citizen.AgeGroup.Senior && BuildingMgr.IsBuildingAIOfType<EldercareAI>(buildingId))
            {
                if (CitizenProxy.GetHealth(ref citizen) > Random.GetRandomValue(100u))
                {
                    Log.Debug(LogCategory.State, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} was sick, but got healed in a child or elder care building");
                    CitizenProxy.SetSick(ref citizen, isSick: false);
                }
            }
        }

        private bool CanGoOnVacation(uint citizenId)
        {
            ref var schedule = ref residentSchedules[citizenId];
            var citizen = CitizenManager.instance.m_citizens.m_buffer[citizenId];

            // vacation can start only between midnight and 2am
            if (TimeInfo.CurrentHour <= 23.85f || TimeInfo.CurrentHour >= 2f)
            {
                return false;
            }

            if ((citizen.m_flags & Citizen.Flags.Student) != 0)
            {
                if (schedule.SchoolBuilding == 0)
                {
                    return false;
                }

                if (schedule.CurrentState == ResidentState.AtSchool || schedule.CurrentState == ResidentState.EatMeal
                    || schedule.ScheduledState == ResidentState.GoToSchool || schedule.ScheduledState == ResidentState.GoToMeal ||
                    schedule.ScheduledState == ResidentState.GoShopping && schedule.Hint == ScheduleHint.LocalShoppingOnlyBeforeUniversity)
                {
                    return false;
                }

                return true;
            }
            else
            {
                if (schedule.WorkBuilding == 0)
                {
                    return false;
                }
                // nobody working or on the way to work, and building is essential service and workforce matters
                if (Config.WorkForceMatters && IsEssentialService(schedule.WorkBuilding) && GetCitizensInWorkPlaceByShift(schedule.WorkBuilding, schedule.WorkShift) == 0)
                {
                    return false;
                }
                if (schedule.CurrentState == ResidentState.AtWork || schedule.CurrentState == ResidentState.EatMeal
                    || schedule.ScheduledState == ResidentState.GoToWork || schedule.ScheduledState == ResidentState.GoToMeal
                    || schedule.ScheduledState == ResidentState.GoShopping && schedule.Hint == ScheduleHint.LocalShoppingOnlyBeforeWork)
                {
                    return false;
                }

                return true;
            }
        }

        private void ProcessVacation(uint citizenId)
        {
            ref var schedule = ref residentSchedules[citizenId];
            var citizen = CitizenManager.instance.m_citizens.m_buffer[citizenId];

            if (CanGoOnVacation(citizenId))
            {
                if ((citizen.m_flags & Citizen.Flags.Student) != 0)
                {
                    schedule.SchoolStatus = SchoolStatus.OnVacation;
                }
                else
                {
                    schedule.WorkStatus = WorkStatus.OnVacation;
                }
            }
            else
            {
                return;
            }

            // Note: this might lead to different vacation durations for family members even if they all were initialized to same length.
            // This is because the simulation loop for a family member could process this citizen right after their vacation has been set.
            // But we intentionally don't avoid this - let's add some randomness.
            if ((schedule.SchoolStatus == SchoolStatus.OnVacation || schedule.WorkStatus == WorkStatus.OnVacation) && schedule.VacationDaysLeft > 0)
            {
                // vacation can end only between midnight and 2am
                if (TimeInfo.CurrentHour <= 23.85f || TimeInfo.CurrentHour >= 2f)
                {
                    return;
                }

                --schedule.VacationDaysLeft;

                if (schedule.VacationDaysLeft == 0)
                {
                    Log.Debug(LogCategory.State, $"The citizen {citizenId} returns from vacation");
                    if ((citizen.m_flags & Citizen.Flags.Student) != 0)
                    {
                        schedule.SchoolStatus = SchoolStatus.None;
                    }
                    else
                    {
                        schedule.WorkStatus = WorkStatus.None;
                    }

                }

                return;
            }

            int days = 1 + Random.GetRandomValue(Config.MaxVacationLength - 1);
            schedule.VacationDaysLeft = (byte)days;

            Log.Debug(LogCategory.State, $"The citizen {citizenId} is now on vacation for {days} days");
            if (!Random.ShouldOccur(FamilyVacationChance) || !CitizenMgr.TryGetFamily(citizenId, familyBuffer))
            {
                return;
            }

            for (int i = 0; i < familyBuffer.Length; ++i)
            {
                uint familyMemberId = familyBuffer[i];
                if (familyMemberId != 0)
                {
                    Log.Debug(LogCategory.State, $"The citizen {familyMemberId} goes on vacation with {citizenId} as a family member");
                    if(CanGoOnVacation(familyMemberId))
                    {
                        if ((CitizenManager.instance.m_citizens.m_buffer[familyMemberId].m_flags & Citizen.Flags.Student) != 0)
                        {
                            residentSchedules[familyMemberId].SchoolStatus = SchoolStatus.OnVacation;
                        }
                        else
                        {
                            residentSchedules[familyMemberId].WorkStatus = WorkStatus.OnVacation;
                        }
                    }
                    residentSchedules[familyMemberId].VacationDaysLeft = (byte)days;
                }
            }
        }

        // set work shift to the shift with the minimum number of people 
        public WorkShift SetWorkShift(ushort workBuildingId)
        {
            if (workBuildingId == 0 || !IsEssentialService(workBuildingId))
            {
                return WorkShift.Unemployed;
            }

            uint[] workForce = buildingAI.GetBuildingWorkForce(workBuildingId);

            var building = BuildingManager.instance.m_buildings.m_buffer[workBuildingId];

            BuildingWorkTimeManager.WorkTime workTime;

            if (!BuildingWorkTimeManager.BuildingWorkTimeExist(workBuildingId))
            {
                if (!BuildingWorkTimeManager.ShouldHaveBuildingWorkTime(workBuildingId))
                {
                    return WorkShift.Unemployed;
                }
                workTime = BuildingWorkTimeManager.CreateBuildingWorkTime(workBuildingId, building.Info);
            }
            else
            {
                workTime = BuildingWorkTimeManager.GetBuildingWorkTime(workBuildingId);
            }

            if (workTime.HasContinuousWorkShift)
            {
                int continuousDayShiftWorkers = 0;
                int continuousNightShiftWorkers = 0;

                for (int i = 0; i < workForce.Length; i++)
                {
                    var citizen_schedule = GetCitizenSchedule(workForce[i]);
                    switch (citizen_schedule.WorkShift)
                    {
                        case WorkShift.ContinuousDay:
                            continuousDayShiftWorkers++;
                            break;

                        case WorkShift.ContinuousNight when workTime.WorkAtNight == true && workTime.WorkShifts == 2:
                            continuousNightShiftWorkers++;
                            break;

                        default:
                            break;
                    }
                }

                if(workTime.WorkAtNight == true && workTime.WorkShifts == 2)
                {
                    int minShift = Math.Min(continuousDayShiftWorkers, continuousNightShiftWorkers);

                    if (minShift == continuousNightShiftWorkers)
                    {
                        return WorkShift.ContinuousNight;
                    }
                    else
                    {
                        return WorkShift.ContinuousDay;
                    }
                }
                else
                {
                    return WorkShift.ContinuousDay;
                }
            }
            else
            {
                int firstShiftWorkers = 0;
                int secondShiftWorkers = 0;
                int nightShiftWorkers = 0;

                for (int i = 0; i < workForce.Length; i++)
                {
                    var citizen_schedule = GetCitizenSchedule(workForce[i]);
                    switch (citizen_schedule.WorkShift)
                    {
                        case WorkShift.First:
                            firstShiftWorkers++;
                            break;

                        case WorkShift.Second when workTime.WorkShifts == 2 || workTime.WorkShifts == 3:
                            secondShiftWorkers++;
                            break;

                        case WorkShift.Night when workTime.WorkAtNight == true && workTime.WorkShifts == 3:
                            nightShiftWorkers++;
                            break;

                        default:
                            break;
                    }
                }

                if(workTime.WorkShifts == 1)
                {
                    return WorkShift.First;
                }
                else if (workTime.WorkShifts == 2)
                {
                    int minShift = Math.Min(firstShiftWorkers, secondShiftWorkers);
                    if (minShift == firstShiftWorkers)
                    {
                        return WorkShift.First;
                    }
                    else 
                    {
                        return WorkShift.Second;
                    }
                }
                else if (workTime.WorkShifts == 3 && workTime.WorkAtNight)
                {
                    int minShift = Math.Min(Math.Min(firstShiftWorkers, secondShiftWorkers), nightShiftWorkers);
                    if (minShift == firstShiftWorkers)
                    {
                        return WorkShift.First;
                    }
                    else if (minShift == secondShiftWorkers)
                    {
                        return WorkShift.Second;
                    }
                    else if (minShift == nightShiftWorkers)
                    {
                        return WorkShift.Night;
                    }
                    else
                    {
                        return WorkShift.First;
                    }
                }
                else
                {
                    return WorkShift.First;
                }
            }
        }
    }
}
