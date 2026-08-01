namespace RealTime.CustomAI
{
    using System;
    using ColossalFramework;
    using SkyTools.Tools;
    using static Constants;

    internal sealed partial class RealTimeResidentAI<TAI, TCitizen>
    {
        public bool ScheduleMeal(ref CitizenSchedule schedule, ref TCitizen citizen, bool workRealtedMeal = false, bool schoolRealtedMeal = false, DateTime departureTime = default)
        {
            var citizenAge = CitizenProxy.GetAge(ref citizen);

            bool isWorkOrSchool = workRealtedMeal || schoolRealtedMeal;

            if(!isWorkOrSchool)
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
                if (schedule.WorkStatus == WorkStatus.None && schedule.SchoolStatus == SchoolStatus.None)
                {
                    mealBehavior.GetMealDataByTimeOfDay(TimeInfo.CurrentHour, out var mealType, out float _, out float mealDuration);

                    if (!mealBehavior.ShouldScheduleWorkOrSchoolMeal(ref schedule, citizenAge, mealType))
                    {
                        return false;
                    }

                    var endMealTime = TimeInfo.Now.AddHours(mealDuration);

                    if(departureTime != default && departureTime <= endMealTime)
                    {
                        Log.Debug(LogCategory.Schedule, $"  - work/school citizen wanted to go to eat {mealType} at {TimeInfo.Now:dd.MM.yy HH:mm} but meal end time {endMealTime:dd.MM.yy HH:mm} is after departureTime {departureTime:dd.MM.yy HH:mm}");
                        return false;
                    }

                    if(workRealtedMeal)
                    {
                        schedule.Hint = ScheduleHint.WorkRelatedMeal;
                    }
                    else if (schoolRealtedMeal)
                    {
                        schedule.Hint = ScheduleHint.SchoolRelatedMeal;
                    }

                    schedule.Schedule(ResidentState.GoToMeal, mealType, endMealTime);
                    Log.Debug(LogCategory.Schedule, $"  - work/school citizen will go to eat {mealType} at {TimeInfo.Now:dd.MM.yy HH:mm} and will finish eating at {endMealTime:dd.MM.yy HH:mm}");
                    return true;
                }
                else
                {
                    mealBehavior.GetMealDataByTimeOfDay(TimeInfo.CurrentHour, out var mealType, out float mealBegin, out float mealDuration);

                    if (!mealBehavior.ShouldScheduleWorkOrSchoolMeal(ref schedule, citizenAge, mealType))
                    {
                        return false;
                    }

                    var MealBegin = TimeInfo.Now.Date.AddHours(mealBegin);
                    bool mealRange = (MealBegin - TimeInfo.Now).TotalHours >= 2.5;

                    if(mealRange)
                    {
                        if (workRealtedMeal)
                        {
                            schedule.Hint = ScheduleHint.WorkRelatedMeal;
                        }
                        else if (schoolRealtedMeal)
                        {
                            schedule.Hint = ScheduleHint.SchoolRelatedMeal;
                        }
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
                schedule.ResetScheduledStateTime();
            }

            if (schedule.Hint == ScheduleHint.LocalMealOnly)
            {
                schedule.Schedule(ResidentState.Unknown);

                if (CurrentBuildingSupportsTarget(currentBuilding, ref schedule))
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} stays in building {currentBuilding} for the purpose of eating {schedule.ScheduledMealType}");
                    return true;
                }

                if (TimeInfo.IsNightTime)
                {
                    schedule.Hint = ScheduleHint.NoMealAnyMore;
                }

                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} is going to eat {schedule.ScheduledMealType} at a local food place {mealPlace}");
                return true;
            }

            if (QuitVisit(citizenId, ref citizen, currentBuilding))
            {
                if (schedule.Hint == ScheduleHint.WorkRelatedMeal)
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} wanted to eat {schedule.ScheduledMealType} at {mealPlace} but it is closed - going to work");
                    schedule.Schedule(ResidentState.GoToWork);
                }
                else if (schedule.Hint == ScheduleHint.SchoolRelatedMeal)
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} wanted to eat {schedule.ScheduledMealType} at {mealPlace} but it is closed - going to school");
                    schedule.Schedule(ResidentState.GoToSchool);
                }
                else
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} wanted to eat {schedule.ScheduledMealType} at {mealPlace} but it is closed - find someplace else to go to");
                    schedule.Schedule(ResidentState.Unknown);
                }
                return false;
            }

            if (schedule.Hint == ScheduleHint.WorkRelatedMeal)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} is going from {currentBuilding} to eat {schedule.ScheduledMealType} at {mealPlace} and will go to work at {schedule.ScheduledMealEndTime:dd.MM.yy HH:mm}");
                schedule.Schedule(ResidentState.GoToWork, schedule.ScheduledMealEndTime);
            }
            else if (schedule.Hint == ScheduleHint.SchoolRelatedMeal)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} is going from {currentBuilding} to eat {schedule.ScheduledMealType} at {mealPlace} and will go to school at {schedule.ScheduledMealEndTime:dd.MM.yy HH:mm}");
                schedule.Schedule(ResidentState.GoToSchool, schedule.ScheduledMealEndTime);
            }
            else
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} is going from {currentBuilding} to eat {schedule.ScheduledMealType} at {mealPlace} and will finish eating at {schedule.ScheduledMealEndTime:dd.MM.yy HH:mm}");
                schedule.Schedule(ResidentState.Unknown, schedule.ScheduledMealEndTime);
            }

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
