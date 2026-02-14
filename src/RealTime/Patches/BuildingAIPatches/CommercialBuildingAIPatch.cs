// CommercialBuildingAIPatch.cs

namespace RealTime.Patches.BuildingAIPatches
{
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using System.Reflection;
    using ColossalFramework;
    using HarmonyLib;
    using ICities;
    using RealTime.CustomAI;
    using RealTime.GameConnection;

    [HarmonyPatch]
    internal static class CommercialBuildingAIPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        [HarmonyPatch(typeof(CommercialBuildingAI), "SimulationStepActive")]
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
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Call, IsBuildingWorking),
                            new(OpCodes.Brtrue, inst[i + 3].operand),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Stloc_S, 10)
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
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Ldc_I4_0),
                            new(OpCodes.Call, IsBuildingWorking),
                            new(OpCodes.Brfalse, inst[i + 1].operand)
                    ]);
                }
            }

            return inst;
        }

        [HarmonyPatch(typeof(CommercialBuildingAI), "SimulationStepActive")]
        [HarmonyPrefix]
        public static bool SimulationStepActivePrefix(ref Building buildingData, ref byte __state)
        {
            __state = buildingData.m_outgoingProblemTimer;
            if (buildingData.m_customBuffer2 > 0)
            {
                // Simulate some goods become spoiled; additionally, this will cause the buildings to never reach the 'stock full' state.
                // In that state, no visits are possible anymore, so the building gets stuck
                --buildingData.m_customBuffer2;
            }

            return true;
        }

        [HarmonyPatch(typeof(CommercialBuildingAI), "SimulationStepActive")]
        [HarmonyPostfix]
        public static void SimulationStepActivePostfix(CommercialBuildingAI __instance, ushort buildingID, ref Building buildingData, byte __state)
        {
            if (__state != buildingData.m_outgoingProblemTimer && RealTimeBuildingAI != null)
            {
                RealTimeBuildingAI.ProcessBuildingProblems(buildingID, __state);
            }
            if (BuildingManagerConnection.IsHotel(buildingID))
            {
                int aliveCount = 0;
                int hotelTotalCount = 0;
                Citizen.BehaviourData behaviour = default;
                CommonBuildingAIPatch.GetHotelBehaviour(__instance, buildingID, ref buildingData, ref behaviour, ref aliveCount, ref hotelTotalCount);
                buildingData.m_roomUsed = (ushort)hotelTotalCount;
                Singleton<DistrictManager>.instance.m_districts.m_buffer[0].m_hotelData.m_tempHotelVisitors += (uint)hotelTotalCount;
            }
            if (!RealTimeBuildingAI.IsBuildingWorking(buildingID) && Singleton<LoadingManager>.instance.SupportsExpansion(Expansion.Hotels))
            {
                float radius = Singleton<ImmaterialResourceManager>.instance.m_properties.m_hotel.m_commertialBuilding.m_radius + (buildingData.m_width + buildingData.m_length) * 0.25f;
                int rate = Singleton<ImmaterialResourceManager>.instance.m_properties.m_hotel.m_commertialBuilding.m_attraction * buildingData.m_width * buildingData.m_length;
                Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Shopping, rate, buildingData.m_position, radius);
            }
        }

        [HarmonyPatch(typeof(CommercialBuildingAI), "SetGoodsAmount")]
        [HarmonyPrefix]
        public static bool SetGoodsAmount(CommercialBuildingAI __instance, ref Building data, ushort amount)
        {
            if (RealTimeBuildingAI != null && !RealTimeBuildingAI.WeeklyCommericalDeliveries())
            {
                return true;
            }
            if (data.m_customBuffer1 - amount > 0)
            {
                var rnd = new System.Random();
                int custom_amount = rnd.Next(1, 5);
                data.m_customBuffer1 -= (ushort)custom_amount;
                return false;
            }
            return true;
        }

    }
}
