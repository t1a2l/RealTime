// HumanAIPatch.cs

namespace RealTime.Patches
{
    using HarmonyLib;
    using RealTime.CustomAI;
    using SkyTools.Tools;

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
        private static void Postfix(HumanAI __instance, uint citizenID, bool __result)
        {
            if (__result && __instance is ResidentAI && citizenID != 0 && RealTimeResidentAI != null)
            {
                RealTimeResidentAI.RegisterCitizenDeparture(citizenID);
            }
        }

        [HarmonyPatch(typeof(HumanAI), "ArriveAtTarget")]
        [HarmonyPostfix]
        private static void Postfix(HumanAI __instance, ushort instanceID, ref CitizenInstance citizenData)
        {
            if (citizenData.m_citizen != 0 && RealTimeResidentAI != null && __instance is ResidentAI)
            {
                RealTimeResidentAI.RegisterCitizenArrival(citizenData.m_citizen);
            }
        }
    }
}
