// PlayerBuildingAIPatch.cs

namespace RealTime.Patches.BuildingAIPatches
{
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using System.Reflection;
    using HarmonyLib;
    using RealTime.Config;
    using RealTime.CustomAI;
    using RealTime.GameConnection;
    using RealTime.Managers;
    using SkyTools.Localization;
    using RealTime.Localization;

    [HarmonyPatch]
    internal class PlayerBuildingAIPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        /// <summary>Gets or sets the mod localization.</summary>
        public static ILocalizationProvider localizationProvider { get; set; }

        [HarmonyPatch(typeof(PlayerBuildingAI), "CreateBuilding")]
        [HarmonyPrefix]
        public static void CreateBuilding(PlayerBuildingAI __instance, ushort buildingID, ref Building data)
        {
            if (!BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID) && BuildingWorkTimeManager.ShouldHaveBuildingWorkTime(buildingID))
            {
                var buildingInfo = data.Info;
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

                if (BuildingManagerConnection.IsHotel(buildingID) && !HotelManager.HotelExist(buildingID))
                {
                    HotelManager.AddHotel(buildingID);
                } 
            }

            if (data.Info.GetAI() is MainCampusBuildingAI && !AcademicYearManager.MainCampusBuildingExist(buildingID))
            {
                AcademicYearManager.CreateAcademicYearData(buildingID);
            }
        }

        [HarmonyPatch(typeof(PlayerBuildingAI), "BuildingLoaded")]
        [HarmonyPrefix]
        public static void BuildingLoaded(PlayerBuildingAI __instance, ushort buildingID, ref Building data)
        {
            if (!BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID) && BuildingWorkTimeManager.ShouldHaveBuildingWorkTime(buildingID))
            {
                var buildingInfo = data.Info;
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

                if (BuildingManagerConnection.IsHotel(buildingID) && !HotelManager.HotelExist(buildingID) && data.m_roomUsed < data.m_roomMax)
                {
                    HotelManager.AddHotel(buildingID);
                }
            }

            if (data.Info.GetAI() is MainCampusBuildingAI && !AcademicYearManager.MainCampusBuildingExist(buildingID))
            {
                AcademicYearManager.CreateAcademicYearDataExistingCampus(buildingID);
            }
        }

        [HarmonyPatch(typeof(PlayerBuildingAI), "SimulationStepActive")]
        public static void SimulationStepActive(ushort buildingID, ref Building buildingData, ref Building.Frame frameData)
        {
            if ((buildingData.m_flags & Building.Flags.Collapsed) != 0)
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

        [HarmonyPatch(typeof(PlayerBuildingAI), "GetLocalizedStatus")]
        [HarmonyPostfix]
        public static void GetLocalizedStatus(ushort buildingID, ref Building data, ref string __result)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
            {
                __result = localizationProvider.Translate(TranslationKeys.ClosedBuilding);
            }
        }

        [HarmonyPatch(typeof(PlayerBuildingAI), "SimulationStepActive")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> TranspileSimulationStepActive(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var IsBuildingWorkingInstance = AccessTools.PropertyGetter(typeof(BuildingAIPatch), nameof(RealTimeBuildingAI));
            var IsBuildingWorking = typeof(RealTimeBuildingAI).GetMethod("IsBuildingWorking", BindingFlags.Public | BindingFlags.Instance);
            var IsSchoolBuilding = typeof(RealTimeBuildingAI).GetMethod("IsSchoolBuilding", BindingFlags.Public | BindingFlags.Instance);
            var inst = new List<CodeInstruction>(instructions);

            for (int i = 0; i < inst.Count; i++)
            {
                if (inst[i].opcode == OpCodes.Ldfld &&
                    inst[i].operand == typeof(Building).GetField("m_flags") &&
                    inst[i + 1].opcode == OpCodes.Ldc_I4 &&
                    inst[i + 1].operand is int s &&
                    s == 32768 &&
                    inst[i - 1].opcode == OpCodes.Ldarg_2)
                {
                    inst.InsertRange(i - 1, [
                        new(OpCodes.Call, IsBuildingWorkingInstance),
                            new(OpCodes.Ldarg_1),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Call, IsBuildingWorking),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Ceq),
                            new(OpCodes.Call, IsBuildingWorkingInstance),
                            new(OpCodes.Ldarg_1),
                            new(OpCodes.Call, IsSchoolBuilding),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Ceq),
                            new(OpCodes.And),
                            new(OpCodes.Brfalse, inst[i + 3].operand),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Stloc_1)
                    ]);
                    break;
                }
            }

            return inst;
        }

    }
}
