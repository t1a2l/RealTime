// BuildingManagerPatch.cs

namespace RealTime.Patches.BuildingAIPatches
{
    using HarmonyLib;
    using RealTime.Core;
    using RealTime.CustomAI;
    using RealTime.GameConnection;
    using RealTime.Managers;

    [HarmonyPatch]
    internal class BuildingManagerPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        [HarmonyPatch(typeof(BuildingManager), "CreateBuilding")]
        [HarmonyPrefix]
        public static bool CreateBuildingPrefix(BuildingInfo info, ref bool __result)
        {
            if (!RealTimeCore.ApplyBuildingPatch)
            {
                return true;
            }

            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.CanBuildOrUpgrade(info.GetService()))
            {
                __result = false;
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(BuildingManager), "CreateBuilding")]
        [HarmonyPostfix]
        public static void CreateBuildingPostfix(ushort building, BuildingInfo info, ref bool __result)
        {
            if (!RealTimeCore.ApplyBuildingPatch)
            {
                return;
            }

            if (__result && RealTimeBuildingAI != null)
            {
                RealTimeBuildingAI.RegisterConstructingBuilding(building, info.GetService());
            }
        }

        [HarmonyPatch(typeof(BuildingManager), "ReleaseBuilding")]
        [HarmonyPostfix]
        public static void ReleaseBuildingPostfix(ushort buildingID, ref Building data)
        {
            if (BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID))
            {
                BuildingWorkTimeManager.RemoveBuildingWorkTime(buildingID);
            }
            if (BuildingManagerConnection.IsHotel(buildingID) && HotelManager.HotelExist(buildingID))
            {
                HotelManager.RemoveHotel(buildingID);
            }
            if (data.Info.GetAI() is MainCampusBuildingAI && AcademicYearManager.MainCampusBuildingExist(buildingID))
            {
                AcademicYearManager.DeleteAcademicYearData(buildingID);
            }

            GarbageSlowdownManager._garbageAccumulator[buildingID] = 0f;
        }
    }
}
