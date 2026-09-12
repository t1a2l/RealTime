// HumanAIPatch.cs

namespace RealTime.Patches
{
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.CustomAI;

    /// <summary>
    /// A static class that provides the patch objects for the Human AI.
    /// </summary>
    [HarmonyPatch]
    internal static class HumanAIPatch
    {
        /// <summary>Gets or sets the custom AI object for resident citizens.</summary>
        public static RealTimeResidentAI<ResidentAI, Citizen> RealTimeResidentAI { get; set; }

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
