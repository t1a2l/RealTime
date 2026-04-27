// ResidentAIPatch.cs

namespace RealTime.Patches
{
    using System;
    using HarmonyLib;
    using RealTime.CustomAI;
    using SkyTools.Tools;
    using RealTime.GameConnection;
    using static RealTime.GameConnection.HumanAIConnectionBase<ResidentAI, Citizen>;
    using static RealTime.GameConnection.ResidentAIConnection<ResidentAI, Citizen>;
    using RealTime.Core;
    using ColossalFramework;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using UnityEngine;
    using RealTime.Managers;

    /// <summary>
    /// A static class that provides the patch objects and the game connection objects for the resident AI .
    /// </summary>
    [HarmonyPatch]
    internal static class ResidentAIPatch
    {
        /// <summary>Gets or sets the custom AI object for resident citizens.</summary>
        public static RealTimeResidentAI<ResidentAI, Citizen> RealTimeResidentAI { get; set; }

        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        public static TimeInfo TimeInfo { get; set; }

        public static ushort Chosen_Building = 0;

        /// <summary>Creates a game connection object for the resident AI class.</summary>
        /// <returns>A new <see cref="ResidentAIConnection{ResidentAI, Citizen}"/> object.</returns>
        public static ResidentAIConnection<ResidentAI, Citizen> GetResidentAIConnection()
        {
            try
            {
                var doRandomMove = AccessTools.MethodDelegate<DoRandomMoveDelegate>(AccessTools.Method(typeof(ResidentAI), "DoRandomMove"));

                var findEvacuationPlace = AccessTools.MethodDelegate<FindEvacuationPlaceDelegate>(AccessTools.Method(typeof(HumanAI), "FindEvacuationPlace"));

                var findHospital = AccessTools.MethodDelegate<FindHospitalDelegate>(AccessTools.Method(typeof(ResidentAI), "FindHospital"));

                var findVisitPlace = AccessTools.MethodDelegate<FindVisitPlaceDelegate>(AccessTools.Method(typeof(HumanAI), "FindVisitPlace"));

                var getEntertainmentReason = AccessTools.MethodDelegate<GetEntertainmentReasonDelegate>(AccessTools.Method(typeof(ResidentAI), "GetEntertainmentReason"));

                var getEvacuationReason = AccessTools.MethodDelegate<GetEvacuationReasonDelegate>(AccessTools.Method(typeof(ResidentAI), "GetEvacuationReason"));

                var getShoppingReason = AccessTools.MethodDelegate<GetShoppingReasonDelegate>(AccessTools.Method(typeof(ResidentAI), "GetShoppingReason"));

                var startMoving = AccessTools.MethodDelegate<StartMovingDelegate>(AccessTools.Method(typeof(HumanAI), "StartMoving", [typeof(uint), typeof(Citizen).MakeByRefType(), typeof(ushort), typeof(ushort)]));

                var startMovingWithOffer = AccessTools.MethodDelegate<StartMovingWithOfferDelegate>(AccessTools.Method(typeof(HumanAI), "StartMoving", [typeof(uint), typeof(Citizen).MakeByRefType(), typeof(ushort), typeof(TransferManager.TransferOffer)]));

                var attemptAutodidact = AccessTools.MethodDelegate<AttemptAutodidactDelegate>(AccessTools.Method(typeof(ResidentAI), "AttemptAutodidact"));

                return new ResidentAIConnection<ResidentAI, Citizen>(
                    doRandomMove,
                    findEvacuationPlace,
                    findHospital,
                    findVisitPlace,
                    getEntertainmentReason,
                    getEvacuationReason,
                    getShoppingReason,
                    startMoving,
                    startMovingWithOffer,
                    attemptAutodidact);
            }
            catch (Exception e)
            {
                Log.Error("The 'Real Time' mod failed to create a delegate for type 'ResidentAI', no method patching for the class: " + e);
                return null;
            }
        }

