// RealTimeResidentAI.Visit.cs

namespace RealTime.CustomAI
{
    using System;
    using SkyTools.Tools;
    using RealTime.Managers;
    using ColossalFramework;
    using static Constants;

    internal sealed partial class RealTimeResidentAI<TAI, TCitizen>
    {
        private bool ScheduleRelaxing(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen)
        {
            var citizenAge = CitizenProxy.GetAge(ref citizen);

            uint relaxChance = spareTimeBehavior.GetRelaxingChance(citizenAge, GetCitizenStartHour(ref schedule), schedule.WorkShift, schedule.WorkStatus == WorkStatus.OnVacation);
            relaxChance = AdjustRelaxChance(relaxChance, ref citizen);

            Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} has a relaxing chance of {relaxChance}%");

            if (!Random.ShouldOccur(relaxChance) || WeatherInfo.IsBadWeather)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} will not relax due to low chance or bad weather.");
                return false;
            }

            ushort eventBuilding = 0;
            var cityEvent = GetEventToAttend(citizenId, ref citizen, ref eventBuilding);
            if (cityEvent != null && eventBuilding != 0)
            {
                ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);
                var departureTime = cityEvent.StartTime.AddHours(-travelBehavior.GetEstimatedTravelTime(currentBuilding, eventBuilding));
                schedule.Schedule(ResidentState.GoToRelax, departureTime);
                schedule.EventBuilding = eventBuilding;
                schedule.Hint = ScheduleHint.AttendingEvent;
                Log.Debug(LogCategory.Events, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} will attend an event at building {eventBuilding} starting at {cityEvent.StartTime}. Departure time is {departureTime}.");
                return true;
            }

            schedule.Schedule(ResidentState.GoToRelax);
            schedule.Hint = TimeInfo.IsNightTime && Random.ShouldOccur(NightLeisureChance) ? ScheduleHint.RelaxAtLeisureBuilding : ScheduleHint.None;
            Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} will relax. Scheduled state: {schedule.ScheduledState}, Hint: {schedule.Hint}");

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

                    BuildingMgr.GetBuildingService(currentBuilding, out var _, out var targetSubService);

                    if(targetSubService == ItemClass.SubService.CommercialLeisure)
                    {
                        Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} is already in a leisure building {currentBuilding} and continues relaxing there.");
                        schedule.CurrentState = ResidentState.Relaxing;
                        schedule.Schedule(ResidentState.Unknown);
                        return true;
                    }

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
                    var cityEvent = EventMgr.GetCityEvent(eventBuilding);
                    if (cityEvent == null)
                    {
                        Log.Debug(LogCategory.Events, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted attend an event at '{eventBuilding}', but event does not exist");
                    }
                    else if (StartMovingToVisitBuilding(instance, citizenId, ref citizen, eventBuilding))
                    {
                        Log.Debug(LogCategory.Events, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} is going to attend an event at '{eventBuilding}'");
                        return true;
                    }
                    else
                    {
                        Log.Debug(LogCategory.Events, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted to go to an event at {eventBuilding} but cant");
                    }

                    return false;

                case ScheduleHint.RelaxNearbyOnly:

                    if (CurrentBuildingSupportsTarget(currentBuilding, ResidentState.Relaxing))
                    {
                        Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} stays in building {currentBuilding} for {schedule.CurrentState}");
                        schedule.CurrentState = ResidentState.Relaxing;
                        schedule.Schedule(ResidentState.Unknown);
                        return true;
                    }

                    var parkBuildingType = ParkBuildingTypesManager.GetPreferredParkType(CitizenProxy.GetAge(ref citizen), Random);
                    ushort parkBuildingId = buildingAI.FindActiveBuilding(
                        currentBuilding,
                        LocalSearchDistance,
                        ItemClass.Service.Beautification,
                        ItemClass.SubService.None,
                        CommercialBuildingType.None,
                        parkBuildingType,
                        default,
                        50);

                    if (parkBuildingId == 0)
                    {
                        Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted to relax nearby, but no suitable park was found");
                    }
                    else if (!StartMovingToVisitBuilding(instance, citizenId, ref citizen, parkBuildingId))
                    {
                        Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} found park {parkBuildingId}, but starting movement failed");
                    }
                    else
                    {
                        Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} found park {parkBuildingId}, heading to it");
                        return true;
                    }
                    return false;
            }

            if (!buildingAI.IsBuildingClosingSoon(currentBuilding) && CurrentBuildingSupportsTarget(currentBuilding, ResidentState.Relaxing))
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} stays in building {currentBuilding} for relaxing");
                schedule.CurrentState = ResidentState.Relaxing;
                schedule.Schedule(ResidentState.Unknown);
                return true;
            }

            Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} in state {schedule.CurrentState} wanna relax, heading to an entertainment building.");

            TransferManager.TransferReason entertainmentReason;
            var citizenAge = CitizenProxy.GetAge(ref citizen);
            if (citizenAge == Citizen.AgeGroup.Child || citizenAge == Citizen.AgeGroup.Teen)
            {
                entertainmentReason = TransferManager.TransferReason.ChildCare;
            }
            else if (citizenAge == Citizen.AgeGroup.Senior && Random.ShouldOccur(SeniorElderCareVisitChance))
            {
                entertainmentReason = TransferManager.TransferReason.ElderCare;
            }
            else
            {
                entertainmentReason = residentAI.GetEntertainmentReason(instance);
            }

            residentAI.FindVisitPlace(instance, citizenId, currentBuilding, entertainmentReason);
            schedule.FindVisitPlaceAttempts++;

            return true;
        }

        private bool ProcessCitizenRelaxing(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen, bool noReschedule)
        {
            ushort currentBuilding = CitizenProxy.GetVisitBuilding(ref citizen);
            if (CitizenProxy.HasFlags(ref citizen, Citizen.Flags.NeedGoods) && BuildingMgr.GetBuildingSubService(currentBuilding) == ItemClass.SubService.CommercialLeisure)
            {
                // No Citizen.Flags.NeedGoods flag reset here, because we only bought 'beer' or 'champagne' in a leisure building.
                BuildingMgr.ModifyMaterialBuffer(currentBuilding, TransferManager.TransferReason.Shopping, -ShoppingGoodsAmount);
            }
            return RescheduleVisit(ref schedule, citizenId, ref citizen, currentBuilding, noReschedule);
        }

        private bool ScheduleShopping(ref CitizenSchedule schedule, ref TCitizen citizen, bool localOnly)
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

            if (schedule.Hint == ScheduleHint.LocalShoppingOnly)
            {
                if (CurrentBuildingSupportsTarget(currentBuilding, ResidentState.Shopping))
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} stays in building {currentBuilding} for shopping");
                    schedule.CurrentState = ResidentState.Shopping;
                    schedule.Schedule(ResidentState.Unknown);
                    return true;
                }

                ushort shop = MoveToCommercialBuilding(instance, citizenId, ref citizen, LocalSearchDistance, CommercialBuildingType.Shopping, default);
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

            if (CurrentBuildingSupportsTarget(currentBuilding, ResidentState.Shopping) && !buildingAI.IsBuildingClosingSoon(currentBuilding))
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} stays in building {currentBuilding} for shopping");
                schedule.CurrentState = ResidentState.Shopping;
                schedule.Schedule(ResidentState.Unknown);
                return true;
            }

            Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} in state {schedule.CurrentState} wanna go shopping, heading to a random shop, hint = {schedule.Hint}");
            residentAI.FindVisitPlace(instance, citizenId, currentBuilding, residentAI.GetShoppingReason(instance));
            schedule.FindVisitPlaceAttempts++;

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

        private bool ScheduleBankVisit(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen)
        {
            if(!BankPostOfficeVisitManager.CitizenBankVisitDataExist(citizenId))
            {
                BankPostOfficeVisitManager.CitizenBankVisitDataExist(citizenId);
            }
            else
            {
                var bankVisitData = BankPostOfficeVisitManager.GetCitizenBankVisitData(citizenId);
                if (bankVisitData.LastVisit.AddDays(Config.VisitBankOrPostOfficeInterval) > TimeInfo.Now)
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} will not visit a bank because of cooldown. Last visit: {bankVisitData.LastVisit}, Cooldown: {Config.VisitBankOrPostOfficeInterval} days");
                    return false;
                }
            }

            var citizenAge = CitizenProxy.GetAge(ref citizen);
            uint visitBankChance = spareTimeBehavior.GetBankChance(citizenAge, GetCitizenStartHour(ref schedule), schedule.WorkShift, schedule.WorkStatus == WorkStatus.OnVacation);

            if (WeatherInfo.IsBadWeather || !Random.ShouldOccur(visitBankChance))
            {
                return false;
            }

            schedule.Schedule(ResidentState.GoToBank);
            Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} will visit a bank. Scheduled state: {schedule.ScheduledState}");
            return true;
        }

        private bool DoScheduledBankVisit(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            // BankVisit was already scheduled last time, but the citizen is still at school/work or in shelter.
            // This can occur when the game's transfer manager can't find any activity for the citizen.
            // In that case, move back home.
            if ((schedule.ScheduledState == ResidentState.GoToWork || schedule.CurrentState == ResidentState.AtWork ||
                schedule.ScheduledState == ResidentState.GoToSchool || schedule.CurrentState == ResidentState.AtSchool ||
                schedule.ScheduledState == ResidentState.GoToShelter || schedule.CurrentState == ResidentState.InShelter)
                && schedule.LastScheduledState == ResidentState.GoToBank)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted to visit a bank but is still at work or at school or in a shelter. No bank visit activity found. Now going home.");
                return false;
            }

            ushort targetBuilding = FindBank(ref citizen);

            if (targetBuilding == 0)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} in state {schedule.CurrentState} wanted to visit a bank but did not find an active building");
                return false;
            }

            return StartMovingToVisitBuilding(instance, citizenId, ref citizen, targetBuilding);
        }

        private bool SchedulePostOfficeVisit(ref CitizenSchedule schedule, uint citizenId, ref TCitizen citizen)
        {
            if (!BankPostOfficeVisitManager.CitizenPostOfficeVisitDataExist(citizenId))
            {
                BankPostOfficeVisitManager.CitizenPostOfficeVisitDataExist(citizenId);
            }
            else
            {
                var postOfficeVisitData = BankPostOfficeVisitManager.GetCitizenPostOfficeVisitData(citizenId);
                if (postOfficeVisitData.LastVisit.AddDays(Config.VisitBankOrPostOfficeInterval) > TimeInfo.Now)
                {
                    Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} will not visit a post office because of cooldown. Last visit: {postOfficeVisitData.LastVisit}, Cooldown: {Config.VisitBankOrPostOfficeInterval} days");
                    return false;
                }
            }

            var citizenAge = CitizenProxy.GetAge(ref citizen);
            uint visitPostOfficeChance = spareTimeBehavior.GetPostOfficeChance(citizenAge, GetCitizenStartHour(ref schedule), schedule.WorkShift, schedule.WorkStatus == WorkStatus.OnVacation);

            if (WeatherInfo.IsBadWeather || !Random.ShouldOccur(visitPostOfficeChance))
            {
                return false;
            }

            schedule.Schedule(ResidentState.GoToPostOffice);
            Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} will visit a post office. Scheduled state: {schedule.ScheduledState}");
            return true;
        }

        private bool DoScheduledPostOfficeVisit(ref CitizenSchedule schedule, TAI instance, uint citizenId, ref TCitizen citizen)
        {
            // PostOfficeVisit was already scheduled last time, but the citizen is still at school/work or in shelter.
            // This can occur when the game's transfer manager can't find any activity for the citizen.
            // In that case, move back home.
            if ((schedule.ScheduledState == ResidentState.GoToWork || schedule.CurrentState == ResidentState.AtWork ||
                schedule.ScheduledState == ResidentState.GoToSchool || schedule.CurrentState == ResidentState.AtSchool ||
                schedule.ScheduledState == ResidentState.GoToShelter || schedule.CurrentState == ResidentState.InShelter)
                && schedule.LastScheduledState == ResidentState.GoToPostOffice)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wanted to visit a post office but is still at work or at school or in a shelter. No post office visit activity found. Now going home.");
                return false;
            }

            ushort targetBuilding = FindPostOffice(ref citizen);
            if (targetBuilding == 0)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} in state {schedule.CurrentState} wanted to visit a post office but did not find an active building");
                return false;
            }

            return StartMovingToVisitBuilding(instance, citizenId, ref citizen, targetBuilding);
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
                case ResidentState.GoToBank:
                case ResidentState.GoToPostOffice:
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

            if (schedule.ActiveTravelState == ResidentState.GoToBank || schedule.ActiveTravelState == ResidentState.GoToPostOffice)
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} wont quit a visit to the bank or post office while traveling");
                return false;
            }

            var age = CitizenProxy.GetAge(ref citizen);
            uint stayChance = 0;

            switch (schedule.CurrentState)
            {
                case ResidentState.Shopping:
                    stayChance = spareTimeBehavior.GetShoppingChance(age);
                    break;
                case ResidentState.Relaxing:
                    stayChance = spareTimeBehavior.GetRelaxingChance(age, GetCitizenStartHour(ref schedule), schedule.WorkShift, schedule.WorkStatus == WorkStatus.OnVacation);
                    break;
                case ResidentState.AtBank:
                    stayChance = spareTimeBehavior.GetBankChance(age, GetCitizenStartHour(ref schedule), schedule.WorkShift, schedule.WorkStatus == WorkStatus.OnVacation);
                    break;
                case ResidentState.AtPostOffice:
                    stayChance = spareTimeBehavior.GetPostOfficeChance(age, GetCitizenStartHour(ref schedule), schedule.WorkShift, schedule.WorkStatus == WorkStatus.OnVacation);
                    break;
            }

            if (!Random.ShouldOccur(stayChance))
            {
                Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} quits the visit to do something else");
                schedule.Schedule(ResidentState.Unknown);
                return true;
            }

            Log.Debug(LogCategory.Movement, TimeInfo.Now, $"{GetCitizenDesc(citizenId, ref citizen)} continues the visit");

            return false;
        }

        private uint AdjustRelaxChance(uint relaxChance, ref TCitizen citizen)
        {
            ushort visitBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);

            if (BuildingMgr.GetBuildingSubService(visitBuilding) == ItemClass.SubService.BeautificationParks)
            {
                return Math.Min(relaxChance * 2, 100u);
            }
            else if (CitizenProxy.GetAge(ref citizen) == Citizen.AgeGroup.Senior && BuildingMgr.IsBuildingServiceLevel(visitBuilding, ItemClass.Service.HealthCare, ItemClass.Level.Level3))
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

        private bool CurrentBuildingSupportsTarget(ushort buildingId, ResidentState intendedState)
        {
            if (buildingId == 0)
            {
                return false;
            }

            if (CommercialBuildingTypesManager.CommercialBuildingTypeExist(buildingId))
            {
                var commercialBuildingType = CommercialBuildingTypesManager.GetCommercialBuildingType(buildingId);

                return intendedState switch
                {
                    ResidentState.Shopping => commercialBuildingType.IsFlagSet(CommercialBuildingType.Shopping),
                    ResidentState.EatMeal => commercialBuildingType.IsFlagSet(CommercialBuildingType.Food),
                    ResidentState.Relaxing => commercialBuildingType.IsFlagSet(CommercialBuildingType.Entertainment),
                    _ => false
                };
            }

            if (ParkBuildingTypesManager.ParkBuildingTypeExist(buildingId))
            {
                var parkBuildingType = ParkBuildingTypesManager.GetParkBuildingType(buildingId);

                return intendedState switch
                {
                    ResidentState.Relaxing => parkBuildingType != ParkBuildingType.None,
                    _ => false
                };
            }

            return false;
        }

        private float GetCitizenStartHour(ref CitizenSchedule schedule)
        {
            float starthour = -1;

            if (schedule.WorkBuilding != 0)
            {
                starthour = schedule.WorkShiftStartTime;
            }
            else if (schedule.SchoolBuilding != 0)
            {
                starthour = schedule.SchoolClassStartTime;
            }

            return starthour;
        }

        private ushort FindPostOffice(ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);

            return currentBuilding == 0
                ? (ushort)0
                : buildingAI.FindActiveBuilding(
                    currentBuilding,
                    MaxSearchDistance,
                    ItemClass.Service.PublicTransport,
                    ItemClass.SubService.PublicTransportPost,
                    CommercialBuildingType.None,
                    ParkBuildingType.None,
                    default,
                    30);
        }

        private ushort FindBank(ref TCitizen citizen)
        {
            ushort currentBuilding = CitizenProxy.GetCurrentBuilding(ref citizen);

            return currentBuilding == 0
                ? (ushort)0
                : buildingAI.FindActiveBuilding(
                    currentBuilding,
                    MaxSearchDistance,
                    ItemClass.Service.PoliceDepartment,
                    ItemClass.SubService.PoliceDepartmentBank,
                    CommercialBuildingType.None,
                    ParkBuildingType.None,
                    default,
                    30);
        }
    }
}
