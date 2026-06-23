// IndustrialBuildingAIPatch.cs

namespace RealTime.Patches.BuildingAIPatches
{
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using System.Reflection;
    using HarmonyLib;
    using RealTime.CustomAI;

    [HarmonyPatch]
    internal class IndustrialBuildingAIPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        [HarmonyPatch(typeof(IndustrialBuildingAI), "SimulationStepActive")]
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
                            new(OpCodes.Stloc_S, 9)
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

        [HarmonyPatch(typeof(IndustrialBuildingAI), "SimulationStepActive")]
        [HarmonyPrefix]
        public static bool SimulationStepActivePrefix(ref Building buildingData, ref byte __state)
        {
            __state = buildingData.m_outgoingProblemTimer;
            return true;
        }

        [HarmonyPatch(typeof(IndustrialBuildingAI), "SimulationStepActive")]
        [HarmonyPostfix]
        public static void SimulationStepActivePostfix(ushort buildingID, ref Building buildingData, byte __state)
        {
            if (__state != buildingData.m_outgoingProblemTimer && RealTimeBuildingAI != null)
            {
                RealTimeBuildingAI.ProcessBuildingProblems(buildingID, __state);
            }
        }
    }
}