        [HarmonyPatch(typeof(ResidentAI), "UpdateHealth")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> TranspileUpdateHealth(IEnumerable<CodeInstruction> instructions)
        {
            var inst = new List<CodeInstruction>(instructions);

            for (int i = 0; i < inst.Count; i++)
            {
                if (inst[i].LoadsConstant(1000) && inst[i + 1].opcode == OpCodes.Div)
                {
                    inst[i].operand = 10000;
                }
            }
            return inst;
        }

        [HarmonyPatch(typeof(ResidentAI), "UpdateLocation")]
        [HarmonyPrefix]
        private static bool UpdateLocationPrefix(ResidentAI __instance, uint citizenID, ref Citizen data)
        {
            if (RealTimeResidentAI != null)
            {
                RealTimeResidentAI.UpdateLocation(__instance, citizenID, ref data);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(ResidentAI), "UpdateAge")]
        [HarmonyPrefix]
        private static bool UpdateAgePrefix(ref bool __result)
        {
            if (!RealTimeCore.ApplyCitizenPatch)
            {
                return true;
            }

            if (RealTimeResidentAI != null && RealTimeResidentAI.CanCitizensGrowUp)
            {
                return true;
            }

            __result = false;
            return false;
        }

        [HarmonyPatch(typeof(ResidentAI), "CanMakeBabies")]
        [HarmonyPrefix]
        private static bool CanMakeBabiesPrefix(uint citizenID, ref Citizen data, ref bool __result)
        {
            if (!RealTimeCore.ApplyCitizenPatch)
            {
                return true;
            }

            if (RealTimeResidentAI != null)
            {
                __result = RealTimeResidentAI.CanMakeBabies(citizenID, ref data);
            }

            return false;
        }

        [HarmonyPatch(typeof(ResidentAI), "FinishSchoolOrWork")]
        [HarmonyPrefix]
        private static bool FinishSchoolOrWorkPrefix(ref Citizen data)
        {
            if (data.m_workBuilding == 0)
            {
                return true;
            }
            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_workBuilding];
            if ((data.m_flags & Citizen.Flags.Student) == 0)
            {
                return true;
            }
            bool IsCampusUniversity = building.Info && (building.Info.GetAI() is CampusBuildingAI || building.Info.GetAI() is UniqueFacultyAI);
            if (!IsCampusUniversity)
            {
                return true;
            }

            byte park = Singleton<DistrictManager>.instance.GetPark(building.m_position);
            var campus = Singleton<DistrictManager>.instance.m_parks.m_buffer[park];

            var academicYearData = AcademicYearManager.GetAcademicYearData(campus.m_mainGate);

            return academicYearData.DidLastYearEnd;
        }

        [HarmonyPatch(typeof(ResidentAI), "SimulationStep",
                [typeof(ushort), typeof(CitizenInstance), typeof(CitizenInstance.Frame), typeof(bool)],
                [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal])]
        [HarmonyPostfix]
        private static void SimulationStepPostfix(ResidentAI __instance, ushort instanceID, ref CitizenInstance citizenData)
        {
            if (instanceID == 0)
            {
                return;
            }

            if ((citizenData.m_flags & (CitizenInstance.Flags.WaitingTaxi | CitizenInstance.Flags.WaitingTransport)) != 0 && RealTimeResidentAI != null)
            {
                RealTimeResidentAI.ProcessWaitingForTransport(__instance, citizenData.m_citizen, instanceID);
            }
        }

