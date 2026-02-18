// RealTimeResidentAI.Visit.cs

namespace RealTime.CustomAI
{
    using HarmonyLib;
    using System.Reflection;
    using SkyTools.Tools;
    using static Constants;
    using RealTime.Core;

    internal sealed partial class RealTimeResidentAI<TAI, TCitizen>
    {
        private delegate TransferManager.TransferReason GoToPostOfficeOrBankDelegate(Citizen.AgeGroup ageGroup);
        private static GoToPostOfficeOrBankDelegate GoToPostOfficeOrBank;

        private bool ScheduleRelaxing(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen)
        {
            var citizenAge = CitizenProxy.GetAge(ref citizen);

            uint relaxChance = spareTimeBehavior.GetRelaxingChance(citizenAge, schedule.WorkShift, schedule.WorkStatus == WorkStatus.OnVacation);
            relaxChance = AdjustRelaxChance(relaxChance, ref citizen);

            if (!Random.ShouldOccur(relaxChance) || WeatherInfo.IsBadWeather)
            {
                return false;
            }

            var cityEvent = GetEventToAttend(citizenId, ref citizen);
            if (cityEvent != null)
            {
                ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
                var departureTime = cityEvent.StartTime.AddHours(-travelBehavior.GetEstimatedTravelTime(currentBuilding, cityEvent.BuildingId));
                schedule.Schedule(ResidentState.GoToRelax, departureTime);
                schedule.EventBuilding = cityEvent.BuildingId;
                schedule.Hint = ScheduleHint.AttendingEvent;
                return true;
            }

            schedule.Schedule(ResidentState.GoToRelax);
            schedule.Hint = TimeInfo.IsNightTime && Random.ShouldOccur(NightLeisureChance)
                ? ScheduleHint.RelaxAtLeisureBuilding
                : ScheduleHint.None;

            return true;
        }

        private bool DoScheduledRelaxing(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            // Relaxing was already scheduled last time, but the citizen is still at school/work or in shelter.
            // This can occur when the game's transfer manager can't find any activity for the citizen.
            // In that case, move back home.
            if ((schedule.ScheduledState == ResidentState.GoToWork || schedule.CurrentState == ResidentState.AtWork ||
                schedule.ScheduledState == ResidentState.GoToSchool || schedule.CurrentState == ResidentState.AtSchool ||
                schedule.ScheduledState == ResidentState.GoToShelter || schedule.CurrentState == ResidentState.InShelter)
                && schedule.LastScheduledState == ResidentState.GoToRelax)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted relax but is still at work or in shelter. No relaxing activity found. Now going home.");
                return false;
            }

            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            switch (schedule.Hint)
            {
                case ScheduleHint.RelaxAtLeisureBuilding:
                    schedule.Schedule(ResidentState.Unknown);

                    ushort leisure = MoveToLeisureBuilding(instance, citizenId, ref citizen, currentBuilding);
                    if (leisure == 0)
                    {
                        Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted relax but didn't find a leisure building");
                        return false;
                    }

                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} heading to a leisure building {leisure}");
                    return true;

                case ScheduleHint.AttendingEvent:
                    ushort eventBuilding = schedule.EventBuilding;
                    schedule.EventBuilding = 0;

                    var cityEvent = EventMgr.GetCityEvent(eventBuilding);
                    if (cityEvent == null)
                    {
                        Log.Debug(LogCategory.Events, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted attend an event at '{eventBuilding}', but there was no event there");
                    }
                    else if (StartMovingToVisitBuilding(instance, citizenId, ref citizen, eventBuilding))
                    {
                        schedule.Schedule(ResidentState.Unknown, cityEvent.EndTime);
                        Log.Debug(LogCategory.Events, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanna attend an event at '{eventBuilding}', will return at {cityEvent.EndTime}");
                        return true;
                    }

                    schedule.Schedule(ResidentState.Unknown);
                    return false;

                case ScheduleHint.RelaxNearbyOnly:
                    var currentPosition = CitizenMgr.GetCitizenPosition(CitizenProxy.GetInstance(ref citizen));
                    ushort parkBuildingId = buildingAI.FindActiveBuilding(currentPosition, LocalSearchDistance, ItemClass.Service.Beautification);
                    if (StartMovingToVisitBuilding(instance, citizenId, ref citizen, parkBuildingId))
                    {
                        Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} heading to a nearby entertainment building {parkBuildingId}");
                        schedule.Schedule(ResidentState.Unknown);
                        return true;
                    }

                    schedule.Schedule(ResidentState.Unknown);
                    DoScheduledHome(ref schedule, instance, citizenId, ref citizen);
                    return true;
            }

