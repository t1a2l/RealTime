// EventRouteTimeManager.cs

namespace RealTime.Managers
{
    using System.Collections.Generic;

    public static class EventRouteTimeManager
    {
        public static Dictionary<ushort, EventTimeSchedule[]> TimeSchedules;

        public struct EventTimeSchedule
        {
            public byte StartHour;
            public byte StartMinute;
            public byte Frequency;
            public bool AutoOccur;
        }

        public static void Init() => TimeSchedules ??= [];

        public static void Deinit() => TimeSchedules = [];

        public static bool EventTimeScheduleExist(ushort eventRouteID) => TimeSchedules.ContainsKey(eventRouteID);

        public static EventTimeSchedule[] GetEventTimeSchedules(ushort eventRouteID)
        {
            if (!TimeSchedules.TryGetValue(eventRouteID, out var _))
            {
                var eventTimeSchedule = new EventTimeSchedule[6];
                TimeSchedules.Add(eventRouteID, eventTimeSchedule);
                return eventTimeSchedule;
            }
            return TimeSchedules[eventRouteID];
        }

        public static void SetEventTimeScheduleHour(ushort eventRouteID, int scheduleIndex, byte startHour)
        {
            var eventTimeSchedule = GetEventTimeSchedules(eventRouteID);
            eventTimeSchedule[scheduleIndex].StartHour = startHour;
            SetEventTimeSchedule(eventRouteID, eventTimeSchedule);
        }

        public static void SetEventTimeScheduleMinute(ushort eventRouteID, int scheduleIndex, byte startMinute)
        {
            var eventTimeSchedule = GetEventTimeSchedules(eventRouteID);
            eventTimeSchedule[scheduleIndex].StartMinute = startMinute;
            SetEventTimeSchedule(eventRouteID, eventTimeSchedule);
        }

        public static void SetEventTimeScheduleFrequency(ushort eventRouteID, int scheduleIndex, byte frequency)
        {
            var eventTimeSchedule = GetEventTimeSchedules(eventRouteID);
            eventTimeSchedule[scheduleIndex].Frequency = frequency;
            SetEventTimeSchedule(eventRouteID, eventTimeSchedule);
        }

        public static void SetEventTimeScheduleAutoOccur(ushort eventRouteID, int scheduleIndex, bool autoOccur)
        {
            var eventTimeSchedule = GetEventTimeSchedules(eventRouteID);
            eventTimeSchedule[scheduleIndex].AutoOccur = autoOccur;
            SetEventTimeSchedule(eventRouteID, eventTimeSchedule);
        }

        public static void SetEventTimeSchedule(ushort eventRouteID, EventTimeSchedule[] eventTimeSchedule) => TimeSchedules[eventRouteID] = eventTimeSchedule;

        public static void RemoveEventTimeSchedule(ushort eventRouteID) => TimeSchedules.Remove(eventRouteID);
    }

}