        [HarmonyPatch(typeof(ResidentAI), "GetColor")]
        [HarmonyPrefix]
        private static bool GetColorPrefix(ResidentAI __instance, ushort instanceID, ref CitizenInstance data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
        {
            if (instanceID == 0)
            {
                return true;
            }

            if (infoMode == InfoManager.InfoMode.Density)
            {
                if (Chosen_Building == 0 && WorldInfoPanel.GetCurrentInstanceID().Building == 0)
                {
                    return true;
                }

                if (WorldInfoPanel.GetCurrentInstanceID().Building != 0)
                {
                    Chosen_Building = WorldInfoPanel.GetCurrentInstanceID().Building;
                }

                var citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[data.m_citizen];

                ushort home_building = Singleton<CitizenManager>.instance.m_citizens.m_buffer[data.m_citizen].m_homeBuilding;
                ushort work_building = Singleton<CitizenManager>.instance.m_citizens.m_buffer[data.m_citizen].m_workBuilding;
                ushort visit_building = Singleton<CitizenManager>.instance.m_citizens.m_buffer[data.m_citizen].m_visitBuilding;

                if (Chosen_Building == work_building)
                {
                    __result = (citizen.m_flags & Citizen.Flags.Student) != 0 ? Color.yellow : Color.blue;
                }
                else if (Chosen_Building == home_building)
                {
                    __result = Color.green;
                }
                else
                {
                    __result = Chosen_Building == visit_building ? Color.magenta : Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                }
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(ResidentAI), "StartTransfer")]
        [HarmonyPrefix]
        private static bool StartTransferPrefix(ResidentAI __instance, uint citizenID, ref Citizen data, TransferManager.TransferReason reason, TransferManager.TransferOffer offer)
        {
            if (data.m_flags == Citizen.Flags.None || data.Dead && reason != TransferManager.TransferReason.Dead)
            {
                return true;
            }
            switch (reason)
            {
                case TransferManager.TransferReason.Shopping:
                case TransferManager.TransferReason.ShoppingB:
                case TransferManager.TransferReason.ShoppingC:
                case TransferManager.TransferReason.ShoppingD:
                case TransferManager.TransferReason.ShoppingE:
                case TransferManager.TransferReason.ShoppingF:
                case TransferManager.TransferReason.ShoppingG:
                case TransferManager.TransferReason.ShoppingH:
                    if (data.m_homeBuilding != 0 && !data.Sick)
                    {
                        var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[offer.Building];
                        // dont shop in hotel buildings
                        if (BuildingManagerConnection.IsHotel(offer.Building))
                        {
                            return false;
                        }
                        // dont shop in party buildings
                        if (building.Info && building.Info.m_class.m_service == ItemClass.Service.Commercial && building.Info.m_class.m_subService == ItemClass.SubService.CommercialLeisure)
                        {
                            return false;
                        }
                        // dont shop in closed buildings
                        if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(offer.Building))
                        {
                            return false;
                        }
                        // dont visit buildings that cannot be visited
                        if (!RealTimeBuildingAI.HaveUnits(offer.Building, CitizenUnit.Flags.Visit))
                        {
                            return false;
                        }
                        // normal residents or students from other campuses will not visit
                        if (building.Info && building.Info.GetAI() is CampusBuildingAI && building.Info.name.Contains("Cafeteria"))
                        {
                            ushort currentBuilding = data.GetBuildingByLocation();
                            if ((data.m_flags & Citizen.Flags.Student) == 0)
                            {
                                return false;
                            }
                            if (!BuildingManagerConnection.CheckSameCampusArea(currentBuilding, offer.Building))
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                case TransferManager.TransferReason.Entertainment:
                case TransferManager.TransferReason.EntertainmentB:
                case TransferManager.TransferReason.EntertainmentC:
                case TransferManager.TransferReason.EntertainmentD:
                    if (data.m_homeBuilding != 0 && !data.Sick)
                    {
                        var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[offer.Building];

                        // dont go to entertainment in hotels with no events
                        if (BuildingManagerConnection.IsHotel(offer.Building))
                        {
                            if (building.m_eventIndex == 0)
                            {
                                return false;
                            }
                        }
                        // dont go to entertainment in closed buildings
                        if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(offer.Building))
                        {
                            return false;
                        }
                        // dont visit buildings that cannot be visited
                        if (!RealTimeBuildingAI.HaveUnits(offer.Building, CitizenUnit.Flags.Visit))
                        {
                            return false;
                        }
                        // normal residents or students from other campuses will not visit
                        if (building.Info && building.Info.GetAI() is CampusBuildingAI && building.Info.name.Contains("Gymnasium"))
                        {
                            ushort currentBuilding = data.GetBuildingByLocation();
                            if ((data.m_flags & Citizen.Flags.Student) == 0)
                            {
                                return false;
                            }
                            if (!BuildingManagerConnection.CheckSameCampusArea(currentBuilding, offer.Building))
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                default:
                    return true;
            }
        }

        [HarmonyPatch(typeof(ResidentAI), "FindHospital")]
        [HarmonyPrefix]
        [HarmonyBefore(["t1a2l.SeniorCitizenCenterMod", "t1a2l.CimCareMod"])]
        [HarmonyAfter(["Sleepy.TransferManagerCE"])]
        private static bool FindHospital(ResidentAI __instance, uint citizenID, ushort sourceBuilding, TransferManager.TransferReason reason, ref bool __result)
        {
            if (IsInNursingHomeAndNotTooSick(citizenID, sourceBuilding))
            {
                // We pretend we have successfully sent out an offer but we actually don't need to
                __result = true;
            }
            else
            {
                // Call our bug fixed version of the function
                __result = FindHospital(citizenID, sourceBuilding, reason);
            }

            // Always return false as we don't want to run the buggy vanilla function
            return false;
        }

        // Added support for the nursing home mod which tries to patch the same function
        private static bool IsInNursingHomeAndNotTooSick(uint citizenID, ushort sourceBuilding)
        {
            if (Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.HealthCare) &&
                Singleton<CitizenManager>.exists && Singleton<CitizenManager>.instance is not null && IsSenior(citizenID))
            {
                var citizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID];
                if (citizen.m_flags != 0 && sourceBuilding == citizen.m_homeBuilding && citizen.m_health >= 40)
                {
                    return BuildingManagerConnection.IsCimCareBuilding(citizen.m_homeBuilding);
                }
            }
            return false;
        }

        private static bool FindHospital(uint citizenID, ushort sourceBuilding, TransferManager.TransferReason reason)
        {
            if (reason == TransferManager.TransferReason.Dead)
            {
                if (Singleton<UnlockManager>.instance.Unlocked(UnlockManager.Feature.DeathCare))
                {
                    return true;
                }

                Singleton<CitizenManager>.instance.ReleaseCitizen(citizenID);
                return false;
            }

            if (Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.HealthCare))
            {
                var instance = Singleton<BuildingManager>.instance;
                var instance2 = Singleton<DistrictManager>.instance;
                var position = instance.m_buildings.m_buffer[sourceBuilding].m_position;
                byte district = instance2.GetDistrict(position);
                var servicePolicies = instance2.m_districts.m_buffer[district].m_servicePolicies;

                // Add a transfer offer
                var offer = default(TransferManager.TransferOffer);
                offer.Priority = 6;
                offer.Citizen = citizenID;
                offer.Position = position;
                offer.Amount = 1;

                // Half the time request Eldercare/Childcare services instead of using a Hospital if the citizen isnt too sick
                if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) == 0 && RequestEldercareChildcareService(citizenID, offer))
                {
                    return true; // offer sent
                }

