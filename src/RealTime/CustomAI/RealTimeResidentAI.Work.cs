// RealTimeResidentAI.Work.cs

namespace RealTime.CustomAI
{
    using ColossalFramework;
    using RealTime.Managers;
    using SkyTools.Tools;
    using static Constants;

    internal sealed partial class RealTimeResidentAI<TAI, TCitizen>
    {
        private bool ScheduleWork(ref CitizenSchedule schedule, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            if (!workBehavior.ShouldScheduleGoToWork(ref schedule))
            {
                return false;
            }

            var departureTime = workBehavior.ScheduleGoToWorkTime(ref schedule, currentBuilding, simulationCycle);

            float timeLeft = (float)(departureTime - TimeInfo.Now).TotalHours;
            Log.Debug(LogCategory.Schedule, $"  - departureTime: {departureTime}, TimeInfo.Now: {TimeInfo.Now} and timeLeft: {timeLeft}");

            if (timeLeft <= PrepareToWorkHours)
            {
                Log.Debug(LogCategory.Schedule, $"  - Schedule work at {departureTime:dd.MM.yy HH:mm}");
                schedule.Schedule(ResidentState.GoToWork, departureTime);
                // Just sit at home if the work time will come soon
                Log.Debug(LogCategory.Schedule, $"  - Work time in {timeLeft} hours, preparing for departure");
                return true;
            }

            if (timeLeft <= MaxTravelTime)
            {
                Log.Debug(LogCategory.Schedule, $"  - Schedule work at {departureTime:dd.MM.yy HH:mm}");
                schedule.Schedule(ResidentState.GoToWork, departureTime);

                if (schedule.CurrentState != ResidentState.AtHome)
                {
                    Log.Debug(LogCategory.Schedule, $"  - Work time in {timeLeft} hours, returning home");
                    schedule.Schedule(ResidentState.GoHome);
                    return true;
                }

                var citizenAge = CitizenProxy.GetAge(ref citizen);
                if (workBehavior.ScheduleBreakfast(ref schedule, citizenAge))
                {
                    Log.Debug(LogCategory.Schedule, $"  - Work time in {timeLeft} hours, going to eat breakfast in a shop before heading to work");
                    return true;
                }

                // If we have some time, try to shop locally.
                if (ScheduleShopping(ref schedule, ref citizen, localOnly: false, localOnlyWork: true, localOnlySchool: false))
                {
                    Log.Debug(LogCategory.Schedule, $"  - Work time in {timeLeft} hours, trying local shop");
                }
                else
                {
                    Log.Debug(LogCategory.Schedule, $"  - Work time in {timeLeft} hours, doing nothing");
                }

                return true;
            }

            return false;
        }

