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
            
            if (schedule.ScheduledMealType == MealType.None)
            {
                mealBehavior.UpdateMealTypeByTimeOfDay(citizenId, ref schedule);
                schedule.ResetScheduledStateTime();
            }

            float mealDuration = mealBehavior.GetMealDuration(schedule.ScheduledMealType);
            var mealStart = schedule.ScheduledStateTime != default && schedule.ScheduledStateTime > TimeInfo.Now ? schedule.ScheduledStateTime : TimeInfo.Now;
            var mealEnd = schedule.ScheduledMealEndTime != default ? schedule.ScheduledMealEndTime : mealStart.AddHours(mealDuration);

            if (schedule.Hint == ScheduleHint.LocalMealOnly)
            {
                if (CurrentBuildingSupportsMeal(currentBuilding) && buildingAI.IsBuildingOpenForMeal(currentBuilding, mealStart, mealDuration))
                {
                    MarkScheduledMealStarted(ref schedule);
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} stays in building {currentBuilding} for the purpose of eating {schedule.ScheduledMealType}");
                    return true;
                }

                if (TimeInfo.IsNightTime)
                {
                    schedule.Hint = ScheduleHint.NoMealAnyMore;
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
                        Log.Debug(LogCategory.Schedule, TimeInfo.Now, $"Citizen {citizenId} could not calculate travel time for meal place {localMealPlace}");
                        return false;
                    }

                    if (!CanCompleteWorkOrSchoolMeal(ref schedule, mealEnd, returnTravel))
                    {
                        Log.Debug(LogCategory.Schedule, TimeInfo.Now, $"Citizen {citizenId} cannot complete work/school meal at {localMealPlace} and return to work/school on time");
                        return false;
                    }
                }

                if (!StartMovingToVisitBuilding(instance, citizenId, ref citizen, localMealPlace))
                {
                    return false;
                }

                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} is going to eat {schedule.ScheduledMealType} at a local food place {localMealPlace}");
                return true;
            }

            ushort mealPlace = FindMealBuilding(ref citizen, MaxSearchDistance, mealEnd);

            if (mealPlace == 0)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{citizenDesc} wanted to go from {currentBuilding} to eat, but no suitable food place was available");
                return false;
            }

            if (schedule.Hint == ScheduleHint.WorkOrSchoolRelatedMeal)
            {
                if (!TryGetMealTravelTime(ref schedule, mealPlace, out float _, out float returnTravel))
                {
                    Log.Debug(LogCategory.Schedule, TimeInfo.Now, $"Citizen {citizenId} could not calculate travel time for meal place {mealPlace}");
                    return false;
                }

                if (!CanCompleteWorkOrSchoolMeal(ref schedule, mealEnd, returnTravel))
                {
                    Log.Debug(LogCategory.Schedule, TimeInfo.Now, $"Citizen {citizenId} cannot complete work/school meal at {mealPlace} and return to work/school on time");
                    return false;
                }
            }

            if (schedule.Hint != ScheduleHint.WorkOrSchoolRelatedMeal)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"Citizen {citizenId} is going from {currentBuilding} to eat {schedule.ScheduledMealType} at {mealPlace} and will finish eating at {schedule.ScheduledMealEndTime:dd.MM.yy HH:mm}");
                schedule.Schedule(ResidentState.Unknown, schedule.ScheduledMealEndTime);
            }

            return true;
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
                mealPlace = buildingAI.FindActiveBuilding(currentBuilding, distance, ItemClass.Service.Commercial, ItemClass.SubService.None, CommercialBuildingType.Food, ParkBuildingType.None, mealEndTime);
            }

            return mealPlace;
        }

        private bool TryGetMealTravelTime(ref CitizenSchedule schedule, ushort mealPlace, out float outboundTravel, out float returnTravel)
        {
            outboundTravel = 0f;
            returnTravel = 0f;

            ushort obligationBuilding;

            if (schedule.WorkStatus == WorkStatus.Working)
            {
                obligationBuilding = schedule.WorkBuilding;
            }
            else if (schedule.SchoolStatus == SchoolStatus.Studying)
            {
                obligationBuilding = schedule.SchoolBuilding;
            }
            else
            {
                return false;
            }

            if (obligationBuilding == 0 || mealPlace == 0)
            {
                return false;
            }

            if (obligationBuilding == mealPlace)
            {
                return true;
            }

            outboundTravel = travelBehavior.GetEstimatedTravelTime(obligationBuilding, mealPlace);
            returnTravel = travelBehavior.GetEstimatedTravelTime(mealPlace, obligationBuilding);

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
            schedule.Hint = ScheduleHint.LocalMealOnly;

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

            if (returnTime <= latestAllowedReturn)
            {
                return true;
            }

            Log.Debug(LogCategory.Schedule, TimeInfo.Now, $"Meal would end at {mealEnd:dd.MM.yy HH:mm}, return would finish at {returnTime:dd.MM.yy HH:mm}, but the latest allowed return is {latestAllowedReturn:dd.MM.yy HH:mm}");
            schedule.Schedule(schedule.SchoolStatus == SchoolStatus.Studying ? ResidentState.GoToSchool : ResidentState.GoToWork);
            return false;
        }
    }
}
