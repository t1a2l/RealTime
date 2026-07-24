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
                    if (schoolBehavior.ScheduleMeal(ref schedule, schedule.SchoolBuilding))
                    {
                        Log.Debug(LogCategory.Schedule, $"  - School time in {timeLeft} hours, going to eat {schedule.ScheduledMealType} in a shop or a cafeteria before heading to school");
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

            string citizenDesc = GetCitizenDesc(citizenId, ref citizen);

            if (residentAI.StartMoving(instance, citizenId, ref citizen, currentBuilding, schedule.SchoolBuilding))
            {
                if (schedule.CurrentState != ResidentState.AtHome)
                {
                    // The start moving method will register a departure from any building to school,
                    // but we are only interested in the 'home->school' route.
                    schedule.DepartureTime = default;
                }

                if (schoolBehavior.ScheduleMeal(ref schedule, schedule.SchoolBuilding))
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going from {currentBuilding} to school {schedule.SchoolBuilding} and will go to eat {schedule.ScheduledMealType} at {schedule.ScheduledStateTime:dd.MM.yy HH:mm}");
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
            string citizenDesc = GetCitizenDesc(citizenId, ref citizen);
            ushort mealPlace = 0;

            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[currentBuilding];
            if (building.Info.GetAI() is CampusBuildingAI || building.Info.GetAI() is UniqueFacultyAI)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} moving to cafeteria building to eat school meal and the ScheduledMealType is {schedule.ScheduledMealType}");
                mealPlace = MoveToCafeteriaBuilding(instance, citizenId, ref citizen, LocalSearchDistance);
            }

            if (mealPlace == 0)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} moving to commercial building to eat school meal and the ScheduledMealType is {schedule.ScheduledMealType}");
                mealPlace = MoveToCommercialBuilding(instance, citizenId, ref citizen, LocalSearchDistance, CommercialBuildingType.Food);
            }

            if (schedule.ScheduledMealType == MealType.None)
            {
                if (TimeInfo.CurrentHour >= Config.BreakfastBegin && TimeInfo.CurrentHour <= 10f)
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} - updating school none meal type to {MealType.Breakfast}");
                    schedule.UpdateMealType(MealType.Breakfast);
                }
                else if (TimeInfo.CurrentHour >= Config.LunchBegin && TimeInfo.CurrentHour <= 13f)
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} - updating school none meal type to {MealType.Lunch}");
                    schedule.UpdateMealType(MealType.Lunch);
                }
                else if (TimeInfo.CurrentHour >= Config.SupperBegin && TimeInfo.CurrentHour <= 20f)
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} - updating school none meal type to {MealType.Supper}");
                    schedule.UpdateMealType(MealType.Supper);
                }
            }
            else
            {
                if (mealPlace != 0)
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going from {currentBuilding} to eat {schedule.ScheduledMealType} at {mealPlace}");
                }
                else
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} wanted to go from {currentBuilding} to eat, but there were no food places close enough or open");
                }
            }
        }
    }

}
