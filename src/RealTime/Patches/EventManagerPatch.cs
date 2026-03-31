// EventManagerPatch.cs

namespace RealTime.Patches
{
    using System;
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.CustomAI;
    using RealTime.GameConnection;
    using RealTime.Managers;
    using UnityEngine;

    [HarmonyPatch]
    internal static class EventManagerPatch
    {
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        public static TimeInfo TimeInfo { get; set; }

        [HarmonyPatch(typeof(EventManager), "CreateEvent")]
        [HarmonyPrefix]
        public static bool CreateEvent(EventManager __instance, out ushort eventIndex, ushort building, EventInfo info, ref bool __result)
        {
            if(info.GetAI() is AcademicYearAI)
            {
                var buildingManager = Singleton<BuildingManager>.instance;
                var eventData = default(EventData);
                eventData.m_flags = EventData.Flags.Created;
                eventData.m_startFrame = Singleton<SimulationManager>.instance.m_currentFrameIndex;
                eventData.m_ticketPrice = (ushort)info.m_eventAI.m_ticketPrice;
                eventData.m_securityBudget = (ushort)info.m_eventAI.m_securityBudget;
                if (building != 0)
                {
                    eventData.m_building = building;
                    eventData.m_nextBuildingEvent = buildingManager.m_buildings.m_buffer[building].m_eventIndex;
                }
                eventData.Info = info;
                if (__instance.m_events.m_size < 256)
                {
                    eventIndex = (ushort)__instance.m_events.m_size;

                    bool can_start_new_year = false;
                    if (eventIndex != 0)
                    {
                        if (!RealTimeBuildingAI.IsBuildingWorking(building))
                        {
                            __result = false;
                            return false;
                        }
                        if (!AcademicYearManager.CanAcademicYearEndorBegin(TimeInfo))
                        {
                            __result = false;
                            return false;
                        }

                        var academicYearData = AcademicYearManager.GetAcademicYearData(building);

                        if(academicYearData.IsFirstAcademicYear)
                        {
                            can_start_new_year = true;
                            academicYearData.IsFirstAcademicYear = false;
                            AcademicYearManager.SetAcademicYearData(building, academicYearData);
                        }
                        else
                        {
                            float hours_since_last_year_ended = AcademicYearManager.CalculateHoursSinceLastYearEnded(building);

                            if (hours_since_last_year_ended >= 24f)
                            {
                                can_start_new_year = true;
                                academicYearData.DidLastYearEnd = false;
                            }
                            else
                            {
                                academicYearData.DidLastYearEnd = true;
                                academicYearData.IsFirstAcademicYear = false;
                            }
                            AcademicYearManager.SetAcademicYearData(building, academicYearData);
                        }
                    }
                    else
                    {
                        can_start_new_year = true;
                    }
                    
                    if (can_start_new_year)
                    {
                        __instance.m_events.Add(eventData);
                        __instance.m_eventCount++;
                        info.m_eventAI.CreateEvent(eventIndex, ref __instance.m_events.m_buffer[eventIndex]);
                        buildingManager.m_buildings.m_buffer[building].m_eventIndex = eventIndex;
                        __result = true;
                        return false;
                    }
                }
                eventIndex = 0;
                __result = false;
                return false;
            }
            eventIndex = 0;
            return true;
        }


        [HarmonyPatch(typeof(EventManager), "PopulateRouteSchedule")]
        [HarmonyPrefix]
        public static bool PopulateRouteSchedule(RaceEventWorldInfoPanel __instance, ushort eventRouteIndex, DateTime startDate, EventRouteData.EventRouteSchedule[] schedule, ref FastList<EventRouteData> ___m_eventRoutes, ref bool __result)
        {
            if ((___m_eventRoutes.m_buffer[eventRouteIndex].m_flags & EventRouteData.Flags.Created) == 0)
            {
                __result = false;
                return false;
            }
            if (___m_eventRoutes.m_buffer[eventRouteIndex].m_scheduleCount == 0)
            {
                __result = false;
                return false;
            }
            bool flag = false;
            int num = schedule.Length;
            var eventTimeSchedules = EventRouteTimeManager.GetEventTimeSchedules(eventRouteIndex);
            var scheduleData = ___m_eventRoutes.m_buffer[eventRouteIndex].m_scheduleData;
            for (int i = 0; i < ___m_eventRoutes.m_buffer[eventRouteIndex].m_scheduleCount; i++)
            {
                if ((scheduleData[i].m_flags & EventRouteData.EventScheduleFlags.Suspended) != 0)
                {
                    continue;
                }
                var dateTime = CalculateNextEvent(Singleton<SimulationManager>.instance.m_currentGameTime, scheduleData[i].m_startDay + 1, scheduleData[i].m_startMonth + 1, eventTimeSchedules[i].StartHour, eventTimeSchedules[i].StartMinute);
                int j = 0;
                if (!eventTimeSchedules[i].AutoOccur)
                {
                    num = 1;
                }
                for (int k = 0; k < num; k++)
                {
                    if (j >= num)
                    {
                        break;
                    }
                    if (!flag)
                    {
                        ref var reference = ref schedule[k];
                        reference = new EventRouteData.EventRouteSchedule
                        {
                            m_startDate = dateTime,
                            m_scheduleIndex = i
                        };
                    }
                    else
                    {
                        for (; j < num && schedule[j].m_startDate < dateTime; j++)
                        {
                        }
                        if (j < num)
                        {
                            for (int num2 = num - 1; num2 > j; num2--)
                            {
                                ref var reference2 = ref schedule[num2];
                                reference2 = schedule[num2 - 1];
                            }
                            ref var reference3 = ref schedule[j];
                            reference3 = new EventRouteData.EventRouteSchedule
                            {
                                m_startDate = dateTime,
                                m_scheduleIndex = i
                            };
                        }
                    }
                    if (eventTimeSchedules[i].Frequency == 0)
                    {
                        // daily
                        dateTime = dateTime.AddDays(7);
                    }
                    else if (eventTimeSchedules[i].Frequency == 1)
                    {
                        // weekly default
                        dateTime = dateTime.AddDays(1);
                    }
                }
                flag = true;
            }
            __result = flag;
            return false;
        }

        private static DateTime CalculateNextEvent(DateTime currentDate, int scheduleDay, int scheduleMonth, int scheduleHour, int scheduleMinute)
        {
            var dateTime = new DateTime(currentDate.Year, scheduleMonth, scheduleDay, scheduleHour, scheduleMinute, 0);
            return dateTime >= currentDate ? dateTime : dateTime.AddYears(1);

        }

    }
}
