namespace RealTime.Events.Containers
{
    using System;

    internal class UpcomingEventItem
    {
        public string eventName;
        public string timeStr;
        public Action deleteAction;  // Called on X click
    }
}
