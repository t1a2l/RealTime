// AcademicYearManager.cs

namespace RealTime.Managers
{
    using System.Collections.Generic;
    using RealTime.GameConnection;
    using SkyTools.Tools;

    internal static class AcademicYearManager
    {
        public static Dictionary<ushort, AcademicYearData> MainCampusBuildingsList;

        public struct AcademicYearData
        {
            public bool DidLastYearEnd;
            public bool DidGraduationStart;
            public float GraduationStartTime;
            public uint ActualAcademicYearEndFrame;
            public bool IsFirstAcademicYear;
        }

        public static void Init() => MainCampusBuildingsList = [];

        public static void Deinit() => MainCampusBuildingsList = [];

        public static bool MainCampusBuildingExist(ushort buildingID) => MainCampusBuildingsList.ContainsKey(buildingID);

        public static AcademicYearData GetAcademicYearData(ushort buildingID) => MainCampusBuildingsList[buildingID];

        public static void CreateAcademicYearData(ushort buildingID)
        {
            var academicYearData = new AcademicYearData()
            {
                DidLastYearEnd = false,
                DidGraduationStart = false,
                GraduationStartTime = 0,
                ActualAcademicYearEndFrame = 0,
                IsFirstAcademicYear = true
            };
            
            MainCampusBuildingsList.Add(buildingID, academicYearData);
        }

        public static void CreateAcademicYearDataExistingCampus(ushort buildingID)
        {
            var academicYearData = new AcademicYearData()
            {
                DidLastYearEnd = false,
                DidGraduationStart = false,
                GraduationStartTime = 0,
                ActualAcademicYearEndFrame = 0,
                IsFirstAcademicYear = false
            };

            MainCampusBuildingsList.Add(buildingID, academicYearData);
        }

        public static void SetAcademicYearData(ushort buildingID, AcademicYearData academicYearData) => MainCampusBuildingsList[buildingID] = academicYearData;

        public static void DeleteAcademicYearData(ushort buildingID) => MainCampusBuildingsList.Remove(buildingID);

        // calculate hours since last year ended
        public static float CalculateHoursSinceLastYearEnded(ushort buildingID)
        {
            var academicYearData = GetAcademicYearData(buildingID);
            uint currentFrame = SimulationManager.instance.m_currentFrameIndex;
            uint endFrame = academicYearData.ActualAcademicYearEndFrame;
            return currentFrame < endFrame ? 0f : (currentFrame - endFrame) * SimulationManager.DAYTIME_FRAME_TO_HOUR;
        }

        // dont start or end academic year if night time or weekend or the hour is not between 9 am and 10 am
        public static bool CanAcademicYearEndorBegin(TimeInfo TimeInfo)
        {
            if (TimeInfo.IsNightTime || TimeInfo.Now.IsWeekend())
            {
                return false;
            }
            if (TimeInfo.CurrentHour < 9f || TimeInfo.CurrentHour > 10f)
            {
                return false;
            }
            return true;
        }
    }
}
