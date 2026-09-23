namespace RealTime.CustomAI
{
    using System;
    using ColossalFramework;
    using RealTime.Managers;
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

                var mealEndTime = TimeInfo.Now.AddHours(mealDuration);
                schedule.Schedule(ResidentState.GoToMeal, mealType, mealEndTime);
                Log.Debug(LogCategory.Schedule, $"  - citizen will go to eat {mealType} at {TimeInfo.Now:dd.MM.yy HH:mm} and will finish eating at {mealEndTime:dd.MM.yy HH:mm}");
                return true;
            }
            else
            {
                bool isCurrentlyAtWorkOrSchool = schedule.WorkStatus == WorkStatus.Working || schedule.SchoolStatus == SchoolStatus.Studying;

                if (!isCurrentlyAtWorkOrSchool)
                {
                    return ScheduleMealBeforeWorkOrSchool(ref schedule, citizenAge, departureTime);
                }

                return ScheduleMealDuringWorkOrSchool(ref schedule);
            }
        }

        public bool DoScheduledMeal(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            string citizenDesc = GetCitizenDesc(citizenId, ref citizen);

            Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going to eat {schedule.ScheduledMealType}");

            if (schedule.ScheduledMealType == MealType.None)
            {
                Log.Debug(LogCategory.State, TimeInfo.Now, $"{citizenDesc} has no scheduled meal type - updating");
                mealBehavior.UpdateMealTypeByTimeOfDay(citizenId, ref schedule);
                schedule.ResetScheduledStateTime();
            }

            float mealDuration = mealBehavior.GetMealDuration(schedule.ScheduledMealType);
            var mealStart = schedule.ScheduledStateTime != default && schedule.ScheduledStateTime > TimeInfo.Now ? schedule.ScheduledStateTime : TimeInfo.Now;
            var mealEnd = schedule.ScheduledMealEndTime != default ? schedule.ScheduledMealEndTime : mealStart.AddHours(mealDuration);

            Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is planning to eat {schedule.ScheduledMealType} and will start eat at {mealStart:dd.MM.yy HH:mm}, and the duration of the meal will be {mealDuration} hours, and will finish eating at {mealEnd:dd.MM.yy HH:mm}");

            if (schedule.Hint == ScheduleHint.LocalMealOnly || schedule.Hint == ScheduleHint.WorkOrSchoolRelatedMeal)
            {
                if (CurrentBuildingSupportsMeal(currentBuilding) && buildingAI.IsBuildingOpenForMeal(currentBuilding, mealStart, mealDuration))
                {
                    StartMeal(citizenId, ref schedule);
                    schedule.CurrentState = ResidentState.EatMeal;
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} stays in building {currentBuilding} for the purpose of eating {schedule.ScheduledMealType}");
                    return true;
                }

                ushort localMealPlace = FindMealBuilding(ref citizen, LocalSearchDistance, mealEnd);

                if (localMealPlace == 0)
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} wanted to go from {currentBuilding} to eat, but there were no food places close enough or open");
                    return false;
                }

                if (schedule.Hint == ScheduleHint.WorkOrSchoolRelatedMeal)
                {
                    if (!TryGetMealTravelTime(ref schedule, localMealPlace, out float _, out float returnTravel))
                    {
                        Log.Debug(LogCategory.Schedule, TimeInfo.Now, $"{citizenDesc} could not calculate travel time for meal place {localMealPlace}");
                        return false;
                    }

                    if (!CanCompleteWorkOrSchoolMeal(ref schedule, mealEnd, returnTravel))
                    {
                        Log.Debug(LogCategory.Schedule, TimeInfo.Now, $"{citizenDesc} cannot complete work/school meal at {localMealPlace} and return to work/school on time");
                        return false;
                    }
                }

                if (!StartMovingToVisitBuilding(instance, citizenId, ref citizen, localMealPlace))
                {
                    return false;
                }

                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going to eat {schedule.ScheduledMealType} at a local food place {localMealPlace} and will finish eating at {schedule.ScheduledMealEndTime:dd.MM.yy HH:mm}");
                return true;
            }

            if (CurrentBuildingSupportsMeal(currentBuilding) && buildingAI.IsBuildingOpenForMeal(currentBuilding, mealStart, mealDuration))
            {
                StartMeal(citizenId, ref schedule);
                schedule.CurrentState = ResidentState.EatMeal;
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} stays in building {currentBuilding} for the purpose of eating {schedule.ScheduledMealType}");
                return true;
            }

            ushort mealPlace = FindMealBuilding(ref citizen, MaxSearchDistance, mealEnd);

            if (mealPlace == 0)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} wanted to go from {currentBuilding} to eat, but no suitable food place was available");
                return false;
            }

            if (!StartMovingToVisitBuilding(instance, citizenId, ref citizen, mealPlace))
            {
                return false;
            }

            Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} is going from {currentBuilding} to eat {schedule.ScheduledMealType} at {mealPlace} and will finish eating at {schedule.ScheduledMealEndTime:dd.MM.yy HH:mm}");
            return true;
        }

        public void StartMeal(uint citizenId, ref CitizenSchedule schedule)
        {
            var mealType = schedule.ScheduledMealType;

            Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} arrived at their destination at {TimeInfo.Now:dd.MM.yy HH:mm} and will start eating {mealType}");

            MarkScheduledMealStarted(ref schedule);

            float mealDuration = mealBehavior.GetMealDuration(schedule.ScheduledMealType);
            var mealEnd = TimeInfo.Now.AddHours(mealDuration);

            if (schedule.Hint == ScheduleHint.WorkOrSchoolRelatedMeal)
            {
                ResidentState returnState;

                if (schedule.SchoolStatus == SchoolStatus.Studying || schedule.SchoolBuilding != 0 && schedule.WorkBuilding == 0)
                {
                    returnState = ResidentState.GoToSchool;
                }
                else if (schedule.WorkStatus == WorkStatus.Working || schedule.WorkBuilding != 0 && schedule.SchoolBuilding == 0)
                {
                    returnState = ResidentState.GoToWork;
                }
                else
                {
                    returnState = ResidentState.Unknown;
                }

                schedule.Schedule(returnState, default, MealType.None, mealEnd);
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} started eating {mealType} at {TimeInfo.Now:dd.MM.yy HH:mm}, will finish eating at {mealEnd:dd.MM.yy HH:mm} and then will {returnState}");
            }
            else
            {
                schedule.Schedule(ResidentState.Unknown, default, MealType.None, mealEnd);
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} started eating {mealType} at {TimeInfo.Now:dd.MM.yy HH:mm}, will finish eating at {mealEnd:dd.MM.yy HH:mm} and then will schedule Unknown");
            }
        }

        public bool TryResolveEatMealState(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen)
        {
            if (schedule.CurrentState != ResidentState.EatMeal)
            {
                // Not eating → nothing special to do.
                return true;
            }

            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);

            // No building → cannot be eating; stop meal.
            if (currentBuilding == 0)
            {
                StopEatingAndScheduleNextState(ref schedule, citizenId);
                return true; // let normal update run so they move
            }

            bool buildingIsWorking = buildingAI.IsBuildingWorking(currentBuilding);

            // Building closed → stop eating immediately.
            if (!buildingIsWorking)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} is eating {schedule.CurrentMealType} at building {currentBuilding}, but the building is now closed");

                StopEatingAndScheduleNextState(ref schedule, citizenId);
                return true; // continue update so they leave
            }

            // No scheduled end time → treat as “no active meal”; stop eating.
            if (schedule.ScheduledMealEndTime == default)
            {
                StopEatingAndScheduleNextState(ref schedule, citizenId);
                return true;
            }

            // Still within meal time?
            if (TimeInfo.Now < schedule.ScheduledMealEndTime)
            {
                if (schedule.Hint == ScheduleHint.WorkOrSchoolRelatedMeal)
                {
                    if (TryGetMealTravelTime(ref schedule, currentBuilding, out _, out float returnTravel))
                    {
                        Log.Debug(LogCategory.State, TimeInfo.Now, $"Citizen is still eating {schedule.CurrentMealType} until {schedule.ScheduledMealEndTime:dd.MM.yy HH:mm} and needs {returnTravel} hours to get back to work or school");

                        if (CanCompleteWorkOrSchoolMeal(ref schedule, schedule.ScheduledMealEndTime, returnTravel))
                        {
                            // Stay in EatMeal; skip normal movement this tick.
                            return false;
                        }
                    }

                    // Can't make it back in time → stop eating and go to work/school/unknown.
                    StopEatingAndScheduleNextState(ref schedule, citizenId);
                    return true; // continue update so they move
                }
                else
                {
                    // Non-work/school meal, still in time → stay in building.
                    return false;
                }
            }

            // Meal time expired → stop eating.
            StopEatingAndScheduleNextState(ref schedule, citizenId);
            return true; // continue update so they leave
        }

        private void StopEatingAndScheduleNextState(ref CitizenSchedule schedule, uint citizenId)
        {
            if (schedule.Hint == ScheduleHint.WorkOrSchoolRelatedMeal)
            {
                if (schedule.SchoolStatus == SchoolStatus.Studying || schedule.SchoolBuilding != 0 && schedule.WorkBuilding == 0)
                {
                    schedule.Schedule(ResidentState.GoToSchool);
                }
                else if (schedule.WorkStatus == WorkStatus.Working || schedule.WorkBuilding != 0 && schedule.SchoolBuilding == 0)
                {
                    schedule.Schedule(ResidentState.GoToWork);
                }
                else
                {
                    schedule.Schedule(ResidentState.Unknown);
                }

                schedule.Hint = ScheduleHint.None;
            }
            else
            {
                schedule.Schedule(ResidentState.Unknown);
            }

            Log.Debug(LogCategory.State, TimeInfo.Now, $"Citizen {citizenId} stopped eating {schedule.CurrentMealType} and switched to {schedule.ScheduledState}");
        }

        private ushort FindMealBuilding(ref TCitizen citizen, float distance, DateTime mealEndTime)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);

            if (currentBuilding == 0)
            {
                return 0;
            }

            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[currentBuilding];

            ushort mealPlace = 0;

            if (building.Info.GetAI() is CampusBuildingAI || building.Info.GetAI() is UniqueFacultyAI)
            {
                mealPlace = buildingAI.FindActiveCafeteria(currentBuilding, distance, mealEndTime);
            }

            if (mealPlace == 0)
            {
                mealPlace = buildingAI.FindActiveBuilding(
                    currentBuilding,
                    distance,
                    ItemClass.Service.Commercial,
                    ItemClass.SubService.None,
                    CommercialBuildingType.Food,
                    ParkBuildingType.None,
                    mealEndTime,
                    50);
            }

            return mealPlace;
        }

        private bool TryGetMealTravelTime(ref CitizenSchedule schedule, ushort mealPlace, out float outboundTravel, out float returnTravel)
        {
            outboundTravel = 0f;
            returnTravel = 0f;

            ushort obligationBuilding = 0;

            if (schedule.WorkStatus == WorkStatus.Working)
            {
                obligationBuilding = schedule.WorkBuilding;
            }
            else if (schedule.SchoolStatus == SchoolStatus.Studying)
            {
                obligationBuilding = schedule.SchoolBuilding;
            }
            else if (schedule.Hint == ScheduleHint.WorkOrSchoolRelatedMeal)
            {
                if (schedule.WorkBuilding != 0)
                {
                    obligationBuilding = schedule.WorkBuilding;
                }
                else if (schedule.SchoolBuilding != 0)
                {
                    obligationBuilding = schedule.SchoolBuilding;
                }
            }

            if (obligationBuilding == 0)
            {
                Log.Debug(LogCategory.Schedule, TimeInfo.Now, $" - Cannot calculate travel time for meal place {mealPlace} because obligation building is {obligationBuilding}");
                return false;
            }

            if (obligationBuilding == mealPlace)
            {
                Log.Debug(LogCategory.Schedule, TimeInfo.Now, $" - Obligation building {obligationBuilding} is the same as meal place {mealPlace}, so no travel time is needed");
                return true;
            }

            outboundTravel = travelBehavior.GetEstimatedTravelTime(obligationBuilding, mealPlace);
            returnTravel = travelBehavior.GetEstimatedTravelTime(mealPlace, obligationBuilding);

            Log.Debug(LogCategory.Schedule, TimeInfo.Now, $" - Estimated travel time from obligation building {obligationBuilding} to meal place {mealPlace} is {outboundTravel} hours, and return travel time is {returnTravel} hours");

            return outboundTravel >= 0f && returnTravel >= 0f;
        }

        private bool ScheduleMealBeforeWorkOrSchool(ref CitizenSchedule schedule, Citizen.AgeGroup citizenAge, DateTime departureTime)
        {
            mealBehavior.GetMealDataByTimeOfDay(TimeInfo.CurrentHour, out var mealType, out _, out float mealDuration);

            if (!mealBehavior.ShouldScheduleWorkOrSchoolMeal(ref schedule, citizenAge, mealType))
            {
                return false;
            }

            var earliestMealEnd = TimeInfo.Now.AddHours(mealDuration);

            if (departureTime != default && earliestMealEnd > departureTime)
            {
                Log.Debug(LogCategory.Schedule, $"  - work/school citizen wanted to go to eat {mealType} but meal end time {earliestMealEnd:dd.MM.yy HH:mm} is after departureTime {departureTime:dd.MM.yy HH:mm}");
                return false;
            }

            var placeholderEndTime = TimeInfo.Now.AddHours(mealDuration);

            schedule.Schedule(ResidentState.GoToMeal, mealType, placeholderEndTime);

            schedule.Hint = ScheduleHint.WorkOrSchoolRelatedMeal;

            Log.Debug(LogCategory.Schedule, $"  - citizen will go to eat {mealType} at {TimeInfo.Now:dd.MM.yy HH:mm} and will finish eating at {placeholderEndTime:dd.MM.yy HH:mm}");
            return true;
        }

        private bool ScheduleMealDuringWorkOrSchool(ref CitizenSchedule schedule)
        {
            if (!mealBehavior.TryGetBestWorkOrSchoolMealOpportunity(ref schedule, TimeInfo.Now, out var opportunity))
            {
                return false;
            }

            float blockEndHour;

            if (schedule.WorkStatus == WorkStatus.Working)
            {
                blockEndHour = schedule.WorkShiftEndTime;
            }
            else if (schedule.SchoolStatus == SchoolStatus.Studying)
            {
                blockEndHour = schedule.SchoolClassEndTime;
            }
            else
            {
                return false;
            }

            var blockEndTime = TimeInfo.Now.FutureHour(blockEndHour);

            var latestMealEndTime = blockEndTime.AddHours(-MinimumWorkOrSchoolTimeAfterMeal);

            if (opportunity.EndTime > latestMealEndTime)
            {
                Log.Debug(LogCategory.Schedule, $"  - {opportunity.MealType} was rejected because the citizen would have less than {MinimumWorkOrSchoolTimeAfterMeal} hours after the meal to spend at work or school.");
                return false;
            }

            schedule.Hint = ScheduleHint.WorkOrSchoolRelatedMeal;

            float mealDuration = mealBehavior.GetMealDuration(opportunity.MealType);
            var placeholderEndTime = opportunity.BeginTime.AddHours(mealDuration);

            schedule.Schedule(ResidentState.GoToMeal, opportunity.BeginTime, opportunity.MealType, placeholderEndTime);
            Log.Debug(LogCategory.Schedule, $"  - work/school citizen will go to eat {opportunity.MealType} at {opportunity.BeginTime:dd.MM.yy HH:mm} and will finish eating at {opportunity.EndTime:dd.MM.yy HH:mm}");
            return true;
        }

        private void MarkScheduledMealStarted(ref CitizenSchedule schedule)
        {
            schedule.ResetDailyMealsIfNeeded(TimeInfo.Now.DayOfYear);

            switch (schedule.ScheduledMealType)
            {
                case MealType.Breakfast:
                case MealType.Lunch:
                case MealType.Supper:
                    if (schedule.TryMarkMealConsumedToday(schedule.ScheduledMealType))
                    {
                        schedule.MealsEatenOutToday++;
                    }
                    break;

                case MealType.Other:
                    schedule.MarkSnackConsumed(TimeInfo.Now);
                    break;
            }
        }

        private bool CurrentBuildingSupportsMeal(ushort buildingId)
        {
            if (buildingId == 0)
            {
                return false;
            }

            if (!CommercialBuildingTypesManager.CommercialBuildingTypeExist(buildingId))
            {
                return false;
            }

            var commercialBuildingType = CommercialBuildingTypesManager.GetCommercialBuildingType(buildingId);

            return commercialBuildingType.IsFlagSet(CommercialBuildingType.Food);
        }

        private bool CanCompleteWorkOrSchoolMeal(ref CitizenSchedule schedule, DateTime mealEnd, float returnTravel)
        {
            var returnTime = mealEnd.AddHours(returnTravel);

            DateTime blockEndTime;

            if (schedule.WorkStatus == WorkStatus.Working)
            {
                blockEndTime = TimeInfo.Now.FutureHour(schedule.WorkShiftEndTime);
            }
            else if (schedule.SchoolStatus == SchoolStatus.Studying)
            {
                blockEndTime = TimeInfo.Now.FutureHour(schedule.SchoolClassEndTime);
            }
            else
            {
                return true;
            }

            var latestAllowedReturn = blockEndTime.AddHours(-MinimumWorkOrSchoolTimeAfterMeal);
            Log.Debug(LogCategory.Schedule, TimeInfo.Now, $" - Meal would end at {mealEnd:dd.MM.yy HH:mm}, return would finish at {returnTime:dd.MM.yy HH:mm}, and the latest allowed return is {latestAllowedReturn:dd.MM.yy HH:mm}");

            if (returnTime <= latestAllowedReturn)
            {
                Log.Debug(LogCategory.Schedule, TimeInfo.Now, $" - the citizen can complete the meal on time");
                return true;
            }

            Log.Debug(LogCategory.Schedule, TimeInfo.Now, $" - the citizen cannot complete the meal on time");
            return false;
        }
    }
}