            if(QuitVisit(citizenId, ref citizen, currentBuilding))
            {
                schedule.Schedule(ResidentState.GoHome);
                return false;
            }

            uint relaxChance = spareTimeBehavior.GetRelaxingChance(
                CitizenProxy.GetAge(ref citizen),
                schedule.WorkShift,
                schedule.WorkStatus == WorkStatus.OnVacation);

            relaxChance = AdjustRelaxChance(relaxChance, ref citizen);

            var nextState = Random.ShouldOccur(relaxChance)
                    ? ResidentState.GoToRelax
                    : ResidentState.Unknown;

            schedule.Schedule(nextState);

            if (schedule.ScheduledState != ResidentState.GoToRelax || schedule.CurrentState != ResidentState.Relaxing || Random.ShouldOccur(FindAnotherShopOrEntertainmentChance) || buildingAI.IsBuildingClosingSoon(currentBuilding))
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} in state {schedule.CurrentState} wanna relax and then schedules {nextState}, heading to an entertainment building.");

                TransferManager.TransferReason entertainmentReason;
                var citizenAge = CitizenProxy.GetAge(ref citizen);
                if (citizenAge == Citizen.AgeGroup.Child || citizenAge == Citizen.AgeGroup.Teen)
                {
                    entertainmentReason = TransferManager.TransferReason.ChildCare;
                }
                else if (citizenAge == Citizen.AgeGroup.Senior && Random.ShouldOccur(Constants.SeniorElderCareVisitChance))
                {
                    entertainmentReason = TransferManager.TransferReason.ElderCare;
                }
                else
                {
                    entertainmentReason = residentAI.GetEntertainmentReason(instance);
                }

                residentAI.FindVisitPlace(instance, citizenId, currentBuilding, entertainmentReason);
                schedule.FindVisitPlaceAttempts++;
            }
#if DEBUG
            else
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} continues relaxing in the same entertainment building.");
            }
