namespace RealTime.CustomAI
{
    using System;
    using ColossalFramework;
    using SkyTools.Tools;
    using static Constants;

    internal sealed partial class RealTimeResidentAI<TAI, TCitizen>
    {
        public bool ScheduleMeal(ref CitizenSchedule schedule, ref TCitizen citizen, bool workOrSchoolRealtedMeal = false, DateTime departureTime = default)
        {
            var citizenAge = CitizenProxy.GetAge(ref citizen);

            if (!workOrSchoolRealtedMeal)
            {
                mealBehavior.GetMealDataByTimeOfDay(TimeInfo.CurrentHour, out var mealType, out float _, out float mealDuration);

                if (!mealBehavior.ShouldScheduleMeal(ref schedule, citizenAge, mealType))
                {
                    return false;
                }

                var endMealTime = TimeInfo.Now.AddHours(mealDuration);
                schedule.Schedule(ResidentState.GoToMeal, mealType, endMealTime);
                Log.Debug(LogCategory.Schedule, $"  - citizen will go to eat {mealType} at {TimeInfo.Now:dd.MM.yy HH:mm} and will finish eating at {endMealTime:dd.MM.yy HH:mm}");
                return true;
            }
            else
            {

                if (schedule.WorkStatus == WorkStatus.None || schedule.SchoolStatus == SchoolStatus.None)
                {
                    mealBehavior.GetMealDataByTimeOfDay(TimeInfo.CurrentHour, out var mealType, out float _, out float mealDuration);

                    if (!mealBehavior.ShouldScheduleWorkOrSchoolMeal(ref schedule, citizenAge, mealType))
                    {
                        return false;
                    }

                    var endMealTime = TimeInfo.Now.AddHours(mealDuration);

                    if (departureTime != default && departureTime <= endMealTime)
                    {
                        Log.Debug(LogCategory.Schedule, $"  - work/school citizen wanted to go to eat {mealType} but meal end time {endMealTime:dd.MM.yy HH:mm} is after departureTime {departureTime:dd.MM.yy HH:mm}");
                        return false;
                    }

                    schedule.Schedule(ResidentState.GoToMeal, mealType, endMealTime);
                    Log.Debug(LogCategory.Schedule, $"  - citizen will go to eat {mealType} at {TimeInfo.Now:dd.MM.yy HH:mm} and will finish eating at {endMealTime:dd.MM.yy HH:mm}");
                    return true;
                }
                else if (schedule.WorkStatus == WorkStatus.Working || schedule.SchoolStatus == SchoolStatus.Studying)
                {
                    if (!mealBehavior.TryGetBestWorkOrSchoolMealOpportunity(ref schedule, TimeInfo.Now, out var mealOpportunity))
                    {
                        return false;
                    }

                    if (departureTime != default && departureTime <= mealOpportunity.EndTime)
                    {
                        Log.Debug(LogCategory.Schedule, $"  - work/school citizen wanted to go to eat {mealOpportunity.MealType} but meal end time {mealOpportunity.EndTime:dd.MM.yy HH:mm} is after departureTime {departureTime:dd.MM.yy HH:mm}");
                        return false;
                    }

                    schedule.Hint = ScheduleHint.WorkOrSchoolRelatedMeal;
                    schedule.Schedule(ResidentState.GoToMeal, mealOpportunity.BeginTime, mealOpportunity.MealType, mealOpportunity.EndTime);

                    Log.Debug(LogCategory.Schedule,$"  - work/school citizen will go to eat {mealOpportunity.MealType} at {mealOpportunity.BeginTime:dd.MM.yy HH:mm} and will finish eating at {mealOpportunity.EndTime:dd.MM.yy HH:mm}");
                    return true;
                }

                return false;
            }
        }


        public bool DoScheduledMeal(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            string citizenDesc = GetCitizenDesc(citizenId, ref citizen);
            ushort mealPlace = FindMealPlace(ref schedule, instance, citizenId, ref citizen);

            if (mealPlace == 0)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} wanted to go from {currentBuilding} to eat, but there were no food places close enough or open");
                return false;
            }

            if (schedule.ScheduledMealType == MealType.None)
            {
                mealBehavior.UpdateMealTypeByTimeOfDay(citizenId, ref schedule);
                schedule.ResetScheduledStateTime();
            }

            if (schedule.Hint == ScheduleHint.LocalMealOnly)
            {
                if (CurrentBuildingSupportsTarget(currentBuilding, ref schedule))
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} stays in building {currentBuilding} for the purpose of eating {schedule.ScheduledMealType}");
                    return true;
                }

                if (TimeInfo.IsNightTime)
                {
                    schedule.Hint = ScheduleHint.NoMealAnyMore;
                }

                schedule.ResetDailyMealsIfNeeded(TimeInfo.Now.DayOfYear);
                schedule.MarkMealConsumedToday(schedule.ScheduledMealType);
                schedule.MealsEatenOutToday++;
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} is going to eat {schedule.ScheduledMealType} at a local food place {mealPlace}");
                return true;
            }

            if (QuitVisit(citizenId, ref citizen, currentBuilding))
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} wanted to eat {schedule.ScheduledMealType} at {mealPlace} but it is closed - find someplace else to go to");
                schedule.Schedule(ResidentState.Unknown);
                return false;
            }

            if (schedule.Hint != ScheduleHint.WorkOrSchoolRelatedMeal)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} is going from {currentBuilding} to eat {schedule.ScheduledMealType} at {mealPlace} and will finish eating at {schedule.ScheduledMealEndTime:dd.MM.yy HH:mm}");
                schedule.Schedule(ResidentState.Unknown, schedule.ScheduledMealEndTime);
            }

            schedule.ResetDailyMealsIfNeeded(TimeInfo.Now.DayOfYear);
            schedule.MarkMealConsumedToday(schedule.ScheduledMealType);
            schedule.MealsEatenOutToday++;
            return true;
        }


        public ushort FindMealPlace(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[currentBuilding];
            ushort mealPlace = 0;
            if (building.Info.GetAI() is CampusBuildingAI || building.Info.GetAI() is UniqueFacultyAI)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} moving to cafeteria building to eat meal and the ScheduledMealType is {schedule.ScheduledMealType}");
                mealPlace = MoveToCafeteriaBuilding(instance, citizenId, ref citizen, LocalSearchDistance);
            }

            if (mealPlace == 0)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} moving to commercial building to eat meal and the ScheduledMealType is {schedule.ScheduledMealType}");
                mealPlace = MoveToCommercialBuilding(instance, citizenId, ref citizen, LocalSearchDistance, CommercialBuildingType.Food);
            }

            return mealPlace;
        }
    }
}
