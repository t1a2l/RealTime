namespace RealTime.Patches
{
    using System;
    using HarmonyLib;
    using UnityEngine;

    [HarmonyPatch]
    internal static class RaceEventAIPatch
    {
        [HarmonyPatch(typeof(RaceEventAI), "CalculateExpireFrame")]
        [HarmonyPrefix]
        public static bool CalculateExpireFrame(RaceEventAI __instance, uint startFrame, ref uint __result)
        {
            int maxHours = __instance is ParadeAI ? 5 : 7; // 4/6 event cap + 1h disorganize
            __result = startFrame + (uint)Mathf.RoundToInt(maxHours * SimulationManager.DAYTIME_HOUR_TO_FRAME);
            return false;
        }

        [HarmonyPatch(typeof(RaceEventAI), "GetDisorganizingEndFrame")]
        [HarmonyPrefix]
        public static bool GetDisorganizingEndFrame(RaceEventAI __instance, ushort eventID, ref EventData data, ref uint __result)
        {
            uint eventEndFrame = data.m_raceEventData.m_eventEndFrame;
            uint num = (uint)Mathf.RoundToInt(1f * SimulationManager.DAYTIME_HOUR_TO_FRAME);
            __result = eventEndFrame + num;
            return false;
        }

        [HarmonyPatch(typeof(RaceEventAI), "GetEndFrame")]
        [HarmonyPrefix]
        public static bool GetEndFrame(RaceEventAI __instance, ushort eventID, ref EventData data)
        {
            data.m_raceEventData.m_eventEndFrame = __instance is ParadeAI paradeAI ? GetParadeEndFrame(paradeAI, ref data) : GetRaceEndFrame(ref data);
            return false;
        }

        private static uint GetRaceEndFrame(ref EventData data)
        {
            float totalDistance = 0f;
            for (int i = 1; i < data.m_raceEventData.m_trackPathLength; i++)
            {
                totalDistance += Vector3.Distance(data.m_raceEventData.m_trackPath[i - 1], data.m_raceEventData.m_trackPath[i]);
            }

            float totalRaceMeters = totalDistance * data.m_raceEventData.m_lapCount;

            // Safety check for racer data
            float racerMaxSpeed = (data.m_raceEventData.m_racerCount > 0) ? data.m_raceEventData.m_racerData[0].m_maxSpeed : 2f; // Default fallback speed

            // Convert game speed (units/frame) to Meters per Hour
            // 1 unit = 8m. Simulation has ~585,937 frames per "Daytime" cycle in vanilla.
            float speedMetersPerHour = racerMaxSpeed * 8f * SimulationManager.DAYTIME_HOUR_TO_FRAME;

            float travelTimeHours = totalRaceMeters / speedMetersPerHour;

            // Convert to your RealTime frames
            uint durationFrames = (uint)Mathf.RoundToInt(travelTimeHours * SimulationManager.DAYTIME_HOUR_TO_FRAME);

            // REALTIME SCHEDULE PROTECTION:
            // Ensure the race doesn't exceed a reasonable window (e.g., 6 real hours)
            uint maxAllowedFrames = (uint)Mathf.RoundToInt(6f * SimulationManager.DAYTIME_HOUR_TO_FRAME);
            uint finalDuration = Math.Min(durationFrames, maxAllowedFrames);

            return data.m_startFrame + finalDuration;
        }

        private static uint GetParadeEndFrame(ParadeAI instance, ref EventData data)
        {
            float totalDistanceUnits = 0f;
            for (int i = 1; i < data.m_raceEventData.m_trackPathLength; i++)
            {
                totalDistanceUnits += Vector3.Distance(data.m_raceEventData.m_trackPath[i - 1], data.m_raceEventData.m_trackPath[i]);
            }

            // ParadeSpeed is already in units/frame, so we stay in Frames
            float framesForFirstFloat = totalDistanceUnits / instance.m_paradeSpeed;

            // Line time: (Spacing + Avg Float Length) / Speed * Count
            // Each float is roughly 1-2 units long. Let's use 1.5f as a safe average.
            float framesForTail = data.m_raceEventData.m_racerCount * ((instance.m_groupSpacing + 1.5f) / instance.m_paradeSpeed);

            uint totalDuration = (uint)Mathf.CeilToInt(framesForFirstFloat + framesForTail);

            // 4 Hour Cap
            uint maxFrames = (uint)Mathf.RoundToInt(4f * SimulationManager.DAYTIME_HOUR_TO_FRAME);

            return data.m_startFrame + Math.Min(totalDuration, maxFrames);
        }
    }
}
