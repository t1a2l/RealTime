// EventManagerPatch.cs

namespace RealTime.Patches
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using ColossalFramework;
    using ColossalFramework.Threading;
    using ColossalFramework.UI;
    using HarmonyLib;
    using RealTime.Core;
    using UnityEngine;

    [HarmonyPatch]
    internal static class RaceEventWorldInfoPanelPatch
    {
        [HarmonyPatch(typeof(RaceEventWorldInfoPanel), "Start")]
        [HarmonyPostfix]
        public static void Start(RaceEventWorldInfoPanel __instance, ref UITemplateList<UIPanel> ___m_EventConfigs)
        {
            for (int i = 0; i < 4; i++)
            {
                // Match the logic you already have
                var panel = ___m_EventConfigs.items[i];
                var sliderLapCount = panel.Find<UISlider>("SliderLapCount");

                if (sliderLapCount != null)
                {
                    sliderLapCount.minValue = 1f;
                    sliderLapCount.maxValue = 25f;
                    sliderLapCount.stepSize = 1f;
                }
            }   
        }

        [HarmonyPatch(typeof(RaceEventWorldInfoPanel), "CreateNewEvent")]
        [HarmonyPrefix]
        public static bool CreateNewEvent(RaceEventWorldInfoPanel __instance, bool alwaysCreateNew, ref ushort ___m_eventRouteID, ref List<EventInfo> ___m_allowedEventInfos, ref UITabstrip ___m_tabstrip)
        {
            ushort m_eventRouteID = ___m_eventRouteID;
            var m_allowedEventInfos = ___m_allowedEventInfos;
            var m_tabstrip = ___m_tabstrip;
            Singleton<SimulationManager>.instance.AddAction(delegate
            {
                if (m_eventRouteID != 0)
                {
                    var buffer = Singleton<EventManager>.instance.m_eventRoutes.m_buffer;
                    int scheduleCount = buffer[m_eventRouteID].m_scheduleCount;
                    if (scheduleCount == 0 || alwaysCreateNew)
                    {
                        if (scheduleCount >= buffer[m_eventRouteID].m_scheduleData.Length)
                        {
                            return;
                        }
                        var dateTime = Singleton<SimulationManager>.instance.m_currentGameTime;
                        buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_scheduleID = ++Singleton<EventManager>.instance.m_eventScheduleCount;
                        buffer[m_eventRouteID].m_scheduleData[scheduleCount].Info = m_allowedEventInfos[0];
                        buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_startDay = (byte)(dateTime.Day - 1);
                        buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_startMonth = (byte)(dateTime.Month - 1);
                        buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_laps = 1;
                        buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_ticketPrice = 100;
                        buffer[m_eventRouteID].m_scheduleData[scheduleCount].m_flags = EventRouteData.EventScheduleFlags.Suspended;
                        buffer[m_eventRouteID].m_scheduleCount++;
                        if (RealTimeMod.configProvider.Configuration.DisableRaceOrParadeAutoOccur)
                        {
                            buffer[m_eventRouteID].m_scheduleCount = 1;
                        }
                        Singleton<EventManager>.instance.ScheduleEventRoute(m_eventRouteID);
                    }
                    ThreadHelper.dispatcher.Dispatch(delegate
                    {
                        m_tabstrip.selectedIndex = 1;
                        UpdateEventScheduleForm(__instance);
                    });
                }
            });
            return false;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(RaceEventWorldInfoPanel), "UpdateEventScheduleForm")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UpdateEventScheduleForm(object instance)
        {
            string message = "UpdateEventScheduleForm reverse Harmony patch wasn't applied";
            Debug.LogError(message);
            throw new NotImplementedException(message);
        }
    }
}
