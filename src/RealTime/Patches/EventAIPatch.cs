// EventAIPatch.cs

namespace RealTime.Patches
{
    using System.Reflection;
    using HarmonyLib;
    using RealTime.Config;
    using RealTime.CustomAI;

    [HarmonyPatch]
    internal static class EventAIPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        /// <summary>Gets or sets the mod configuration.</summary>
        public static RealTimeConfig RealTimeConfig { get; set; }

        private delegate void CancelDelegate(EventAI __instance, ushort eventID, ref EventData data);
        private static readonly CancelDelegate Cancel = AccessTools.MethodDelegate<CancelDelegate>(typeof(EventAI).GetMethod("Cancel", BindingFlags.Instance | BindingFlags.NonPublic), null, true);

        [HarmonyPatch(typeof(EventAI), "BuildingDeactivated")]
        [HarmonyPrefix]
        public static bool BuildingDeactivated(EventAI __instance, ushort eventID, ref EventData data)
        {
            if ((data.m_flags & (EventData.Flags.Completed | EventData.Flags.Cancelled)) == 0 && __instance.m_info.m_type != EventManager.EventType.AcademicYear && RealTimeBuildingAI != null && !RealTimeBuildingAI.IsEventWithinOperationHours(ref data))
            {
                Cancel(__instance, eventID, ref data);
            }
            return false;
        }

        [HarmonyPatch(typeof(EventAI), "GetPrepareStartFrame")]
        [HarmonyPrefix]
        public static void GetPrepareStartFrame(EventAI __instance, ushort eventID, ref EventData data)
        {
            if (__instance.m_info.GetAI() is AcademicYearAI)
            {
                __instance.m_prepareDuration = 0;
            }
            if (__instance.m_info.GetAI() is RaceEventAI)
            {
                __instance.m_prepareDuration = 2f;
            }
        }

        [HarmonyPatch(typeof(EventAI), "CalculateExpireFrame")]
        [HarmonyPrefix]
        public static void CalculateExpireFrame(EventAI __instance, uint startFrame)
        {
            if (__instance.m_info.GetAI() is AcademicYearAI)
            {
                __instance.m_eventDuration = RealTimeConfig.AcademicYearLength * 24f;
                __instance.m_disorganizeDuration = 0;
            }
        }

        [HarmonyPatch(typeof(EventAI), "GetDisorganizingEndFrame")]
        [HarmonyPrefix]
        public static void GetDisorganizingEndFrame(EventAI __instance, ushort eventID, ref EventData data)
        {
            if (__instance.m_info.GetAI() is AcademicYearAI)
            {
                __instance.m_disorganizeDuration = 0;
            }
        }

        [HarmonyPatch(typeof(EventAI), "GetEndFrame")]
        [HarmonyPrefix]
        public static void GetEndFrame(EventAI __instance, ushort eventID, ref EventData data)
        {
            if (__instance.m_info.GetAI() is AcademicYearAI)
            {
                __instance.m_eventDuration = RealTimeConfig.AcademicYearLength * 24f;
            }
        }
    }
}
