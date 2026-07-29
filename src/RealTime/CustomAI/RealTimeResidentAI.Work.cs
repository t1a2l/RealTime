// RealTimeResidentAI.Work.cs

namespace RealTime.CustomAI
{
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
    }
}