#endif

            return true;
        }

        private bool ProcessCitizenRelaxing(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen, bool noReschedule)
        {
            ushort currentBuilding = CitizenProxy.GetVisitBuilding(ref citizen);
            if (CitizenProxy.HasFlags(ref citizen, Citizen.Flags.NeedGoods)
                && BuildingMgr.GetBuildingSubService(currentBuilding) == ItemClass.SubService.CommercialLeisure)
            {
                // No Citizen.Flags.NeedGoods flag reset here, because we only bought 'beer' or 'champagne' in a leisure building.
                BuildingMgr.ModifyMaterialBuffer(currentBuilding, TransferManager.TransferReason.Shopping, -ShoppingGoodsAmount);
            }

            return RescheduleVisit(ref schedule, citizenId, ref citizen, currentBuilding, noReschedule);
        }

        private bool ScheduleShopping(ref CitizenSchedule schedule, ref TCitizen citizen, bool localOnly, bool localOnlyWork = false, bool localOnlySchool = false)
        {
            // If the citizen doesn't need any goods, he/she still can go shopping just for fun
            if (!CitizenProxy.HasFlags(ref citizen, Citizen.Flags.NeedGoods))
            {
                if (schedule.Hint == ScheduleHint.NoShoppingAnyMore || WeatherInfo.IsBadWeather || !Random.ShouldOccur(Config.ShoppingForFunQuota))
                {
                    schedule.Hint = ScheduleHint.None;
                    return false;
                }

                schedule.Hint = ScheduleHint.NoShoppingAnyMore;
            }
            else
            {
                schedule.Hint = ScheduleHint.None;
            }

            if (!Random.ShouldOccur(spareTimeBehavior.GetShoppingChance(CitizenProxy.GetAge(ref citizen))))
            {
                return false;
            }

            if (TimeInfo.IsNightTime || localOnly || Random.ShouldOccur(Config.LocalBuildingSearchQuota))
            {
                schedule.Hint = ScheduleHint.LocalShoppingOnly;
            }

            if (localOnlyWork)
            {
                schedule.Hint = ScheduleHint.LocalShoppingOnlyBeforeWork;
            }
            if (localOnlySchool)
            {
                schedule.Hint = ScheduleHint.LocalShoppingOnlyBeforeUniversity;
            }

            schedule.Schedule(ResidentState.GoShopping);
            return true;
        }

        private bool DoScheduledShopping(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            // Shopping was already scheduled last time, but the citizen is still at school/work or in shelter.
            // This can occur when the game's transfer manager can't find any activity for the citizen.
            // In that case, move back home.
            if ((schedule.ScheduledState == ResidentState.GoToWork || schedule.CurrentState == ResidentState.AtWork
                || schedule.ScheduledState == ResidentState.GoToSchool || schedule.CurrentState == ResidentState.AtSchool
                || schedule.ScheduledState == ResidentState.GoToShelter || schedule.CurrentState == ResidentState.InShelter)
                && schedule.LastScheduledState == ResidentState.GoShopping)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted go shopping but is still at work or school or in shelter. No shopping activity found. Now going home.");
                return false;
            }

            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
            if (schedule.Hint == ScheduleHint.LocalShoppingOnly || schedule.Hint == ScheduleHint.LocalShoppingOnlyBeforeWork || schedule.Hint == ScheduleHint.LocalShoppingOnlyBeforeUniversity)
            {
                if(schedule.Hint == ScheduleHint.LocalShoppingOnly)
                {
                    schedule.Schedule(ResidentState.Unknown);

                    ushort shop = MoveToCommercialBuilding(instance, citizenId, ref citizen, LocalSearchDistance, true);
                    if (shop == 0)
                    {
                        Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted go shopping, but didn't find a local shop");
                        return false;
                    }

                    if (TimeInfo.IsNightTime)
                    {
                        schedule.Hint = ScheduleHint.NoShoppingAnyMore;
                    }

                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} goes shopping at a local shop {shop}");
                    return true;

                }

                if (schedule.Hint == ScheduleHint.LocalShoppingOnlyBeforeWork || schedule.Hint == ScheduleHint.LocalShoppingOnlyBeforeUniversity)
                {
                    ushort shop = MoveToCommercialBuilding(instance, citizenId, ref citizen, LocalSearchDistance, true);
                    ushort sourceBuilding = currentBuilding;
                    bool found = false;
                    if (shop == 0)
                    {
                        Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted go shopping, but didn't find a local shop");
                    }
                    else
                    {
                        Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} goes shopping at a local shop {shop}");
                        sourceBuilding = shop;
                        found = true;
                    }

                    if (schedule.Hint == ScheduleHint.LocalShoppingOnlyBeforeWork)
                    {
                        var departureTime = workBehavior.ScheduleGoToWorkTime(ref schedule, sourceBuilding, simulationCycle);
                        schedule.Schedule(ResidentState.GoToWork, departureTime);
                        Log.Debug(LogCategory.Schedule, $"  - Schedule work at {departureTime:dd.MM.yy HH:mm}");
                    }
                    else if (schedule.Hint == ScheduleHint.LocalShoppingOnlyBeforeUniversity)
                    {
                        var departureTime = schoolBehavior.ScheduleGoToSchoolTime(ref schedule, sourceBuilding, simulationCycle);
                        schedule.Schedule(ResidentState.GoToSchool, departureTime);
                        Log.Debug(LogCategory.Schedule, $"  - Schedule school at {departureTime:dd.MM.yy HH:mm}");
                    }

                    return found;
                }
            }

            if (QuitVisit(citizenId, ref citizen, currentBuilding))
            {
                schedule.Schedule(ResidentState.GoHome);
                return false;
            }

            uint moreShoppingChance = spareTimeBehavior.GetShoppingChance(CitizenProxy.GetAge(ref citizen));
            var nextState = schedule.Hint != ScheduleHint.NoShoppingAnyMore && Random.ShouldOccur(moreShoppingChance)
                ? ResidentState.GoShopping
                : ResidentState.Unknown;

            schedule.Schedule(nextState);

            if (schedule.ScheduledState != ResidentState.GoShopping || schedule.CurrentState != ResidentState.Shopping || Random.ShouldOccur(FindAnotherShopOrEntertainmentChance) || buildingAI.IsBuildingClosingSoon(currentBuilding))
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} in state {schedule.CurrentState} wanna go shopping and schedules {nextState}, heading to a random shop, hint = {schedule.Hint}");
                residentAI.FindVisitPlace(instance, citizenId, currentBuilding, residentAI.GetShoppingReason(instance));
                schedule.FindVisitPlaceAttempts++;
            }
