// HumanAIPatch.cs

namespace RealTime.Patches
{
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.CustomAI;
    using SkyTools.Tools;

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
            var citizenManager = Singleton<CitizenManager>.instance;
            if (targetBuilding == sourceBuilding)
            {
                Log.Debug(LogCategory.Movement, $"Citizen {citizenID} - targetBuilding and sourceBuilding are the same.");
                __result = false;
                return false;
            }

            if (targetBuilding == 0)
            {
                Log.Debug(LogCategory.Movement, $"Citizen {citizenID} targetBuilding is 0.");
                __result = false;
                return false;
            }

            ref var building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetBuilding];

            bool targetActive = (building.m_flags & Building.Flags.Active) != 0;

            bool emergency = building.m_fireIntensity != 0 || (building.m_flags & Building.Flags.Evacuating) != 0;

            bool openingSoon = RealTimeBuildingAI.IsBuildingOpeningSoon(targetBuilding, 1);

            if (!targetActive && (!openingSoon || emergency))
            {
                Log.Debug(LogCategory.Movement, $"Citizen {citizenID} targetBuilding is not active and not opening soon or has an emergency.");
                __result = false;
                return false;
            }

            if (data.m_instance != 0)
            {
                __instance.m_info.m_citizenAI.SetTarget(data.m_instance, ref citizenManager.m_instances.m_buffer[data.m_instance], targetBuilding);
                data.CurrentLocation = Citizen.Location.Moving;
                Log.Debug(LogCategory.Movement, $"Citizen {citizenID} already has an instance {data.m_instance}, set the target building and return true.");
                __result = true;
                return false;
            }

            if (sourceBuilding == 0)
            {
                sourceBuilding = data.GetBuildingByLocation();
                if (sourceBuilding == 0)
                {
                    Log.Debug(LogCategory.Movement, $"Citizen {citizenID} sourceBuilding is 0 and cannot find a building by location.");
                    __result = false;
                    return false;
                }
            }

            if (citizenManager.CreateCitizenInstance(out ushort instanceID, ref Singleton<SimulationManager>.instance.m_randomizer, __instance.m_info, citizenID))
            {
                __instance.m_info.m_citizenAI.SetSource(instanceID, ref citizenManager.m_instances.m_buffer[instanceID], sourceBuilding);
                __instance.m_info.m_citizenAI.SetTarget(instanceID, ref citizenManager.m_instances.m_buffer[instanceID], targetBuilding);
                data.CurrentLocation = Citizen.Location.Moving;
                Log.Debug(LogCategory.Movement, $"Citizen {citizenID} created a new instance {instanceID}, set the source and target buildings and starts moving.");
                __result = true;
                return false;
            }

            Log.Debug(LogCategory.Movement, $"Citizen {citizenID} failed to create a new instance.");
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