        private void DoScheduledWork(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            schedule.WorkStatus = WorkStatus.Working;

            if (currentBuilding == schedule.WorkBuilding && schedule.CurrentState != ResidentState.AtWork && schedule.ScheduledState != ResidentState.GoToWork) // to check
            {
                CitizenProxy.SetVisitPlace(ref citizen, citizenId, 0);
                CitizenProxy.SetLocation(ref citizen, Citizen.Location.Work);
                return;
            }
#if DEBUG
            string citizenDesc = GetCitizenDesc(citizenId, ref citizen);
#else
            const string citizenDesc = null;
#endif
            if (residentAI.StartMoving(instance, citizenId, ref citizen, currentBuilding, schedule.WorkBuilding))
            {
                if (schedule.CurrentState != ResidentState.AtHome)
                {
                    // The start moving method will register a departure from any building to work,
                    // but we are only interested in the 'home->work' route.
                    schedule.DepartureTime = default;
                }

                var citizenAge = CitizenProxy.GetAge(ref citizen);
                if (workBehavior.ScheduleLunch(ref schedule, citizenAge))
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going from {currentBuilding} to work {schedule.WorkBuilding} and will go to lunch at {schedule.ScheduledStateTime:dd.MM.yy HH:mm}");
                }
                else
                {

                    if (!Config.WorkForceMatters)
                    {
                        workBehavior.ScheduleReturnFromWork(citizenId, ref schedule, CitizenProxy.GetAge(ref citizen));
                        Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going from {currentBuilding} to work {schedule.WorkBuilding} and will leave work at {schedule.ScheduledStateTime:dd.MM.yy HH:mm}");
                    }
                    else
                    {
                        Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going from {currentBuilding} to work {schedule.WorkBuilding}");
                    }
                }
            }
            else
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted to go to work from {currentBuilding} but can't, will try once again next time");
                schedule.Schedule(ResidentState.Unknown);
            }
        }

        private void DoScheduledBreakfast(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
#if DEBUG
            string citizenDesc = GetCitizenDesc(citizenId, ref citizen);
#endif
            ushort breakfastPlace = MoveToCommercialBuilding(instance, citizenId, ref citizen, LocalSearchDistance * 4, false);
            if (breakfastPlace != 0)
            {
#if DEBUG
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going for breakfast from {currentBuilding} to {breakfastPlace}");
#endif
                var departureTime = workBehavior.ScheduleGoToWorkTime(ref schedule, breakfastPlace, simulationCycle);

                schedule.Schedule(ResidentState.GoToWork, departureTime);
            }
            else
            {
#if DEBUG
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} wanted to go for breakfast from {currentBuilding}, but there were no buildings close enough or open");

                var departureTime = workBehavior.ScheduleGoToWorkTime(ref schedule, currentBuilding, simulationCycle);

                schedule.Schedule(ResidentState.GoToWork, departureTime);
#endif
            }

        }

        private void DoScheduledWorkLunch(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
#if DEBUG
            string citizenDesc = GetCitizenDesc(citizenId, ref citizen);
#endif
            // no one it worked besides me
            if(buildingAI.GetWorkersInBuilding(currentBuilding) <= 1 && Config.WorkForceMatters)
            {
#if DEBUG
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} wanted to go for lunch from {currentBuilding}, but there is no one at work to cover his shift");
#endif
            }
            else
            {
                bool found_cafeteria = false;
                var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[currentBuilding];
                if(building.Info.GetAI() is CampusBuildingAI || building.Info.GetAI() is UniqueFacultyAI)
                {
                    ushort lunchPlace = MoveToCafeteriaBuilding(instance, citizenId, ref citizen, LocalSearchDistance);
                    if (lunchPlace != 0)
                    {
#if DEBUG
                        Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going for lunch from {currentBuilding} to {lunchPlace}");
#endif
                        workBehavior.ScheduleReturnFromLunch(ref schedule);
                        found_cafeteria = true;
                    }
                }

                if(!found_cafeteria)
                {
                    ushort lunchPlace = MoveToCommercialBuilding(instance, citizenId, ref citizen, LocalSearchDistance, false);
                    if (lunchPlace != 0)
                    {
#if DEBUG
                        Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going for lunch from {currentBuilding} to {lunchPlace}");
#endif
                        workBehavior.ScheduleReturnFromLunch(ref schedule);
                    }
                    else
                    {
#if DEBUG
                        Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} wanted to go for lunch from {currentBuilding}, but there were no buildings close enough or open");
#endif
                        if (!Config.WorkForceMatters)
                        {
                            workBehavior.ScheduleReturnFromWork(citizenId, ref schedule, CitizenProxy.GetAge(ref citizen));
                        }
                    }
                }                
            }
        }

        private bool ProcessCitizenWork(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            return RescheduleReturnFromWork(ref schedule, citizenId, ref citizen, currentBuilding);
        }

        private bool RescheduleReturnFromWork(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen, ushort currentBuilding)
        {
            if (!buildingAI.IsBuildingWorking(currentBuilding, 1))
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} return from work because the building is currently closed");
                return true;
            }
            if (Config.WorkForceMatters && ShouldReturnFromWork(ref schedule, citizenId, ref citizen, currentBuilding))
            {
                workBehavior.ScheduleReturnFromWork(citizenId, ref schedule, CitizenProxy.GetAge(ref citizen));
                return true;
            }

            return false;
        }

        public bool IsEssentialService(ushort buildingId)
        {
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingId];
            var service = building.Info.m_class.m_service;
            var sub_service = building.Info.m_class.m_subService;
            switch (service)
            {
                case ItemClass.Service.Electricity:
                case ItemClass.Service.Water:
                case ItemClass.Service.HealthCare:
                case ItemClass.Service.PoliceDepartment when sub_service != ItemClass.SubService.PoliceDepartmentBank:
                case ItemClass.Service.FireDepartment:
                case ItemClass.Service.PublicTransport:
                case ItemClass.Service.Disaster:
                case ItemClass.Service.Natural:
                case ItemClass.Service.Garbage:
                case ItemClass.Service.Road:
                case ItemClass.Service.Hotel:
                case ItemClass.Service.ServicePoint:
                    return true;

                default:
                    return false;
            }
        }

        private bool ShouldReturnFromWork(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen, ushort currentBuildingId)
        {
            // work place data
            BuildingWorkTimeManager.WorkTime workTime;

            var currentBuilding = BuildingManager.instance.m_buildings.m_buffer[currentBuildingId];

            if (!BuildingWorkTimeManager.BuildingWorkTimeExist(currentBuildingId))
            {
                if (!BuildingWorkTimeManager.ShouldHaveBuildingWorkTime(schedule.WorkBuilding))
                {
                    return true;
                }
                workTime = BuildingWorkTimeManager.CreateBuildingWorkTime(currentBuildingId, currentBuilding.Info);
            }
            else
            {
                workTime = BuildingWorkTimeManager.GetBuildingWorkTime(currentBuildingId);
            }

            // building that are required for city operations - must wait for the next shift to arrive
            if (!IsEssentialService(currentBuildingId))
            {
                return true;
            }

            // find the next work shift of the work place
            WorkShift workShiftToFind;

            switch (schedule.WorkShift)
            {
                case WorkShift.First when workTime.WorkShifts == 2 && !workTime.HasContinuousWorkShift:
                    workShiftToFind = WorkShift.Second;
                    break;

                case WorkShift.Second when workTime.WorkShifts == 3:
                    workShiftToFind = WorkShift.Night;
                    break;

                case WorkShift.Night:
                    workShiftToFind = WorkShift.First;
                    break;

                case WorkShift.ContinuousDay when workTime.WorkShifts == 2 && workTime.HasContinuousWorkShift:
                    workShiftToFind = WorkShift.ContinuousNight;
                    break;

                case WorkShift.ContinuousNight:
                    workShiftToFind = WorkShift.ContinuousDay;
                    break;

                default:
                    return true;
            }

            
            // get the building work force 
            uint[] workforce = buildingAI.GetBuildingWorkForce(currentBuildingId);

            for (int i = 0; i < workforce.Length; i++)
            {
                // check if all people from the next shift that are not on vacation has arrived
                var citizen_schedule = GetCitizenSchedule(workforce[i]);
                if(citizen_schedule.WorkShift == workShiftToFind && citizen_schedule.WorkStatus == WorkStatus.Working)
                {
                    ref var nextShiftCitizen = ref CitizenManager.instance.m_citizens.m_buffer[workforce[i]];

                    if (nextShiftCitizen.CurrentLocation != Citizen.Location.Work)
                    {
                        // do not leave work until next shift has arrived
                        return false;
                    }
                }
                // no one from the next shift was found - stay at work until new workers are assigned
                if (i == workforce.Length - 1)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Return the number of citizens in a shift at a workplace</summary>
        /// <param name="currentBuilding">The ID of the building where the citizens work.</param>
        /// <param name="workShift">The work shift that citizens work in.</param>
        /// <returns>number of citizens in the work place by in a given work shift.</returns>
        private int GetCitizensInWorkPlaceByShift(ushort currentBuilding, WorkShift workShift)
        {
            var instance = Singleton<CitizenManager>.instance;
            // get work building entire work force
            uint[] workforce = buildingAI.GetBuildingWorkForce(currentBuilding);
            int count = 0;

            for (int i = 0; i < workforce.Length; i++)
            {
                var citizen_schedule = GetCitizenSchedule(workforce[i]);

                var citizen = CitizenManager.instance.m_citizens.m_buffer[workforce[i]];

                ushort citizen_instance = instance.m_citizens.m_buffer[workforce[i]].m_instance;

                // if it is work time for the citizen in the work force and citzen work building is the current building and he has working hours
                if(TimeInfo.CurrentHour > citizen_schedule.WorkShiftStartHour && TimeInfo.CurrentHour < citizen_schedule.WorkShiftEndHour
                    && citizen_schedule.WorkShift == workShift && citizen.m_workBuilding == currentBuilding && citizen_schedule.WorkStatus == WorkStatus.Working)
                {
                    // citizen is working
                    if (citizen.CurrentLocation == Citizen.Location.Work)
                    {
                        count++;
                    }
                    // citizen is on the way to work
                    else if (citizen.CurrentLocation == Citizen.Location.Moving && instance.m_instances.m_buffer[citizen_instance].m_targetBuilding == currentBuilding)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

    }
}
