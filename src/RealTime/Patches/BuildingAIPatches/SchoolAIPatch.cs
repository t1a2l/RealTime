// SchoolAIPatch.cs

namespace RealTime.Patches.BuildingAIPatches
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.CustomAI;
    using UnityEngine;

    [HarmonyPatch]
    internal static class SchoolAIPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        private delegate void PlayerBuildingAICreateBuildingDelegate(PlayerBuildingAI __instance, ushort buildingID, ref Building data);
        private static readonly PlayerBuildingAICreateBuildingDelegate BaseCreateBuilding = AccessTools.MethodDelegate<PlayerBuildingAICreateBuildingDelegate>(typeof(PlayerBuildingAI).GetMethod("CreateBuilding", BindingFlags.Instance | BindingFlags.Public), null, false);

        private delegate void PlayerBuildingAIBuildingLoadedDelegate(PlayerBuildingAI __instance, ushort buildingID, ref Building data, uint version);
        private static readonly PlayerBuildingAIBuildingLoadedDelegate BaseBuildingLoaded = AccessTools.MethodDelegate<PlayerBuildingAIBuildingLoadedDelegate>(typeof(PlayerBuildingAI).GetMethod("BuildingLoaded", BindingFlags.Instance | BindingFlags.Public), null, false);

        private delegate void PlayerBuildingAIEndRelocatingDelegate(PlayerBuildingAI __instance, ushort buildingID, ref Building data);
        private static readonly PlayerBuildingAIEndRelocatingDelegate BaseEndRelocating = AccessTools.MethodDelegate<PlayerBuildingAIEndRelocatingDelegate>(typeof(PlayerBuildingAI).GetMethod("EndRelocating", BindingFlags.Instance | BindingFlags.Public), null, false);

        private delegate void PlayerBuildingAIProduceGoodsDelegate(PlayerBuildingAI __instance, ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, int finalProductionRate, ref Citizen.BehaviourData behaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveVisitorCount, int totalVisitorCount, int visitPlaceCount);
        private static readonly PlayerBuildingAIProduceGoodsDelegate BaseProduceGoods = AccessTools.MethodDelegate<PlayerBuildingAIProduceGoodsDelegate>(typeof(PlayerBuildingAI).GetMethod("ProduceGoods", BindingFlags.Instance | BindingFlags.NonPublic), null, false);

        [HarmonyPatch(typeof(SchoolAI), "CreateBuilding")]
        [HarmonyPrefix]
        public static bool CreateBuilding(SchoolAI __instance, ushort buildingID, ref Building data)
        {
            if (data.Info.GetAI() is CampusBuildingAI campusBuildingAI && (data.Info.name.Contains("Cafeteria") || data.Info.name.Contains("Gymnasium")))
            {
                BaseCreateBuilding(__instance, buildingID, ref data);
                int workCount = __instance.m_workPlaceCount0 + __instance.m_workPlaceCount1 + __instance.m_workPlaceCount2 + __instance.m_workPlaceCount3;
                campusBuildingAI.m_studentCount = 0;
                Singleton<CitizenManager>.instance.CreateUnits(out data.m_citizenUnits, ref Singleton<SimulationManager>.instance.m_randomizer, buildingID, 0, 0, workCount, 300, 0);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(SchoolAI), "BuildingLoaded")]
        [HarmonyPrefix]
        public static bool BuildingLoaded(SchoolAI __instance, ushort buildingID, ref Building data, uint version)
        {
            if (data.Info.GetAI() is CampusBuildingAI campusBuildingAI && (data.Info.name.Contains("Cafeteria") || data.Info.name.Contains("Gymnasium")))
            {
                BaseBuildingLoaded(__instance, buildingID, ref data, version);
                int workCount = __instance.m_workPlaceCount0 + __instance.m_workPlaceCount1 + __instance.m_workPlaceCount2 + __instance.m_workPlaceCount3;
                campusBuildingAI.m_studentCount = 0;
                EnsureCitizenUnits(buildingID, ref data, 0, workCount, 300, 0);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(SchoolAI), "EndRelocating")]
        [HarmonyPrefix]
        public static bool EndRelocating(SchoolAI __instance, ushort buildingID, ref Building data)
        {
            if (data.Info.GetAI() is CampusBuildingAI campusBuildingAI && (data.Info.name.Contains("Cafeteria") || data.Info.name.Contains("Gymnasium")))
            {
                BaseEndRelocating(__instance, buildingID, ref data);
                int workCount = __instance.m_workPlaceCount0 + __instance.m_workPlaceCount1 + __instance.m_workPlaceCount2 + __instance.m_workPlaceCount3;
                campusBuildingAI.m_studentCount = 0;
                EnsureCitizenUnits(buildingID, ref data, 0, workCount, 300, 0);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(SchoolAI), "ProduceGoods")]
        [HarmonyPrefix]
        public static bool ProduceGoods(SchoolAI __instance, ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, int finalProductionRate, ref Citizen.BehaviourData behaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveVisitorCount, int totalVisitorCount, int visitPlaceCount)
        {
            if (buildingData.Info.GetAI() is CampusBuildingAI && (buildingData.Info.name.Contains("Cafeteria") || buildingData.Info.name.Contains("Gymnasium")))
            {
                int visitCount = 300;
                BaseProduceGoods(__instance, buildingID, ref buildingData, ref frameData, productionRate, finalProductionRate, ref behaviour, aliveWorkerCount, totalWorkerCount, workPlaceCount, aliveVisitorCount, totalVisitorCount, visitPlaceCount);
                int aliveCount = 0;
                int totalCount = 0;
                GetVisitBehaviour(__instance, buildingID, ref buildingData, ref behaviour, ref aliveCount, ref totalCount);
                if (aliveCount != 0)
                {
                    behaviour.m_crimeAccumulation = behaviour.m_crimeAccumulation * aliveWorkerCount / (aliveWorkerCount + aliveCount);
                }
                var instance = Singleton<DistrictManager>.instance;
                byte district = instance.GetDistrict(buildingData.m_position);
                var servicePolicies = instance.m_districts.m_buffer[district].m_servicePolicies;
                int num = productionRate * __instance.EducationAccumulation / 100;
                if ((servicePolicies & DistrictPolicies.Services.EducationalBlimps) != 0)
                {
                    num = (num * 21 + 10) / 20;
                    instance.m_districts.m_buffer[district].m_servicePoliciesEffect |= DistrictPolicies.Services.EducationalBlimps;
                }
                if (num != 0)
                {
                    Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.EducationUniversity, num, buildingData.m_position, __instance.m_educationRadius);
                }
                if (finalProductionRate == 0)
                {
                    return false;
                }
                buildingData.m_customBuffer1 = (ushort)aliveCount;
                if ((servicePolicies & DistrictPolicies.Services.SchoolsOut) != 0)
                {
                    instance.m_districts.m_buffer[district].m_servicePoliciesEffect |= DistrictPolicies.Services.SchoolsOut;
                }
                int num2 = Mathf.Min((finalProductionRate * visitCount + 99) / 100, visitCount * 5 / 4);
                int num3 = num2 - totalVisitorCount;
                var campusBuildingAI = buildingData.Info.m_buildingAI as CampusBuildingAI;
                campusBuildingAI.HandleDead2(buildingID, ref buildingData, ref behaviour, totalWorkerCount + totalCount);
                if (num3 >= 1)
                {
                    TransferManager.TransferOffer offer = default;
                    offer.Priority = Mathf.Max(1, num3 * 8 / num2);
                    offer.Building = buildingID;
                    offer.Position = buildingData.m_position;
                    offer.Amount = num3;
                    offer.Active = false;
                    if (buildingData.Info.name.Contains("Cafeteria"))
                    {
                        switch (Singleton<SimulationManager>.instance.m_randomizer.Int32(8u))
                        {
                            case 0:
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Shopping, offer);
                                break;
                            case 1:
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.ShoppingB, offer);
                                break;
                            case 2:
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.ShoppingC, offer);
                                break;
                            case 3:
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.ShoppingD, offer);
                                break;
                            case 4:
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.ShoppingE, offer);
                                break;
                            case 5:
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.ShoppingF, offer);
                                break;
                            case 6:
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.ShoppingG, offer);
                                break;
                            case 7:
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.ShoppingH, offer);
                                break;
                            default:
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Shopping, offer);
                                break;
                        }
                    }
                    else if (buildingData.Info.name.Contains("Gymnasium"))
                    {
                        switch (Singleton<SimulationManager>.instance.m_randomizer.Int32(4u))
                        {
                            case 0:
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Entertainment, offer);
                                break;
                            case 1:
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.EntertainmentB, offer);
                                break;
                            case 2:
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.EntertainmentC, offer);
                                break;
                            case 3:
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.EntertainmentD, offer);
                                break;
                            default:
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Entertainment, offer);
                                break;
                        }
                    }
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(SchoolAI), "GetCurrentRange")]
        [HarmonyPrefix]
        private static bool GetCurrentRange(SchoolAI __instance, ushort buildingID, ref Building data, ref float __result)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                int num = data.m_productionRate;
                if ((data.m_flags & Building.Flags.Evacuating) != 0)
                {
                    num = 0;
                }
                else if ((data.m_flags & Building.Flags.RateReduced) != 0)
                {
                    num = Mathf.Min(num, 50);
                }
                int budget = Singleton<EconomyManager>.instance.GetBudget(__instance.m_info.m_class);
                num = PlayerBuildingAI.GetProductionRate(num, budget);
                __result = num * __instance.m_educationRadius * 0.01f;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(SchoolAI), "GetColor")]
        [HarmonyPrefix]
        public static bool GetColor(SchoolAI __instance, ushort buildingID, ref Building data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
        {
            if (infoMode == InfoManager.InfoMode.Education)
            {
                var level = ItemClass.Level.None;
                switch (subInfoMode)
                {
                    case InfoManager.SubInfoMode.Default:
                        level = ItemClass.Level.Level1;
                        break;
                    case InfoManager.SubInfoMode.WaterPower:
                        level = ItemClass.Level.Level2;
                        break;
                    case InfoManager.SubInfoMode.WindPower:
                        level = ItemClass.Level.Level3;
                        break;
                }
                if (level == __instance.m_info.m_class.m_level && __instance.m_info.m_class.m_service == ItemClass.Service.Education)
                {
                    if (data.m_productionRate > 0)
                    {
                        __result = Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_activeColor;
                    }
                    else
                    {
                        __result = Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_inactiveColor;
                    }
                    return false;
                }
            }
            return true;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(CommonBuildingAI), "GetVisitBehaviour")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void GetVisitBehaviour(object instance, ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount)
        {
            string message = "GetVisitBehaviour reverse Harmony patch wasn't applied";
            Debug.LogError(message);
            throw new NotImplementedException(message);
        }

        private static void EnsureCitizenUnits(ushort buildingID, ref Building data, int homeCount = 0, int workCount = 0, int visitCount = 0, int studentCount = 0, int hotelCount = 0)
        {
            if ((data.m_flags & (Building.Flags.Abandoned | Building.Flags.Collapsed)) != 0)
            {
                return;
            }
            var wealthLevel = Citizen.GetWealthLevel((ItemClass.Level)data.m_level);
            var instance = Singleton<CitizenManager>.instance;
            uint num = 0u;
            uint num2 = data.m_citizenUnits;
            int num3 = 0;
            while (num2 != 0)
            {
                var flags = instance.m_units.m_buffer[num2].m_flags;
                if ((flags & CitizenUnit.Flags.Home) != 0)
                {
                    instance.m_units.m_buffer[num2].SetWealthLevel(wealthLevel);
                    homeCount--;
                }
                if ((flags & CitizenUnit.Flags.Work) != 0)
                {
                    workCount -= 5;
                }
                if ((flags & CitizenUnit.Flags.Visit) != 0)
                {
                    visitCount -= 5;
                }
                if ((flags & CitizenUnit.Flags.Student) != 0)
                {
                    studentCount -= 5;
                }
                if ((flags & CitizenUnit.Flags.Hotel) != 0)
                {
                    hotelCount -= 5;
                }
                num = num2;
                num2 = instance.m_units.m_buffer[num2].m_nextUnit;
                if (++num3 > 524288)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
            homeCount = Mathf.Max(0, homeCount);
            workCount = Mathf.Max(0, workCount);
            visitCount = Mathf.Max(0, visitCount);
            studentCount = Mathf.Max(0, studentCount);
            hotelCount = Mathf.Max(0, hotelCount);
            if (homeCount == 0 && workCount == 0 && visitCount == 0 && studentCount == 0 && hotelCount == 0)
            {
                return;
            }
            if (instance.CreateUnits(out uint firstUnit, ref Singleton<SimulationManager>.instance.m_randomizer, buildingID, 0, homeCount, workCount, visitCount, 0, studentCount, hotelCount))
            {
                if (num != 0)
                {
                    instance.m_units.m_buffer[num].m_nextUnit = firstUnit;
                }
                else
                {
                    data.m_citizenUnits = firstUnit;
                }
            }
        }

    }
}