                // Add a Sick or Sick2 outgoing offer instead
                if (SteamHelper.IsDLCOwned(SteamHelper.DLC.NaturalDisastersDLC) && (servicePolicies & DistrictPolicies.Services.HelicopterPriority) != 0)
                {
                    instance2.m_districts.m_buffer[district].m_servicePoliciesEffect |= DistrictPolicies.Services.HelicopterPriority;
                    offer.Active = false;
                    reason = TransferManager.TransferReason.Sick2;
                }
                else if (SteamHelper.IsDLCOwned(SteamHelper.DLC.NaturalDisastersDLC) && ((instance.m_buildings.m_buffer[sourceBuilding].m_flags & Building.Flags.RoadAccessFailed) != 0 || Singleton<SimulationManager>.instance.m_randomizer.Int32(20u) == 0))
                {
                    offer.Active = false;
                    reason = TransferManager.TransferReason.Sick2;
                }
                else
                {
                    offer.Active = Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) == 0;
                    reason = TransferManager.TransferReason.Sick;
                }

                Singleton<TransferManager>.instance.AddOutgoingOffer(reason, offer);

                Log.Debug(LogCategory.State, TimeInfo.Now, $"Citizen {citizenID} is sick, requesting {reason}, is offer active: {offer.Active}");

                return true;
            }

            Singleton<CitizenManager>.instance.ReleaseCitizen(citizenID);
            return false;
        }

        private static bool RequestEldercareChildcareService(uint citizenID, TransferManager.TransferOffer offer)
        {
            if (Singleton<CitizenManager>.exists &&
                Singleton<CitizenManager>.instance is not null &&
                Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].m_health >= 40 &&
                (IsChild(citizenID) || IsSenior(citizenID)))
            {
                var reason = TransferManager.TransferReason.None;
                var serviceBuildings = Singleton<BuildingManager>.instance.GetServiceBuildings(ItemClass.Service.HealthCare);
                for (int i = 0; i < serviceBuildings.m_size; i++)
                {
                    var info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[serviceBuildings[i]].Info;
                    if ((object)info is not null)
                    {
                        if (IsChild(citizenID) && info.m_class.m_level == ItemClass.Level.Level4)
                        {
                            reason = TransferManager.TransferReason.ChildCare;
                            break;
                        }
                        else if (IsSenior(citizenID) && info.m_class.m_level == ItemClass.Level.Level5)
                        {
                            reason = TransferManager.TransferReason.ElderCare;
                            break;
                        }
                    }
                }

                // Send request if we found a Childcare/Eldercare facility
                if (reason != TransferManager.TransferReason.None)
                {
                    // WARNING: Childcare and Eldercare need an IN offer
                    offer.Active = true;
                    Singleton<TransferManager>.instance.AddIncomingOffer(reason, offer);
                    return true;
                }
            }

            return false;
        }

        private static bool IsChild(uint citizenID) => Citizen.GetAgeGroup(Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].Age) == Citizen.AgeGroup.Child || Citizen.GetAgeGroup(Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].Age) == Citizen.AgeGroup.Teen;

        private static bool IsSenior(uint citizenID) => Citizen.GetAgeGroup(Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenID].Age) == Citizen.AgeGroup.Senior;

    }
}
