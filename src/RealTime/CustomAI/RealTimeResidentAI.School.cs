// RealTimeResidentAI.School.cs

namespace RealTime.CustomAI
{
    using ColossalFramework;
    using SkyTools.Tools;
    using static Constants;

    internal sealed partial class RealTimeResidentAI<TAI, TCitizen>
    {
        private bool ScheduleSchool(ref CitizenSchedule schedule, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            if (!schoolBehavior.ShouldScheduleGoToSchool(ref schedule))
            {
                return false;
            }

            var departureTime = schoolBehavior.ScheduleGoToSchoolTime(ref schedule, currentBuilding, simulationCycle);

            float timeLeft = (float)(departureTime - TimeInfo.Now).TotalHours;
            Log.Debug(LogCategory.Schedule, $"  - departureTime: {departureTime}, TimeInfo.Now: {TimeInfo.Now} and timeLeft: {timeLeft}");

            if (timeLeft <= PrepareToSchoolHours)
            {
                Log.Debug(LogCategory.Schedule, $"  - Schedule school at {departureTime:dd.MM.yy HH:mm}");
                schedule.Schedule(ResidentState.GoToSchool, departureTime);
                // Just sit at home if the school time will come soon
                Log.Debug(LogCategory.Schedule, $"  - School time in {timeLeft} hours, preparing for departure");
                return true;
            }

            if (timeLeft <= MaxTravelTime)
            {
                Log.Debug(LogCategory.Schedule, $"  - Schedule school at {departureTime:dd.MM.yy HH:mm}");
                schedule.Schedule(ResidentState.GoToSchool, departureTime);

                if (schedule.CurrentState != ResidentState.AtHome)
                {
                    Log.Debug(LogCategory.Schedule, $"  - School time in {timeLeft} hours, returning home");
                    schedule.Schedule(ResidentState.GoHome);
                    return true;
                }

                var age = CitizenProxy.GetAge(ref citizen);
                if(age == Citizen.AgeGroup.Young || age == Citizen.AgeGroup.Adult)
                {
                    if (schoolBehavior.ScheduleMeal(ref schedule, schedule.SchoolBuilding, MealType.Breakfast))
                    {
                        Log.Debug(LogCategory.Schedule, $"  - School time in {timeLeft} hours, going to eat breakfast in a shop or a cafeteria before heading to school");
                        return true;
                    }

                    // If we have some time, try to shop locally.
                    if (ScheduleShopping(ref schedule, ref citizen, localOnly: false, localOnlyWork: false, localOnlySchool: true))
                    {
                        Log.Debug(LogCategory.Schedule, $"  - University time in {timeLeft} hours, trying local shop");
                    }
                    else
                    {
                        Log.Debug(LogCategory.Schedule, $"  - University time in {timeLeft} hours, doing nothing");
                    }
                }
                return true;
            }

            return false;
        }

        private void DoScheduledSchool(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            schedule.SchoolStatus = SchoolStatus.Studying;

            if (currentBuilding == schedule.SchoolBuilding && schedule.CurrentState != ResidentState.AtSchool)
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

            if (residentAI.StartMoving(instance, citizenId, ref citizen, currentBuilding, schedule.SchoolBuilding))
            {
                if (schedule.CurrentState != ResidentState.AtHome)
                {
                    // The start moving method will register a departure from any building to school,
                    // but we are only interested in the 'home->school' route.
                    schedule.DepartureTime = default;
                }

                if (schoolBehavior.ScheduleMeal(ref schedule, schedule.SchoolBuilding, MealType.Lunch))
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going from {currentBuilding} to school {schedule.SchoolBuilding} and will go to lunch at {schedule.ScheduledStateTime:dd.MM.yy HH:mm}");
                }
                else
                {
                    schoolBehavior.ScheduleReturnFromSchool(citizenId, ref schedule);
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going from {currentBuilding} to school {schedule.SchoolBuilding} and will leave school at {schedule.ScheduledStateTime:dd.MM.yy HH:mm}");
                }
            }    
            else
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted to go to school from {currentBuilding} but can't, will try once again next time");
                schedule.Schedule(ResidentState.Unknown);
            }
        }

        private void DoScheduledSchoolMeal(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
#if DEBUG
            string citizenDesc = GetCitizenDesc(citizenId, ref citizen);
#endif
            ushort mealPlace = 0;

            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[currentBuilding];
            if (building.Info.GetAI() is CampusBuildingAI || building.Info.GetAI() is UniqueFacultyAI)
            {
                mealPlace = MoveToCafeteriaBuilding(instance, citizenId, ref citizen, LocalSearchDistance);
            }

            if (mealPlace == 0)
            {
                mealPlace = MoveToCommercialBuilding(instance, citizenId, ref citizen, LocalSearchDistance, CommercialBuildingType.Food);
            }

            if (schedule.ScheduledMealType == MealType.Breakfast)
            {
                if (mealPlace != 0)
                {
#if DEBUG
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going for before work breakfast from {currentBuilding} to {mealPlace}");
#endif
                    var departureTime = schoolBehavior.ScheduleGoToSchoolTime(ref schedule, mealPlace, simulationCycle);
                    schedule.Schedule(ResidentState.GoToSchool, departureTime);
                }
                else
                {
#if DEBUG
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} wanted to go for before work breakfast from {currentBuilding}, but there were no buildings close enough or open");
#endif
                    var departureTime = schoolBehavior.ScheduleGoToSchoolTime(ref schedule, currentBuilding, simulationCycle);
                    schedule.Schedule(ResidentState.GoToSchool, departureTime);
                }
            }
            else if (schedule.ScheduledMealType == MealType.Lunch)
            {
                if (mealPlace != 0)
                {
#if DEBUG
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going for during work lunch from {currentBuilding} to {mealPlace}");
#endif
                    schoolBehavior.ScheduleReturnFromMeal(ref schedule);
                }
                else
                {
#if DEBUG
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} wanted to go for during work lunch from {currentBuilding}, but there were no buildings close enough or open");
#endif
                    schoolBehavior.ScheduleReturnFromSchool(citizenId, ref schedule);
                }
            }
            else if (schedule.ScheduledMealType == MealType.Supper)
            {
                if (mealPlace != 0)
                {
#if DEBUG
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going for after work supper from {currentBuilding} to {mealPlace}");
#endif
                }
                else
                {
#if DEBUG
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} wanted to go for after work supper from {currentBuilding}, but there were no buildings close enough or open");
#endif
                }
            }
        }
    }

}
