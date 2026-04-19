namespace RealTime.Patches
{
    using System;
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.Managers;
    using SkyTools.Tools;
    using UnityEngine;

    [HarmonyPatch]
    internal static class RaceEventAIPatch
    {
        [HarmonyPatch(typeof(RaceEventAI), "EndEvent")]
        [HarmonyPostfix]
        public static void EndEvent(ushort eventID, ref EventData data)
        {
            if (data.m_raceEventData?.m_scheduleID == 0)
            {
                return;  // not from a schedule, skip
            }

            ushort eventRouteIndex = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_building].m_eventRouteIndex;
            if (eventRouteIndex == 0)
            {
                return;
            }

            int scheduleId = data.m_raceEventData.m_scheduleID;
            ref var route = ref Singleton<EventManager>.instance.m_eventRoutes.m_buffer[eventRouteIndex];
            var eventTimeSchedules = EventRouteTimeManager.GetEventTimeSchedules(eventRouteIndex);
            for (int i = 0; i < route.m_scheduleCount; i++)
            {
                if (route.m_scheduleData[i].m_scheduleID == scheduleId && !eventTimeSchedules[i].AutoOccur)
                {
                    Singleton<EventManager>.instance.SuspendScheduledEvent(eventRouteIndex, i);
                    Log.Debug(LogCategory.Events, $"RealTime: Suspended non-auto schedule {i} (ID={scheduleId}) for route {eventRouteIndex}");
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(RaceEventAI), "GetDisorganizingEndFrame")]
        [HarmonyPrefix]
        public static bool GetDisorganizingEndFrame(RaceEventAI __instance, ushort eventID, ref EventData data, ref uint __result)
        {
            uint durationFrames = __instance is ParadeAI paradeAI ? GetParadeDurationFrames(paradeAI, ref data) : GetRaceDurationFrames(ref data);

            uint eventEndFrame = data.m_startFrame + durationFrames;
            uint disorganizeFrames = (uint)Mathf.RoundToInt(1f * SimulationManager.DAYTIME_HOUR_TO_FRAME);
            __result = eventEndFrame + disorganizeFrames;
            return false;
        }

        [HarmonyPatch(typeof(RaceEventAI), "GetEndFrame")]
        [HarmonyPrefix]
        public static bool GetEndFrame(RaceEventAI __instance, ushort eventID, ref EventData data, ref uint __result)
        {
            uint durationFrames = __instance is ParadeAI paradeAI ? GetParadeDurationFrames(paradeAI, ref data) : GetRaceDurationFrames(ref data);

            __result = data.m_startFrame + durationFrames;
            return false;
        }

        public static uint GetRaceDurationFrames(ref EventData data)
        {
            float totalDistance = 0f;
            for (int i = 1; i < data.m_raceEventData.m_trackPathLength; i++)
            {
                totalDistance += Vector3.Distance(data.m_raceEventData.m_trackPath[i - 1], data.m_raceEventData.m_trackPath[i]);
            }

            int lapCount = Mathf.Max(1, data.m_raceEventData.m_lapCount);
            float totalRaceUnits = totalDistance * lapCount;

            int racerCount = Mathf.Max(0, data.m_raceEventData.m_racerCount);

            float representativeSpeed = 0f;
            int validSpeedCount = 0;

            for (int i = 0; i < racerCount; i++)
            {
                float speed = data.m_raceEventData.m_racerData[i].m_maxSpeed;
                if (speed > 0.01f)
                {
                    representativeSpeed += speed;
                    validSpeedCount++;
                }
            }

            if (validSpeedCount > 0)
            {
                representativeSpeed /= validSpeedCount;
            }
            else
            {
                representativeSpeed = data.m_raceEventData.m_tier switch
                {
                    1 => 0.8f,
                    2 => 1.2f,
                    3 => 2.0f,
                    _ => 1.5f,
                };
            }

            representativeSpeed = Mathf.Max(representativeSpeed, 0.05f);

            float travelFrames = totalRaceUnits / representativeSpeed;
            float startBufferFrames = 0.10f * SimulationManager.DAYTIME_HOUR_TO_FRAME;
            float finishBufferFrames = 0.25f * SimulationManager.DAYTIME_HOUR_TO_FRAME;

            uint totalDuration = (uint)Mathf.CeilToInt(travelFrames + startBufferFrames + finishBufferFrames);

            uint minFrames = (uint)Mathf.RoundToInt(0.5f * SimulationManager.DAYTIME_HOUR_TO_FRAME);
            uint maxFrames = (uint)Mathf.RoundToInt(6f * SimulationManager.DAYTIME_HOUR_TO_FRAME);

            return (uint)Mathf.Clamp(totalDuration, minFrames, maxFrames);
        }

        public static uint GetParadeDurationFrames(ParadeAI instance, ref EventData data)
        {
            float totalDistanceUnits = 0f;
            for (int i = 1; i < data.m_raceEventData.m_trackPathLength; i++)
            {
                totalDistanceUnits += Vector3.Distance(data.m_raceEventData.m_trackPath[i - 1], data.m_raceEventData.m_trackPath[i]);
            }

            float speed = Mathf.Max(instance.m_paradeSpeed, 0.01f);
            int racerCount = Mathf.Max(1, data.m_raceEventData.m_racerCount);

            float leadTravelFrames = totalDistanceUnits / speed;
            float avgFloatLength = 1.5f;
            float spawnIntervalFrames = (instance.m_groupSpacing + avgFloatLength) / speed;
            float tailSpawnFrames = (racerCount - 1) * spawnIntervalFrames;

            float finishBufferFrames = Mathf.Max(
                0.25f * SimulationManager.DAYTIME_HOUR_TO_FRAME,
                2f * spawnIntervalFrames);

            uint totalDuration = (uint)Mathf.CeilToInt(
                leadTravelFrames + tailSpawnFrames + finishBufferFrames);

            uint maxFrames = (uint)Mathf.RoundToInt(4f * SimulationManager.DAYTIME_HOUR_TO_FRAME);

            return Math.Min(totalDuration, maxFrames);
        }
    }
}
