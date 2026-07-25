namespace RealTime.CustomAI
{
    using ColossalFramework;
    using SkyTools.Tools;
    using static Constants;

    internal sealed partial class RealTimeResidentAI<TAI, TCitizen>
    {
        public bool ScheduleMeal(ref CitizenSchedule schedule, ref TCitizen citizen, bool isWorkOrSchool)
        {
            var citizenAge = CitizenProxy.GetAge(ref citizen);

            mealBehavior.GetMealDataByTimeOfDay(out var mealType, out float mealBegin, out float mealDuration);

            if (!mealBehavior.ShouldScheduleGoToMeal(ref schedule, citizenAge, mealType, isWorkOrSchool))
            {
                return false;
            }

            if(!isWorkOrSchool)
            {
                var endMealTime = TimeInfo.Now.AddHours(mealDuration);
                schedule.Schedule(ResidentState.GoToMeal, mealType, endMealTime);
                Log.Debug(LogCategory.Schedule, $"  - citizen will go to eat {mealType} at {TimeInfo.Now:dd.MM.yy HH:mm} and will finish eating at {endMealTime:dd.MM.yy HH:mm}");
                return true;
            }
            else
            {
                if (schedule.WorkStatus == WorkStatus.None && schedule.SchoolStatus == SchoolStatus.None)
                {
                    var endMealTime = TimeInfo.Now.AddHours(mealDuration);
                    schedule.Schedule(ResidentState.GoToMeal, mealType, endMealTime);
                    Log.Debug(LogCategory.Schedule, $"  - work/school citizen will go to eat {mealType} at {TimeInfo.Now:dd.MM.yy HH:mm} and will finish eating at {endMealTime:dd.MM.yy HH:mm}");
                    return true;
                }
                else
                {
                    var MealBegin = TimeInfo.Now.Date.AddHours(mealBegin);
                    bool mealRange = (MealBegin - TimeInfo.Now).TotalHours >= 2.5;

                    if(mealRange)
                    {
                        var endMealTime = MealBegin.AddHours(mealDuration);
                        schedule.Schedule(ResidentState.GoToMeal, MealBegin, mealType, endMealTime);
                        Log.Debug(LogCategory.Schedule, $"  - work/school citizen will go to eat {mealType} at {MealBegin:dd.MM.yy HH:mm} and will finish eating at {endMealTime:dd.MM.yy HH:mm}");
                        return true;
                    }

                    return false;
                }
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
            }

            if (schedule.Hint == ScheduleHint.LocalMealOnly)
            {
                if (CurrentBuildingSupportsTarget(currentBuilding, ref schedule))
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} stays in building {currentBuilding} for the purpose of eating {schedule.LastScheduledMealType}");
                    return true;
                }

                if (TimeInfo.IsNightTime)
                {
                    schedule.Hint = ScheduleHint.NoMealAnyMore;
                }

                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} going to eat {schedule.LastScheduledMealType} at a local food place {mealPlace}");
                return true;
            }

            if (QuitVisit(citizenId, ref citizen, currentBuilding))
            {
                if (schedule.WorkStatus == WorkStatus.Working)
                {
                    schedule.Schedule(ResidentState.GoToWork);
                }
                else if (schedule.SchoolStatus == SchoolStatus.Studying)
                {
                    schedule.Schedule(ResidentState.GoToSchool);
                }
                else
                {
                    schedule.Schedule(ResidentState.Unknown);
                }
                return false;
            }

            Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going from {currentBuilding} to eat {schedule.ScheduledMealType} at {mealPlace}");
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
