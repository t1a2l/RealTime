// HumanAIPatch.cs

namespace RealTime.Patches
{
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.CustomAI;
    using SkyTools.Tools;
    using static RenderManager;

    /// <summary>
    /// A static class that provides the patch objects for the Human AI.
    /// </summary>
    [HarmonyPatch]
    internal static class HumanAIPatch
    {
        /// <summary>
        /// Gets or sets the custom AI object for resident citizens.
        /// </summary>
        public static RealTimeResidentAI<ResidentAI, Citizen> RealTimeResidentAI { get; set; }

        /// <summary>
        ///  Gets or sets the custom AI object for buildings.
        /// </summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        [HarmonyPatch(typeof(HumanAI), "StartMoving",
                [typeof(uint), typeof(Citizen), typeof(ushort), typeof(ushort)],
                [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPrefix]
        public static bool StartMoving(HumanAI __instance, uint citizenID, ref Citizen data, ushort sourceBuilding, ushort targetBuilding, ref bool __result)
        {
            if (targetBuilding == sourceBuilding)
            {
                Log.Debug(LogCategory.Movement, $"Citizen {citizenID} - targetBuilding and sourceBuilding are the same, run the original function.");
                return true;
            }

            if (targetBuilding == 0)
            {
                Log.Debug(LogCategory.Movement, $"Citizen {citizenID} targetBuilding is 0, run the original function.");
                return true;
            }

            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetBuilding];

            bool active = (building.m_flags & Building.Flags.Active) != 0;

            if (active)
            {
                Log.Debug(LogCategory.Movement, $"Citizen {citizenID} is moving to building {targetBuilding} which is active, run the original function.");
                return true;
            }

            if (!RealTimeBuildingAI.IsBuildingOpeningSoon(targetBuilding, 1) || building.m_fireIntensity != 0 || (building.m_flags & Building.Flags.Evacuating) != 0)
            {
                Log.Debug(LogCategory.Movement, $"Citizen {citizenID} is moving to building {targetBuilding} which is not opening soon, on fire or evacuating, run the original function.");
                return true;
            }

            Log.Debug(LogCategory.Movement, $"Citizen {citizenID} is moving to building {targetBuilding} which is not active but opening soon, try to create a citizen instance and set the source and target buildings.");
            var instance = Singleton<CitizenManager>.instance;
            if (instance.CreateCitizenInstance(out ushort instance2, ref Singleton<SimulationManager>.instance.m_randomizer, __instance.m_info, citizenID))
            {
                __instance.m_info.m_citizenAI.SetSource(instance2, ref instance.m_instances.m_buffer[instance2], sourceBuilding);
                __instance.m_info.m_citizenAI.SetTarget(instance2, ref instance.m_instances.m_buffer[instance2], targetBuilding);
                data.CurrentLocation = Citizen.Location.Moving;
                __result = true;
                Log.Debug(LogCategory.Movement, $"Citizen {citizenID} - instance {instance2} created successfully and source and target buildings set, returning true.");
                return false;
            }

            Log.Debug(LogCategory.Movement, $"Citizen {citizenID} - failed to create citizen instance, returning false.");
            __result = false;
            return false;
        }


        [HarmonyPatch(typeof(HumanAI), "StartMoving",
            [typeof(uint), typeof(Citizen), typeof(ushort), typeof(ushort)],
            [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal])]
        [HarmonyPostfix]
        private static void Postfix(HumanAI __instance, uint citizenID, ref Citizen data, ushort targetBuilding, bool __result)
        {
            if (__result && __instance is ResidentAI && citizenID != 0 && RealTimeResidentAI != null)
            {
                RealTimeResidentAI.RegisterCitizenDeparture(citizenID, ref data, targetBuilding);
            }
        }

        [HarmonyPatch(typeof(HumanAI), "ArriveAtDestination")]
        [HarmonyPostfix]
        private static void Postfix(HumanAI __instance, ushort instanceID, ref CitizenInstance citizenData, bool success)
        {
            if (success && citizenData.m_citizen != 0 && RealTimeResidentAI != null && __instance is ResidentAI)
            {
                ref var citizen = ref Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizenData.m_citizen];
                RealTimeResidentAI.RegisterCitizenArrival(citizenData.m_citizen, ref citizen);
            }
        }
    }
}