#if DEBUG
            else
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} continues shopping in the same building.");
            }
#endif

            return true;
        }

        private bool ProcessCitizenShopping(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen, bool noReschedule)
        {
            ushort currentBuilding = CitizenProxy.GetVisitBuilding(ref citizen);
            if (CitizenProxy.HasFlags(ref citizen, Citizen.Flags.NeedGoods) && currentBuilding != 0)
            {
                BuildingMgr.ModifyMaterialBuffer(currentBuilding, TransferManager.TransferReason.Shopping, -ShoppingGoodsAmount);
                CitizenProxy.RemoveFlags(ref citizen, Citizen.Flags.NeedGoods);
            }

            return RescheduleVisit(ref schedule, citizenId, ref citizen, currentBuilding, noReschedule);
        }

        private bool ProcessCitizenEatingBreakfast(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen, bool noReschedule)
        {
            ushort currentBuilding = CitizenProxy.GetVisitBuilding(ref citizen);
            return RescheduleVisit(ref schedule, citizenId, ref citizen, currentBuilding, noReschedule);
        }

        private bool ProcessCitizenEatingLunch(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen, bool noReschedule)
        {
            ushort currentBuilding = CitizenProxy.GetVisitBuilding(ref citizen);
            return RescheduleVisit(ref schedule, citizenId, ref citizen, currentBuilding, noReschedule);
        }

        private bool ScheduleVisiting(ref CitizenSchedule schedule, ref TCitizen citizen)
        {
            if(RealTimeCore.isCombinedAIEnabled && GoToPostOfficeOrBank == null)
            {
                GoToPostOfficeOrBank = AccessTools.MethodDelegate<GoToPostOfficeOrBankDelegate>(AccessTools.TypeByName("CombinedAIS.Managers.BankPostOfficeManager").GetMethod("GoToPostOfficeOrBank", BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static), null, false);
            }

            if (WeatherInfo.IsBadWeather || GoToPostOfficeOrBank == null)
            {
                return false;
            }

            schedule.Schedule(ResidentState.GoToVisit);
            schedule.Hint = ScheduleHint.None;

            return true;
        }

        private bool DoScheduledVisiting(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            // Relaxing was already scheduled last time, but the citizen is still at school/work or in shelter.
            // This can occur when the game's transfer manager can't find any activity for the citizen.
            // In that case, move back home.
            if ((schedule.ScheduledState == ResidentState.GoToWork || schedule.CurrentState == ResidentState.AtWork ||
                schedule.ScheduledState == ResidentState.GoToSchool || schedule.CurrentState == ResidentState.AtSchool ||
                schedule.ScheduledState == ResidentState.GoToShelter || schedule.CurrentState == ResidentState.InShelter)
                && schedule.LastScheduledState == ResidentState.GoToVisit)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted to visit but is still at work or in shelter. No visit activity found. Now going home.");
                return false;
            }

            if (GoToPostOfficeOrBank == null)
            {
                return false;
            }

            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);

            if (QuitVisit(citizenId, ref citizen, currentBuilding))
            {
                schedule.Schedule(ResidentState.GoHome);
                return false;
            }

            schedule.Schedule(ResidentState.Unknown);

            if (schedule.ScheduledState != ResidentState.GoToVisit || schedule.CurrentState != ResidentState.Visiting || buildingAI.IsBuildingClosingSoon(currentBuilding))
            {
                var reason = GoToPostOfficeOrBank(CitizenProxy.GetAge(ref citizen));
                
                schedule.FindVisitPlaceAttempts++;

                if (reason != TransferManager.TransferReason.None)
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} in state {schedule.CurrentState} want to visit and then schedules {ResidentState.Unknown}, searching for visit place with reason {reason}");
                    residentAI.FindVisitPlace(instance, citizenId, currentBuilding, reason);
                    return true;
                }
                else
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} in state {schedule.CurrentState} wanted to visit but did not find a good reason");
                    return false;
                }
            }
