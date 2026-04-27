// EventManagerPatch.cs

namespace RealTime.Patches
{
    using System;
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.CustomAI;
    using RealTime.GameConnection;
    using RealTime.Managers;
    using RealTime.Simulation;
    using SkyTools.Tools;

    [HarmonyPatch]
    internal static class EventManagerPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        /// <summary>Gets or sets the time adjustment simulation class instance.</summary>
        public static TimeAdjustment TimeAdjustment { get; set; }

        /// <summary>Gets or sets the timeInfo.</summary>
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
        public static bool PopulateRouteSchedule(EventManager __instance, ushort eventRouteIndex, DateTime startDate, EventRouteData.EventRouteSchedule[] schedule, ref FastList<EventRouteData> ___m_eventRoutes, ref bool __result)
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
            Array.Clear(schedule, 0, schedule.Length);

            int count = 0;
            int capacity = schedule.Length;
            var eventTimeSchedules = EventRouteTimeManager.GetEventTimeSchedules(eventRouteIndex);
            var scheduleData = ___m_eventRoutes.m_buffer[eventRouteIndex].m_scheduleData;
            for (int i = 0; i < ___m_eventRoutes.m_buffer[eventRouteIndex].m_scheduleCount; i++)
            {
                if ((scheduleData[i].m_flags & EventRouteData.EventScheduleFlags.Suspended) != 0)
                {
                    continue;
                }

                if (TimeInfo != null && TimeInfo.Now != null)
                {
                    Log.Debug(LogCategory.Events, TimeInfo.Now, $"Processing schedule {i} for event route {eventRouteIndex}, startDay: {scheduleData[i].m_startDay}, startMonth: {scheduleData[i].m_startMonth}, startHour: {eventTimeSchedules[i].StartHour}, startMinute: {eventTimeSchedules[i].StartMinute}");
                }
                
                var dateTime = CalculateNextEvent(Singleton<SimulationManager>.instance.m_currentGameTime, scheduleData[i].m_startDay + 1, scheduleData[i].m_startMonth + 1, eventTimeSchedules[i].StartHour, eventTimeSchedules[i].StartMinute);

                if (TimeInfo != null && TimeInfo.Now != null)
                {
                    Log.Debug(LogCategory.Events, TimeInfo.Now, $"Initial calculated dateTime for schedule {i}: {dateTime:dd/MM/yyyy HH:mm}");
                }

                dateTime = WorldInfoPanelPatch.AdjustEventStartTime(dateTime);

                if (TimeInfo != null && TimeInfo.Now != null)
                {
                    Log.Debug(LogCategory.Events, TimeInfo.Now, $"Adjusted dateTime for schedule {i}: {dateTime:dd/MM/yyyy HH:mm}");
                }

                int occurrences;
                if (eventTimeSchedules[i].AutoOccur)
                {
                    occurrences = capacity;
                }
                else
                {
                    occurrences = HasExistingOccurrence(eventRouteIndex, i, ref ___m_eventRoutes) ? 0 : 1;
                }

                for (int k = 0; k < occurrences; k++)
                {
                    while (HasConflictWithCurrentEvent(eventRouteIndex, dateTime) || HasConflictWithGeneratedSchedule(schedule, count, dateTime))
                    {
                        dateTime = eventTimeSchedules[i].Frequency == 0 ? dateTime.AddDays(7) : dateTime.AddDays(1);
                        dateTime = WorldInfoPanelPatch.AdjustEventStartTime(dateTime);
                    }

                    int insertPos = 0;
                    while (insertPos < count && schedule[insertPos].m_startDate < dateTime)
                    {
                        insertPos++;
                    }

                    if (count < capacity)
                    {
                        for (int shift = count; shift > insertPos; shift--)
                        {
                            schedule[shift] = schedule[shift - 1];
                        }

                        schedule[insertPos] = new EventRouteData.EventRouteSchedule
                        {
                            m_startDate = dateTime,
                            m_scheduleIndex = i
                        };

                        count++;
                    }
                    else if (insertPos < capacity)
                    {
                        for (int shift = capacity - 1; shift > insertPos; shift--)
                        {
                            schedule[shift] = schedule[shift - 1];
                        }

                        schedule[insertPos] = new EventRouteData.EventRouteSchedule
                        {
                            m_startDate = dateTime,
                            m_scheduleIndex = i
                        };
                    }

                    if (eventTimeSchedules[i].Frequency == 0)
                    {
                        // weekly default
                        dateTime = dateTime.AddDays(7);
                    }
                    else if (eventTimeSchedules[i].Frequency == 1)
                    {
                        // daily
                        dateTime = dateTime.AddDays(1);
                    }
                }
            }
            __result = count > 0;
            return false;
        }

        private static DateTime CalculateNextEvent(DateTime currentDate, int scheduleDay, int scheduleMonth, int scheduleHour, int scheduleMinute)
        {
            var dateTime = new DateTime(currentDate.Year, scheduleMonth, scheduleDay, scheduleHour, scheduleMinute, 0);
            return dateTime >= currentDate ? dateTime : dateTime.AddYears(1);
        }

        private static bool HasConflictWithCurrentEvent(ushort eventRouteIndex, DateTime candidate)
        {
            if (Singleton<EventManager>.instance == null || eventRouteIndex >= Singleton<EventManager>.instance.m_eventRoutes.m_size)
            {
                return false;
            }


            ref var route = ref Singleton<EventManager>.instance.m_eventRoutes.m_buffer[eventRouteIndex];
            ushort currentEventId = route.m_event;

            if (currentEventId == 0 || currentEventId >= Singleton<EventManager>.instance.m_events.m_size)
            {
                return false;
            }

            ref var ev = ref Singleton<EventManager>.instance.m_events.m_buffer[currentEventId];
            if (ev.Info?.m_eventAI == null || ev.m_startFrame == 0)
            {
                return false;
            }

            DateTime currentStart;
            try
            {
                currentStart = TimeAdjustment.GetOriginalTime(ev.m_startFrame);
            }
            catch
            {
                return false;
            }

            var candidateEnd = candidate.AddHours(7);
            var currentEnd = currentStart.AddHours(7);

            return candidate < currentEnd && candidateEnd > currentStart;
        }

        private static bool HasConflictWithGeneratedSchedule(EventRouteData.EventRouteSchedule[] schedule, int count, DateTime candidate)
        {
            var candidateEnd = candidate.AddHours(7);

            for (int i = 0; i < count; i++)
            {
                var existingStart = schedule[i].m_startDate;
                var existingEnd = existingStart.AddHours(7);

                if (candidate < existingEnd && candidateEnd > existingStart)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasExistingOccurrence(ushort eventRouteIndex, int scheduleIndex, ref FastList<EventRouteData> m_eventRoutes)
        {
            var events = Singleton<EventManager>.instance.m_events;
            ref var route = ref m_eventRoutes.m_buffer[eventRouteIndex];
            ref var scheduleData = ref route.m_scheduleData[scheduleIndex];

            for (ushort i = 1; i < events.m_size; i++)
            {
                ref var ev = ref events.m_buffer[i];

                // Skip non-live events
                if ((ev.m_flags & (EventData.Flags.Preparing | EventData.Flags.Ready | EventData.Flags.Active)) == 0)
                {
                    continue;
                }

                if ((ev.m_flags & (EventData.Flags.Cancelled | EventData.Flags.Completed | EventData.Flags.Deleted | EventData.Flags.Expired)) != 0)
                {
                    continue;
                }

                // Skip academic year
                if (ev.Info?.m_type == EventManager.EventType.AcademicYear)
                {
                    continue;
                }

                // Route building match
                if (ev.m_building != route.m_startBuilding)
                {
                    continue;
                }

                // Race/parade events only
                if (ev.m_raceEventData == null)
                {
                    continue;
                }

                // Exact schedule ID match
                if (ev.m_raceEventData.m_scheduleID == scheduleData.m_scheduleID)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
