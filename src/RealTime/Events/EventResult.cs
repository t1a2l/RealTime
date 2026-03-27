namespace RealTime.Events
{
    using System;

    internal sealed class EventResult
    {
        public string EventName { get; set; }
        public string BuildingName { get; set; }
        public int AttendeesCount { get; set; }
        public int Capacity { get; set; }
        public float TotalIncome { get; set; }
        public float TotalCost { get; set; }
        public float NetProfit => TotalIncome - TotalCost;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
