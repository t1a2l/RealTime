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


        [HarmonyPatch]
        private sealed class ResidentAI_UpdateHealth
        {
            [HarmonyPatch(typeof(ResidentAI), "UpdateHealth")]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> TranspileUpdateHealth(IEnumerable<CodeInstruction> instructions)
            {
                var inst = new List<CodeInstruction>(instructions);

                for (int i = 0; i < inst.Count; i++)
                {
                    if (inst[i].LoadsConstant(1000) && inst[i+1].opcode == OpCodes.Div)
                    {
                        inst[i].operand = 10000;
                    }
                }
                return inst;
            }
        }

        [HarmonyPatch]
        private sealed class ResidentAI_UpdateLocation
        {
            [HarmonyPatch(typeof(ResidentAI), "UpdateLocation")]
            [HarmonyPrefix]
            private static bool Prefix(ResidentAI __instance, uint citizenID, ref Citizen data)
            {
                if (RealTimeResidentAI != null)
                {
                    RealTimeResidentAI.UpdateLocation(__instance, citizenID, ref data);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        private sealed class ResidentAI_UpdateAge
        {
            [HarmonyPatch(typeof(ResidentAI), "UpdateAge")]
            [HarmonyPrefix]
            private static bool Prefix(ref bool __result)
            {
                if(!RealTimeCore.ApplyCitizenPatch)
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
        }

        [HarmonyPatch]
        private sealed class ResidentAI_CanMakeBabies
        {
            [HarmonyPatch(typeof(ResidentAI), "CanMakeBabies")]
            [HarmonyPrefix]
            private static bool Prefix(uint citizenID, ref Citizen data, ref bool __result)
            {
                if(!RealTimeCore.ApplyCitizenPatch)
                {
                    return true;
                }

                if (RealTimeResidentAI != null)
                {
                    __result = RealTimeResidentAI.CanMakeBabies(citizenID, ref data);
                }

                return false;
            }
        }

        [HarmonyPatch]
        private sealed class ResidentAI_FinishSchoolOrWork
        {
            [HarmonyPatch(typeof(ResidentAI), "FinishSchoolOrWork")]
            [HarmonyPrefix]
            private static bool Prefix(uint citizenID, ref Citizen data)
            {
                if (data.m_workBuilding == 0)
                {
                    return true;
                }
                var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_workBuilding];
                if(building.Info.GetAI() is SchoolAI || building.Info.GetAI() is CampusBuildingAI || building.Info.GetAI() is UniqueFacultyAI)
                {
                    const Building.Flags restrictedFlags = Building.Flags.Deleted | Building.Flags.Evacuating |
                        Building.Flags.Flooded | Building.Flags.Collapsed | Building.Flags.BurnedDown | Building.Flags.RoadAccessFailed;

                    if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(data.m_workBuilding) && (building.m_flags & restrictedFlags) == 0)
                    {
                        return false;
                    }

                    var instance = Singleton<DistrictManager>.instance;
                    byte b = instance.GetPark(building.m_position);
                    if(b != 0 && instance.m_parks.m_buffer[b].IsCampus)
                    {
                        ushort mainCampusBuilding = instance.m_parks.m_buffer[b].m_mainGate;
                        ushort eventIndex = Singleton<BuildingManager>.instance.m_buildings.m_buffer[mainCampusBuilding].m_eventIndex;
                        if (eventIndex != 0 && (Singleton<EventManager>.instance.m_events.m_buffer[eventIndex].m_flags & EventData.Flags.Completed) == 0)
                        {
                            return false;
                        }
                    }

                    
                }
                return true;
            }
        }

        [HarmonyPatch]
        private sealed class ResidentAI_SimulationStep
        {
            [HarmonyPatch(typeof(ResidentAI), "SimulationStep",
                [typeof(ushort), typeof(CitizenInstance), typeof(CitizenInstance.Frame), typeof(bool)],
                [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal])]
            [HarmonyPostfix]
            private static void Postfix(ResidentAI __instance, ushort instanceID, ref CitizenInstance citizenData)
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
        }

        [HarmonyPatch]
        private sealed class ResidentAI_GetColor
        {
            [HarmonyPatch(typeof(ResidentAI), "GetColor")]
            [HarmonyPrefix]
            private static bool Prefix(ResidentAI __instance, ushort instanceID, ref CitizenInstance data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
            {
                if (instanceID == 0)
                {
                    return true;
                }

                if (infoMode == InfoManager.InfoMode.Density)
                {
                    if(Chosen_Building == 0 && WorldInfoPanel.GetCurrentInstanceID().Building == 0)
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
        }

        [HarmonyPatch]
        private sealed class ResidentAI_StartTransfer
        {
            [HarmonyPatch(typeof(ResidentAI), "StartTransfer")]
            [HarmonyPrefix]
            private static bool Prefix(ResidentAI __instance, uint citizenID, ref Citizen data, TransferManager.TransferReason reason, TransferManager.TransferOffer offer)
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
                            if (building.Info.m_class.m_service == ItemClass.Service.Commercial && building.Info.m_class.m_subService == ItemClass.SubService.CommercialLeisure)
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
                            if (building.Info.GetAI() is CampusBuildingAI && building.Info.name.Contains("Cafeteria"))
                            {
                                ushort currentBuilding = data.GetBuildingByLocation();
                                if ((data.m_flags & Citizen.Flags.Student) == 0)
                                {
                                    return false;
                                }
                                if(!BuildingManagerConnection.CheckSameCampusArea(currentBuilding, offer.Building))
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
                                if(building.m_eventIndex == 0)
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
                            if (building.Info.GetAI() is CampusBuildingAI && building.Info.name.Contains("Gymnasium"))
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
        }
    }
}
