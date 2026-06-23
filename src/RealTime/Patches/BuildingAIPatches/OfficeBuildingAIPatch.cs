// OfficeBuildingAIPatch.cs

namespace RealTime.Patches.BuildingAIPatches
{
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using System.Reflection;
    using ColossalFramework;
    using HarmonyLib;
    using ICities;
    using RealTime.CustomAI;

    [HarmonyPatch]
    internal class OfficeBuildingAIPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        [HarmonyPatch(typeof(OfficeBuildingAI), "SimulationStepActive")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> TranspileSimulationStepActive(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var IsBuildingWorkingInstance = AccessTools.PropertyGetter(typeof(BuildingAIPatch), nameof(RealTimeBuildingAI));
            var IsBuildingWorking = typeof(RealTimeBuildingAI).GetMethod("IsBuildingWorking", BindingFlags.Public | BindingFlags.Instance);
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
                            new(OpCodes.Call, IsBuildingWorking),
                            new(OpCodes.Brtrue, inst[i + 3].operand),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Stloc_S, 11)
                    ]);
                    break;
                }
            }

            for (int i = 0; i < inst.Count; i++)
            {
                if (inst[i].opcode == OpCodes.Ldfld && inst[i].operand == typeof(Building).GetField("m_fireIntensity"))
                {
                    inst.InsertRange(i + 2, [
                        new(OpCodes.Call, IsBuildingWorkingInstance),
                            new(OpCodes.Ldarg_1),
                            new(OpCodes.Call, IsBuildingWorking),
                            new(OpCodes.Brfalse, inst[i + 1].operand)
                    ]);
                }
            }

            return inst;
        }

        [HarmonyPatch(typeof(OfficeBuildingAI), "SimulationStepActive")]
        [HarmonyPrefix]
        public static bool SimulationStepActivePrefix(ref Building buildingData, ref byte __state)
        {
            __state = buildingData.m_outgoingProblemTimer;
            return true;
        }

        [HarmonyPatch(typeof(OfficeBuildingAI), "SimulationStepActive")]
        [HarmonyPostfix]
        public static void SimulationStepActivePostfix(ushort buildingID, ref Building buildingData, byte __state)
        {
            if (__state != buildingData.m_outgoingProblemTimer && RealTimeBuildingAI != null)
            {
                RealTimeBuildingAI.ProcessBuildingProblems(buildingID, __state);
            }
            if (!RealTimeBuildingAI.IsBuildingWorking(buildingID) && Singleton<LoadingManager>.instance.SupportsExpansion(Expansion.Hotels))
            {
                float radius = Singleton<ImmaterialResourceManager>.instance.m_properties.m_hotel.m_officeBuilding.m_radius + (buildingData.m_width + buildingData.m_length) * 0.25f;
                int rate = Singleton<ImmaterialResourceManager>.instance.m_properties.m_hotel.m_officeBuilding.m_attraction * buildingData.m_width * buildingData.m_length;
                Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Business, rate, buildingData.m_position, radius);
            }
        }
    }
}
