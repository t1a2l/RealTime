namespace RealTime.CustomAI
{
    using System.Collections.Generic;
    using RealTime.Config;
    using RealTime.GameConnection;
    using RealTime.Simulation;

    internal static class BuildingWorkTimeManager
    {
        public static Dictionary<ushort, WorkTime> BuildingsWorkTime;

        public static IRandomizer Random { get; set; }

        public static RealTimeConfig Config { get; set; }

        public static IBuildingManagerConnection BuildingManager;

        public struct WorkTime
        {
            public bool WorkAtNight;
            public bool WorkAtWeekands;
            public bool HasExtendedWorkShift;
            public bool HasContinuousWorkShift;
            public int WorkShifts;
        }

        public static void Init()
        {
            if (BuildingsWorkTime == null)
            {
                BuildingsWorkTime = new Dictionary<ushort, WorkTime>();
            }
        }

        public static void Deinit() => BuildingsWorkTime = new Dictionary<ushort, WorkTime>();

        internal static WorkTime GetBuildingWorkTime(ushort buildingID) => !BuildingsWorkTime.TryGetValue(buildingID, out var workTime) ? default : workTime;

        internal static void CreateBuildingWorkTime(ushort buildingID)
        {
            if (!BuildingsWorkTime.TryGetValue(buildingID, out _))
            {
                bool OpenAtNight = Random.ShouldOccur(Config.OpenCommercialAtNightQuota);
                if (BuildingManager.GetBuildingHeight(buildingID) > Config.SwitchOffLightsMaxHeight)
                {
                    OpenAtNight = true;
                }
                bool OpenAtWeekends = Random.ShouldOccur(Config.OpenCommercialAtWeekendsQuota);
                bool HasExtendedWorkShift = Random.ShouldOccur(50);
                bool HasContinuousWorkShift = Random.ShouldOccur(50);

                if (HasExtendedWorkShift)
                {
                    HasContinuousWorkShift = false;
                }

                int WorkShifts = 2;

                if (HasContinuousWorkShift && !OpenAtNight)
                {
                    WorkShifts = 1;
                }

                if (OpenAtNight)
                {
                    WorkShifts = HasContinuousWorkShift ? 2 : 3;
                }

                var workTime = new WorkTime()
                {
                    WorkAtNight = OpenAtNight,
                    WorkAtWeekands = OpenAtWeekends,
                    HasExtendedWorkShift = HasExtendedWorkShift,
                    HasContinuousWorkShift = HasContinuousWorkShift,
                    WorkShifts = WorkShifts
                };
                BuildingsWorkTime.Add(buildingID, workTime);
            }
        }

        public static void SetBuildingWorkTime(ushort buildingID, WorkTime workTime) => BuildingsWorkTime[buildingID] = workTime;


        public static void RemoveBuildingWorkTime(ushort buildingID) => BuildingsWorkTime.Remove(buildingID);
    }

}