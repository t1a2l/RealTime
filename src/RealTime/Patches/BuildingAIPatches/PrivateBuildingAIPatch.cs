// PrivateBuildingAIPatch.cs

namespace RealTime.Patches.BuildingAIPatches
{
    using System.Reflection;
    using HarmonyLib;
    using RealTime.Core;
    using RealTime.CustomAI;
    using RealTime.GameConnection;
    using RealTime.Managers;
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
            BuildingWorkTimeManager.BuildingWorkTimeCheck(buildingID, buildingInfo);
            CommercialBuildingTypesManager.CommercialBuildingTypeCheck(buildingID, buildingInfo);
            if (BuildingManagerConnection.IsHotel(buildingID))
            {
                BaseCreateBuilding(__instance, buildingID, ref data);
                HotelManager.HotelCheck(buildingID, ref data, "create");
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPatch(typeof(PrivateBuildingAI), "BuildingLoaded")]
        [HarmonyPrefix]
        public static bool BuildingLoaded(PrivateBuildingAI __instance, ushort buildingID, ref Building data, uint version) => !BuildingManagerConnection.IsHotel(buildingID);

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

    }
}
