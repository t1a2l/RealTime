// RealTimeResidentAI.Work.cs

namespace RealTime.CustomAI
{
    using System;
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
            Log.Debug(LogCategory.Schedule, $"  - departureTime: {departureTime}, TimeInfo.Now: {TimeInfo.Now:dd.MM.yy HH:mm} and timeLeft: {timeLeft}");

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

                if (ScheduleMeal(ref schedule, ref citizen, true, false, departureTime))
                {
                    Log.Debug(LogCategory.Schedule, $"  - Work time in {timeLeft} hours, going to eat {schedule.ScheduledMealType} and will go to work at {schedule.ScheduledMealEndTime:dd.MM.yy HH:mm}");
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

            string citizenDesc = GetCitizenDesc(citizenId, ref citizen);

            if (residentAI.StartMoving(instance, citizenId, ref citizen, currentBuilding, schedule.WorkBuilding))
            {
                if (schedule.CurrentState != ResidentState.AtHome)
                {
                    // The start moving method will register a departure from any building to work,
                    // but we are only interested in the 'home->work' route.
                    schedule.DepartureTime = default;
                }

                if (ScheduleMeal(ref schedule, ref citizen, true, false, default))
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going from {currentBuilding} to work {schedule.WorkBuilding} and will go to eat {schedule.ScheduledMealType} at {schedule.ScheduledStateTime:dd.MM.yy HH:mm}");
                }
                else
                {
                    workBehavior.ScheduleReturnFromWork(citizenId, ref schedule, CitizenProxy.GetAge(ref citizen));
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going from {currentBuilding} to work {schedule.WorkBuilding} and will leave work at {schedule.ScheduledStateTime:dd.MM.yy HH:mm}");
                }
            }
            else
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted to go to work from {currentBuilding} but can't, will try once again next time");
                schedule.Schedule(ResidentState.Unknown);
            }
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

        private bool ProcessCitizenWork(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            return RescheduleReturnFromWork(ref schedule, citizenId, ref citizen, currentBuilding);
        }

        private bool RescheduleReturnFromWork(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen, ushort currentBuilding)
        {
            if (buildingAI.IsBuildingOpeningSoon(currentBuilding, 1))
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} stay in work because the building is opening soon");
                return false;
            }

            if (!buildingAI.IsBuildingWorking(currentBuilding))
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} return from work because the building is currently closed");
                return true;
            }
            //if (Config.WorkForceMatters && ShouldReturnFromWork(ref schedule, citizenId, ref citizen, currentBuilding))
            //{
            //    workBehavior.ScheduleReturnFromWork(citizenId, ref schedule, CitizenProxy.GetAge(ref citizen));
            //    return true;
            //}

            return false;
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


            int index = Array.IndexOf(workTime.WorkShifts, schedule.ShiftIndex);

            if (index == workTime.WorkShifts.Length - 1)
            {
                // if the current shift is the last one, the next shift will be the first one
                index = 0;
            }
            else
            {
                index++;
            }

            // get the building work force 
            uint[] workforce = buildingAI.GetBuildingWorkForce(currentBuildingId);

            for (int i = 0; i < workforce.Length; i++)
            {
                // check if all people from the next shift that are not on vacation has arrived
                var citizen_schedule = GetCitizenSchedule(workforce[i]);
                if(citizen_schedule.ShiftIndex == index && citizen_schedule.WorkStatus == WorkStatus.Working)
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

    }
}
