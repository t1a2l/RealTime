// PrivateBuildingAIPatch.cs

namespace RealTime.Patches.BuildingAIPatches
{
    using System;
    using System.Reflection;
    using ColossalFramework.Math;
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.Config;
    using RealTime.Core;
    using RealTime.CustomAI;
    using RealTime.GameConnection;
    using RealTime.Managers;
    using UnityEngine;
    using SkyTools.Localization;
    using RealTime.Localization;

    [HarmonyPatch]
    internal class PrivateBuildingAIPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        /// <summary>Gets or sets the mod localization.</summary>
        public static ILocalizationProvider localizationProvider { get; set; }

        private delegate void CommonBuildingAICreateBuildingDelegate(CommonBuildingAI __instance, ushort buildingID, ref Building data);
        private static readonly CommonBuildingAICreateBuildingDelegate BaseCreateBuilding = AccessTools.MethodDelegate<CommonBuildingAICreateBuildingDelegate>(typeof(CommonBuildingAI).GetMethod("CreateBuilding", BindingFlags.Instance | BindingFlags.Public), null, false);

        [HarmonyPatch(typeof(PrivateBuildingAI), "CreateBuilding")]
        [HarmonyPrefix]
        public static bool CreateBuilding(PrivateBuildingAI __instance, ushort buildingID, ref Building data)
        {
            var buildingInfo = data.Info;
            if (!BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID) && BuildingWorkTimeManager.ShouldHaveBuildingWorkTime(buildingID))
            {
                BuildingWorkTimeManager.CreateBuildingWorkTime(buildingID, buildingInfo);

                if (BuildingWorkTimeManager.PrefabExist(buildingInfo))
                {
                    var buildignPrefab = BuildingWorkTimeManager.GetPrefab(buildingInfo);
                    UpdateBuildingSettings.SetBuildingToPrefab(buildingID, buildignPrefab);
                }
                else if (BuildingWorkTimeGlobalConfig.Config.GlobalSettingsExist(buildingInfo))
                {
                    var buildignGlobal = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo);
                    UpdateBuildingSettings.SetBuildingToGlobal(buildingID, buildignGlobal);
                }
            }
            if (BuildingManagerConnection.IsHotel(buildingID))
            {
                BaseCreateBuilding(__instance, buildingID, ref data);
                data.m_level = (byte)__instance.m_info.m_class.m_level;
                __instance.CalculateWorkplaceCount((ItemClass.Level)data.m_level, new Randomizer(buildingID), data.Width, data.Length, out int level, out int level2, out int level3, out int level4);
                __instance.AdjustWorkplaceCount(buildingID, ref data, ref level, ref level2, ref level3, ref level4);
                int workCount = level + level2 + level3 + level4;
                int visitCount = __instance.CalculateVisitplaceCount((ItemClass.Level)data.m_level, new Randomizer(buildingID), data.Width, data.Length);
                int hotelRoomCount = visitCount;
                if (BuildingWorkTimeManager.HotelNamesList.ContainsKey(buildingInfo.name))
                {
                    hotelRoomCount = BuildingWorkTimeManager.HotelNamesList[buildingInfo.name];
                }
                visitCount = hotelRoomCount * 20 / 100;
                data.m_roomMax = (ushort)hotelRoomCount;
                Singleton<CitizenManager>.instance.CreateUnits(out data.m_citizenUnits, ref Singleton<SimulationManager>.instance.m_randomizer, buildingID, 0, 0, workCount, visitCount, 0, 0, hotelRoomCount);
                if (!HotelManager.HotelExist(buildingID))
                {
                    HotelManager.AddHotel(buildingID);
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPatch(typeof(PrivateBuildingAI), "BuildingLoaded")]
        [HarmonyPrefix]
        public static bool BuildingLoaded(PrivateBuildingAI __instance, ushort buildingID, ref Building data, uint version)
        {
            var buildingInfo = data.Info;
            if (!BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID) && BuildingWorkTimeManager.ShouldHaveBuildingWorkTime(buildingID))
            {
                BuildingWorkTimeManager.CreateBuildingWorkTime(buildingID, buildingInfo);

                if (BuildingWorkTimeManager.PrefabExist(buildingInfo))
                {
                    var buildignPrefab = BuildingWorkTimeManager.GetPrefab(buildingInfo);
                    UpdateBuildingSettings.SetBuildingToPrefab(buildingID, buildignPrefab);
                }
                else if (BuildingWorkTimeGlobalConfig.Config.GlobalSettingsExist(buildingInfo))
                {
                    var buildignGlobal = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo);
                    UpdateBuildingSettings.SetBuildingToGlobal(buildingID, buildignGlobal);
                }
            }
            if (BuildingManagerConnection.IsHotel(buildingID))
            {
                data.m_level = (byte)Mathf.Max(data.m_level, (int)__instance.m_info.m_class.m_level);
                __instance.CalculateWorkplaceCount((ItemClass.Level)data.m_level, new Randomizer(buildingID), data.Width, data.Length, out int level, out int level2, out int level3, out int level4);
                __instance.AdjustWorkplaceCount(buildingID, ref data, ref level, ref level2, ref level3, ref level4);
                int workCount = level + level2 + level3 + level4;
                int visitCount = __instance.CalculateVisitplaceCount((ItemClass.Level)data.m_level, new Randomizer(buildingID), data.Width, data.Length);
                int hotelRoomCount = visitCount;
                if (BuildingWorkTimeManager.HotelNamesList.ContainsKey(buildingInfo.name))
                {
                    hotelRoomCount = BuildingWorkTimeManager.HotelNamesList[buildingInfo.name];
                }
                visitCount = hotelRoomCount * 20 / 100;
                EnsureCitizenUnits(buildingID, ref data, 0, workCount, visitCount, 0, hotelRoomCount);
                data.m_roomMax = (ushort)hotelRoomCount;
                if (!HotelManager.HotelExist(buildingID) && data.m_roomUsed < data.m_roomMax)
                {
                    HotelManager.AddHotel(buildingID);
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPatch(typeof(PrivateBuildingAI), "HandleWorkers")]
        [HarmonyPrefix]
        public static bool HandleWorkersPrefix(ref Building buildingData, ref byte __state)
        {
            __state = buildingData.m_workerProblemTimer;
            return true;
        }

        [HarmonyPatch(typeof(PrivateBuildingAI), "HandleWorkers")]
        [HarmonyPostfix]
        public static void HandleWorkersPostfix(ushort buildingID, ref Building buildingData, byte __state)
        {
            if (__state != buildingData.m_workerProblemTimer && RealTimeBuildingAI != null)
            {
                RealTimeBuildingAI.ProcessWorkerProblems(buildingID, __state);
            }
        }

        [HarmonyPatch(typeof(PrivateBuildingAI), "SimulationStepActive")]
        [HarmonyPrefix]
        public static void SimulationStepActive(ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
        {
            if ((buildingData.m_flags & Building.Flags.Abandoned) != 0 || (buildingData.m_flags & Building.Flags.Collapsed) != 0)
            {
                if (BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID))
                {
                    BuildingWorkTimeManager.RemoveBuildingWorkTime(buildingID);
                }
                if (BuildingManagerConnection.IsHotel(buildingID) && HotelManager.HotelExist(buildingID))
                {
                    HotelManager.RemoveHotel(buildingID);
                }
            }
        }

        [HarmonyPatch(typeof(PrivateBuildingAI), "GetConstructionTime")]
        [HarmonyPrefix]
        public static bool GetConstructionTime(ref int __result)
        {
            if (RealTimeBuildingAI != null)
            {
                __result = RealTimeBuildingAI.GetConstructionTime();
            }
            return false;
        }

        [HarmonyPatch(typeof(PrivateBuildingAI), "GetUpgradeInfo")]
        [HarmonyPrefix]
        public static bool GetUpgradeInfo(ushort buildingID, ref Building data, ref BuildingInfo __result)
        {
            if (!RealTimeCore.ApplyBuildingPatch)
            {
                return true;
            }

            if ((data.m_flags & Building.Flags.Upgrading) != 0)
            {
                return true;
            }

            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.CanBuildOrUpgrade(data.Info.GetService(), buildingID))
            {
                __result = null;
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(PrivateBuildingAI), "GetLocalizedStatus")]
        [HarmonyPostfix]
        public static void GetLocalizedStatus(ushort buildingID, ref Building data, ref string __result)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                __result = localizationProvider.Translate(TranslationKeys.ClosedBuilding);
            }
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
