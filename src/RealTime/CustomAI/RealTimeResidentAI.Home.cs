// RealTimeResidentAI.Home.cs

namespace RealTime.CustomAI
{
    using SkyTools.Tools;

    internal sealed partial class RealTimeResidentAI<TAI, TCitizen>
    {
        private void DoScheduledHome(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort homeBuilding = CitizenProxy.GetHomeBuilding(ref citizen);
            if (homeBuilding == 0)
            {
                Log.Debug(LogCategory.State, $"{GetCitizenDesc(citizenId, ref citizen)} is currently homeless. Cannot move home, waiting for the next opportunity");
                return;
            }

            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            CitizenProxy.RemoveFlags(ref citizen, Citizen.Flags.Evacuating);

            if (residentAI.StartMoving(instance, citizenId, ref citizen, currentBuilding, homeBuilding))
            {
                CitizenProxy.SetVisitPlace(ref citizen, citizenId, 0);
                schedule.Schedule(ResidentState.Unknown);
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} is going from {currentBuilding} back home to {homeBuilding}");
            }
            else
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted to go home from {currentBuilding} but can't, waiting for the next opportunity");
            }
        }

        private bool RescheduleAtHome(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen, bool noReschedule)
        {
            if (schedule.CurrentState != ResidentState.AtHome || TimeInfo.Now < schedule.ScheduledStateTime)
            {
                return false;
            }

            if (schedule.ScheduledState != ResidentState.GoToRelax && schedule.ScheduledState != ResidentState.GoShopping && 
                schedule.ScheduledState != ResidentState.GoToMeal)
            {
                return false;
            }

            if ((schedule.ScheduledState != ResidentState.GoShopping || schedule.ScheduledState != ResidentState.GoToMeal) && WeatherInfo.IsBadWeather)
            {
                Log.Debug(LogCategory.Schedule, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} re-schedules an activity because of bad weather");
                if(!noReschedule)
                {
                    schedule.Schedule(ResidentState.Unknown);
                }
                return true;
            }

            var age = CitizenProxy.GetAge(ref citizen);
            uint goingOutChance = schedule.ScheduledState == ResidentState.GoShopping
                ? spareTimeBehavior.GetShoppingChance(age)
                : spareTimeBehavior.GetRelaxingChance(age, schedule.WorkShift);

            if (goingOutChance > 0)
            {
                return false;
            }

            if(schedule.ScheduledState == ResidentState.GoToMeal)
            {
                return false;
            }

            Log.Debug(LogCategory.Schedule, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} re-schedules an activity because of time");
            if (!noReschedule)
            {
                schedule.Schedule(ResidentState.Unknown);
            }
            return true;
        }
    }
}