#if DEBUG
            else
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} continues visiting the same building.");
            }
#endif
            return true;
        }

        private bool ProcessCitizenVisit(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen, bool noReschedule)
        {
            ushort currentBuilding = CitizenProxy.GetVisitBuilding(ref citizen);
            var currentBuildingService = BuildingMgr.GetBuildingService(currentBuilding);
            if (currentBuildingService == ItemClass.Service.Education)
            {
                residentAI.AttemptAutodidact(instance, ref citizen, currentBuildingService);
            }

            return RescheduleVisit(ref schedule, citizenId, ref citizen, currentBuilding, noReschedule);
        } 

        private bool RescheduleVisit(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen, ushort currentBuilding, bool noReschedule)
        {
            switch (schedule.ScheduledState)
            {
                case ResidentState.GoShopping:
                case ResidentState.GoToRelax:
                case ResidentState.GoToVisit:
                    break;

                default:
                    return false;
            }

            if (schedule.ScheduledState != ResidentState.GoShopping && schedule.CurrentState != ResidentState.Shopping && WeatherInfo.IsBadWeather)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} quits a visit because of bad weather");
                schedule.Schedule(ResidentState.GoHome);
                return true;
            }

            if (QuitVisit(citizenId, ref citizen, currentBuilding))
            {
                schedule.Schedule(ResidentState.GoHome);
                return true;
            }

            if (buildingAI.IsBuildingClosingSoon(currentBuilding))
            {
                if(!noReschedule)
                {
                    schedule.Schedule(ResidentState.Unknown);
                }
                return true;
            }

            if (schedule.ScheduledState == ResidentState.GoToVisit)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wont quit a visit to the bank of post office");
                return false;
            }

            var age = CitizenProxy.GetAge(ref citizen);
            uint stayChance = schedule.CurrentState == ResidentState.Shopping
                ? spareTimeBehavior.GetShoppingChance(age)
                : spareTimeBehavior.GetRelaxingChance(age, schedule.WorkShift, schedule.WorkStatus == WorkStatus.OnVacation);

            if (!Random.ShouldOccur(stayChance))
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} quits a visit because of time");
                schedule.Schedule(ResidentState.GoHome);
                return true;
            }

            return false;
        }

        private uint AdjustRelaxChance(uint relaxChance, ref TCitizen citizen)
        {
            ushort visitBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);

            if (BuildingMgr.GetBuildingSubService(visitBuilding) == ItemClass.SubService.BeautificationParks)
            {
                return relaxChance * 2;
            }
            else if (CitizenProxy.GetAge(ref citizen) == Citizen.AgeGroup.Senior
                && BuildingMgr.IsBuildingServiceLevel(visitBuilding, ItemClass.Service.HealthCare, ItemClass.Level.Level3))
            {
                return relaxChance * 4;
            }
            else
            {
                return relaxChance;
            }
        }

        private bool QuitVisit(uint citizenId, ref TCitizen citizen, ushort currentBuilding)
        {
            if (!buildingAI.IsBuildingWorking(currentBuilding))
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} quits a visit because the building is currently closed");
                return true;
            }

            return false;
        }
    }
}
