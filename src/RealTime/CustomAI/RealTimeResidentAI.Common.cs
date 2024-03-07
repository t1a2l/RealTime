// RealTimeResidentAI.Common.cs

namespace RealTime.CustomAI
{
    using System;
    using SkyTools.Tools;
    using UnityEngine;
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
                    when BuildingMgr.GetBuildingService(CitizenProxy.GetVisitBuilding(ref citizen)) == ItemClass.Service.PoliceDepartment:
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

        private bool ProcessCitizenInShelter(ref CitizenSchedule schedule, ref TCitizen citizen)
        {
            ushort shelter = CitizenProxy.GetVisitBuilding(ref citizen);
            if (BuildingMgr.BuildingHasFlags(shelter, Building.Flags.Downgrading))
            {
                CitizenProxy.RemoveFlags(ref citizen, Citizen.Flags.Evacuating);
                return true;
            }

            if (schedule.ScheduledState != ResidentState.Unknown)
            {
                schedule.Schedule(ResidentState.Unknown);
            }

            return false;
        }

        private ScheduleAction UpdateCitizenState(ref TCitizen citizen, ref CitizenSchedule schedule)
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
                schedule.CurrentState = ResidentState.Evacuation;
                return ScheduleAction.ProcessState;
            }

            var buildingService = BuildingMgr.GetBuildingService(currentBuilding);
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

                    if (CitizenProxy.HasFlags(ref citizen, Citizen.Flags.Student) || CitizenProxy.GetAge(ref citizen) == Citizen.AgeGroup.Child || CitizenProxy.GetAge(ref citizen) == Citizen.AgeGroup.Teen)
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

                            schedule.CurrentState = ResidentState.Relaxing;
                            return ScheduleAction.ProcessState;

                        case ItemClass.Service.Commercial:
                            schedule.CurrentState = ResidentState.Shopping;
                            return ScheduleAction.ProcessState;

                        case ItemClass.Service.Disaster:
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
            if (CitizenProxy.HasFlags(ref citizen, Citizen.Flags.Student) || CitizenProxy.GetAge(ref citizen) == Citizen.AgeGroup.Child || CitizenProxy.GetAge(ref citizen) == Citizen.AgeGroup.Teen)
            {
                schoolBuilding = CitizenProxy.GetWorkOrSchoolBuilding(ref citizen);
                if (schedule.SchoolBuilding != schoolBuilding)
                {
                    schedule.SchoolBuilding = schoolBuilding;
                    schoolBehavior.UpdateSchoolClass(ref schedule, CitizenProxy.GetAge(ref citizen));
                    if ((schedule.CurrentState == ResidentState.AtSchool || schedule.CurrentState == ResidentState.AtSchoolOrWork) && schedule.ScheduledStateTime == default)
                    {
                        // When enabling for an existing game, the citizens that are studying have no schedule yet
                        schedule.Schedule(ResidentState.Unknown, TimeInfo.Now.FutureHour(schedule.SchoolClassEndHour));
                    }
                    else if (schedule.SchoolBuilding == 0 && (schedule.ScheduledState == ResidentState.AtSchool || schedule.ScheduledState == ResidentState.AtSchoolOrWork || schedule.SchoolStatus == SchoolStatus.Studying))
                    {
                        // This is for the case when the citizen stop studying while in school
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
                    if ((schedule.CurrentState == ResidentState.AtWork || schedule.CurrentState == ResidentState.AtSchoolOrWork) && schedule.ScheduledStateTime == default)
                    {
                        // When enabling for an existing game, the citizens that are working have no schedule yet
                        schedule.Schedule(ResidentState.Unknown, TimeInfo.Now.FutureHour(schedule.WorkShiftEndHour));
                    }
                    else if (schedule.WorkBuilding == 0 && (schedule.ScheduledState == ResidentState.AtSchoolOrWork || schedule.ScheduledState == ResidentState.AtWork || schedule.WorkStatus == WorkStatus.Working))
                    {
                        // This is for the case when the citizen becomes unemployed while at work
                        schedule.Schedule(ResidentState.Unknown);
                    }

                    Log.Debug(LogCategory.Schedule, $"Updated work shifts for citizen {citizenId}: work shift {schedule.WorkShift}, {schedule.WorkShiftStartHour} - {schedule.WorkShiftEndHour}, weekends: {schedule.WorksOnWeekends}");
                }
            }

            // citizen was an event worker and event has finished, fire worker
            if(schedule.WorkBuilding != 0 && schedule.WorkShift == WorkShift.Event && schedule.ScheduledState != ResidentState.AtWork)
            {
                var buildingEvent = EventMgr.GetCityEvent(schedule.WorkBuilding);
                if(buildingEvent != null && TimeInfo.Now.TimeOfDay.TotalHours > schedule.WorkShiftEndHour)
                {
                    CitizenProxy.SetWorkplace(ref citizen, citizenId, 0);
                }
            }


            // nobody working or on the way to work, and building is essential service
            if (IsEssentialService(workBuilding) && GetCitizensInWorkPlaceByShift(workBuilding, schedule.WorkShift) == 0 && Config.WorkForceMatters)
            {
                schedule.WorkStatus = WorkStatus.None;
            }

            if (schedule.ScheduledState != ResidentState.Unknown)
            {
                return false;
            }

            if (CitizenProxy.HasFlags(ref citizen, Citizen.Flags.Student) || CitizenProxy.GetAge(ref citizen) == Citizen.AgeGroup.Child || CitizenProxy.GetAge(ref citizen) == Citizen.AgeGroup.Teen)
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
            if(CitizenProxy.HasFlags(ref citizen, Citizen.Flags.Student) || CitizenProxy.GetAge(ref citizen) == Citizen.AgeGroup.Child || CitizenProxy.GetAge(ref citizen) == Citizen.AgeGroup.Teen)
            {
                if (schedule.CurrentState != ResidentState.AtSchoolOrWork && schedule.CurrentState != ResidentState.AtSchool && schoolBuilding != 0 && schedule.SchoolStatus != SchoolStatus.OnVacation)
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
                if (schedule.CurrentState != ResidentState.AtSchoolOrWork && schedule.CurrentState != ResidentState.AtWork && workBuilding != 0 && schedule.WorkStatus != WorkStatus.OnVacation)
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
            

            if (ScheduleShopping(ref schedule, ref citizen, localOnly: false))
            {
                Log.Debug(LogCategory.Schedule, "  - Schedule shopping");
                return true;
            }

            if (ScheduleRelaxing(ref schedule, citizenId, ref citizen))
            {
                Log.Debug(LogCategory.Schedule, "  - Schedule relaxing");
                return true;
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
                schedule.Schedule(ResidentState.AtHome);
            }

            return true;
        }

        private void ExecuteCitizenSchedule(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen, bool noReschedule)
        {
            if (ProcessCurrentState(ref schedule, instance, citizenId, ref citizen)
                && schedule.ScheduledState == ResidentState.Unknown
                && !noReschedule)
            {
                Log.Debug(LogCategory.Schedule, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} will re-schedule now");

                // If the state processing changed the schedule, we need to update it
                UpdateCitizenSchedule(ref schedule, citizenId, ref citizen);
            }

            if (TimeInfo.Now < schedule.ScheduledStateTime)
            {
                return;
            }

            if (schedule.CurrentState == ResidentState.AtHome
                && schedule.ScheduledState != ResidentState.InShelter
                && IsCitizenVirtual(instance, ref citizen, ShouldRealizeCitizen))
            {
                Log.Debug(LogCategory.Simulation, $" *** Citizen {citizenId} is virtual this time");
                schedule.Schedule(ResidentState.Unknown);
                return;
            }           

            bool executed;
            switch (schedule.ScheduledState)
            {
                case ResidentState.AtHome:
                    DoScheduledHome(ref schedule, instance, citizenId, ref citizen);
                    return;

                case ResidentState.AtSchoolOrWork:
                    if (CitizenProxy.HasFlags(ref citizen, Citizen.Flags.Student) || CitizenProxy.GetAge(ref citizen) == Citizen.AgeGroup.Child || CitizenProxy.GetAge(ref citizen) == Citizen.AgeGroup.Teen)
                    {
                        DoScheduledSchool(ref schedule, instance, citizenId, ref citizen);
                    }
                    else
                    {
                        DoScheduledWork(ref schedule, instance, citizenId, ref citizen);
                    }
                    return;

                case ResidentState.AtWork:
                    DoScheduledWork(ref schedule, instance, citizenId, ref citizen);
                    return;

                case ResidentState.AtSchool:
                    DoScheduledSchool(ref schedule, instance, citizenId, ref citizen);
                    return;

                case ResidentState.Shopping when schedule.WorkStatus == WorkStatus.Working:
                    DoScheduledLunch(ref schedule, instance, citizenId, ref citizen);
                    return;

                case ResidentState.Shopping:
                    executed = DoScheduledShopping(ref schedule, instance, citizenId, ref citizen);
                    break;

                case ResidentState.Relaxing:
                    executed = DoScheduledRelaxing(ref schedule, instance, citizenId, ref citizen);
                    break;

                case ResidentState.InShelter when schedule.CurrentState != ResidentState.InShelter:
                    DoScheduledEvacuation(ref schedule, instance, citizenId, ref citizen);
                    return;

                default:
                    return;
            }

            if (!executed && (schedule.CurrentState == ResidentState.AtSchoolOrWork || schedule.CurrentState == ResidentState.AtSchool || schedule.CurrentState == ResidentState.AtWork || schedule.CurrentState == ResidentState.InShelter))
            {
                schedule.Schedule(ResidentState.Unknown);
                DoScheduledHome(ref schedule, instance, citizenId, ref citizen);
            }
        }

        private bool ProcessCurrentState(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            switch (schedule.CurrentState)
            {
                case ResidentState.AtHome:
                    return RescheduleAtHome(ref schedule, citizenId, ref citizen);

                case ResidentState.Shopping:
                    return ProcessCitizenShopping(ref schedule, citizenId, ref citizen);

                case ResidentState.Relaxing:
                    return ProcessCitizenRelaxing(ref schedule, citizenId, ref citizen);

                case ResidentState.Visiting:
                    return ProcessCitizenVisit(ref schedule, instance, citizenId, ref citizen);

                case ResidentState.InShelter:
                    return ProcessCitizenInShelter(ref schedule, ref citizen);

                case ResidentState.AtSchoolOrWork when !CitizenProxy.HasFlags(ref citizen, Citizen.Flags.Student) && CitizenProxy.GetAge(ref citizen) != Citizen.AgeGroup.Child && CitizenProxy.GetAge(ref citizen) != Citizen.AgeGroup.Teen && Config.WorkForceMatters:
                case ResidentState.AtWork when Config.WorkForceMatters:
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

        private void ProcessVacation(uint citizenId)
        {
            ref var schedule = ref residentSchedules[citizenId];
            var citizen = CitizenManager.instance.m_citizens.m_buffer[citizenId];
            // Note: this might lead to different vacation durations for family members even if they all were initialized to same length.
            // This is because the simulation loop for a family member could process this citizen right after their vacation has been set.
            // But we intentionally don't avoid this - let's add some randomness.
            if (schedule.VacationDaysLeft > 0)
            {
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

            if ((citizen.m_flags & Citizen.Flags.Student) != 0 || Citizen.GetAgeGroup(citizen.m_age) == Citizen.AgeGroup.Child || Citizen.GetAgeGroup(citizen.m_age) == Citizen.AgeGroup.Teen)
            {
                if (schedule.SchoolBuilding == 0)
                {
                    return;
                }
            }
            else
            {
                if (schedule.WorkBuilding == 0)
                {
                    return;
                }
                // nobody working or on the way to work, and building is essential service
                if (IsEssentialService(schedule.WorkBuilding) && GetCitizensInWorkPlaceByShift(schedule.WorkBuilding, schedule.WorkShift) == 0 && Config.WorkForceMatters)
                {
                    return;
                }

            }

            int days = 1 + Random.GetRandomValue(Config.MaxVacationLength - 1);
            if ((citizen.m_flags & Citizen.Flags.Student) != 0 || Citizen.GetAgeGroup(citizen.m_age) == Citizen.AgeGroup.Child || Citizen.GetAgeGroup(citizen.m_age) == Citizen.AgeGroup.Teen)
            {
                schedule.SchoolStatus = SchoolStatus.OnVacation;
            }
            else
            {
                schedule.WorkStatus = WorkStatus.OnVacation;
            }
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
                    if ((CitizenManager.instance.m_citizens.m_buffer[familyMemberId].m_flags & Citizen.Flags.Student) != 0 || Citizen.GetAgeGroup(citizen.m_age) == Citizen.AgeGroup.Child || Citizen.GetAgeGroup(citizen.m_age) == Citizen.AgeGroup.Teen)
                    {
                        residentSchedules[familyMemberId].SchoolStatus = SchoolStatus.OnVacation;
                    }
                    else
                    {
                        residentSchedules[familyMemberId].WorkStatus = WorkStatus.OnVacation;
                    }
                    residentSchedules[familyMemberId].VacationDaysLeft = (byte)days;
                }
            }
        }

        // set work shift to the shift with the minimum number of people 
        public WorkShift SetWorkShift(ushort workBuilding)
        {
            if (workBuilding == 0 || !IsEssentialService(workBuilding))
            {
                return WorkShift.Unemployed;
            }

            uint[] workForce = buildingAI.GetBuildingWorkForce(workBuilding);

            var workTime = BuildingWorkTimeManager.GetBuildingWorkTime(workBuilding);

            if(workTime.HasContinuousWorkShift)
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

                        case WorkShift.ContinuousNight:
                            continuousNightShiftWorkers++;
                            break;

                        default:
                            break;
                    }
                }

                int minShift = Math.Min(continuousDayShiftWorkers, continuousNightShiftWorkers);

                if(minShift == continuousNightShiftWorkers)
                {
                    return WorkShift.ContinuousNight;
                }
                else if (minShift == continuousDayShiftWorkers)
                {
                    return WorkShift.ContinuousDay;
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

                        case WorkShift.Second:
                            secondShiftWorkers++;
                            break;

                        case WorkShift.Night when workTime.WorkAtNight == true:
                            nightShiftWorkers++;
                            break;

                        default:
                            break;
                    }
                }


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
            


        }
    }
}
