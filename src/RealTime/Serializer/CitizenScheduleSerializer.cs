// CitizenScheduleSerializer.cs

namespace RealTime.Serializer
{
    using System;
    using System.IO;
    using ColossalFramework;
    using RealTime.CustomAI;
    using UnityEngine;

    public class CitizenScheduleSerializer
    {
        private const ushort iCITIZEN_SCHEDULE_DATA_VERSION = 1;
        
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        internal static CitizenSchedule[] residentSchedules;
        internal static Citizen[] citizens;

        /// <summary>Gets or sets the custom AI object for resident citizens.</summary>
        internal static RealTimeResidentAI<ResidentAI, Citizen> RealTimeResidentAI { get; set; }

        public static void SaveData(FastList<byte> Data)
        {
            Debug.Log("RealTime CitizenSchedule OnSaveData - Start");

            if (RealTimeResidentAI == null)
            {
                Debug.LogError("RealTime CitizenSchedule OnSaveData failed: RealTimeResidentAI is null.");
                return;
            }

            var citizenMgr = Singleton<CitizenManager>.instance;

            if (citizenMgr == null)
            {
                Debug.LogError("RealTime CitizenSchedule OnSaveData failed: CitizenManager is null.");
                return;
            }

            int recordCount = citizenMgr.m_citizens.m_buffer.Length;

            var residentSchedules = RealTimeResidentAI.GetResidentSchedules();

            citizens = CitizenManager.instance.m_citizens.m_buffer;

            StorageData.WriteUInt16(iCITIZEN_SCHEDULE_DATA_VERSION, Data);
            StorageData.WriteInt32(recordCount, Data);

            for (ushort citizenId = 0; citizenId < citizens.Length; citizenId++)
            {
                ref var schedule = ref residentSchedules[citizenId];

                StorageData.WriteUInt32(uiTUPLE_START, Data);

                StorageData.WriteUInt16(citizenId, Data);

                StorageData.WriteInt32((int)schedule.CurrentState, Data);
                StorageData.WriteInt32((int)schedule.Hint, Data);
                StorageData.WriteUInt16(schedule.EventBuilding, Data);

                StorageData.WriteInt32((int)schedule.WorkStatus, Data);
                StorageData.WriteInt32((int)schedule.SchoolStatus, Data);
                StorageData.WriteInt32(schedule.FindVisitPlaceAttempts, Data);
                StorageData.WriteByte(schedule.VacationDaysLeft, Data);

                StorageData.WriteUInt16(schedule.WorkBuilding, Data);
                StorageData.WriteUInt16(schedule.SchoolBuilding, Data);
                StorageData.WriteDateTime(schedule.DepartureTime, Data);

                StorageData.WriteInt32((int)schedule.ScheduledState, Data);
                StorageData.WriteInt32((int)schedule.LastScheduledState, Data);
                StorageData.WriteDateTime(schedule.ScheduledStateTime, Data);
                StorageData.WriteInt32((int)schedule.ScheduledMealType, Data);
                StorageData.WriteFloat(schedule.TravelTimeToWork, Data);
                StorageData.WriteFloat(schedule.TravelTimeToSchool, Data);

                StorageData.WriteInt32((int)schedule.WorkShift, Data);
                StorageData.WriteFloat(schedule.WorkShiftStartHour, Data);
                StorageData.WriteFloat(schedule.WorkShiftEndHour, Data);
                StorageData.WriteBool(schedule.WorksOnWeekends, Data);

                StorageData.WriteInt32((int)schedule.SchoolClass, Data);
                StorageData.WriteFloat(schedule.SchoolClassStartHour, Data);
                StorageData.WriteFloat(schedule.SchoolClassEndHour, Data);

                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }

            Debug.Log("RealTime CitizenSchedule OnSaveData - End");
        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int iCitizenScheduleVersion = StorageData.ReadUInt16(Data, ref iIndex);
                Debug.Log("RealTime CitizenSchedule - Global: " + iGlobalVersion + " BufferVersion: " + iCitizenScheduleVersion + " DataLength: " + Data.Length + " Index: " + iIndex);

                residentSchedules = [];
                int recordCount = StorageData.ReadInt32(Data, ref iIndex);

                for (int i = 0; i < recordCount; ++i)
                {
                    CheckStartTuple($"Buffer({i})", iCitizenScheduleVersion, Data, ref iIndex);

                    ushort citizenId = StorageData.ReadUInt16(Data, ref iIndex);
                    if (citizenId >= residentSchedules.Length)
                    {
                        throw new InvalidDataException($"Citizen id {citizenId} is outside resident schedule buffer.");
                    }

                    var schedule = residentSchedules[citizenId];

                    schedule.CurrentState = (ResidentState)StorageData.ReadInt32(Data, ref iIndex);
                    schedule.Hint = (ScheduleHint)StorageData.ReadInt32(Data, ref iIndex);
                    schedule.EventBuilding = StorageData.ReadUInt16(Data, ref iIndex);

                    schedule.WorkStatus = (WorkStatus)StorageData.ReadInt32(Data, ref iIndex);
                    schedule.SchoolStatus = (SchoolStatus)StorageData.ReadInt32(Data, ref iIndex);
                    schedule.FindVisitPlaceAttempts = StorageData.ReadInt32(Data, ref iIndex);
                    schedule.VacationDaysLeft = StorageData.ReadByte(Data, ref iIndex);

                    schedule.WorkBuilding = StorageData.ReadUInt16(Data, ref iIndex);
                    schedule.SchoolBuilding = StorageData.ReadUInt16(Data, ref iIndex);
                    schedule.DepartureTime = StorageData.ReadDateTime(Data, ref iIndex);

                    var scheduledState = (ResidentState)StorageData.ReadInt32(Data, ref iIndex);
                    var lastScheduledState = (ResidentState)StorageData.ReadInt32(Data, ref iIndex);
                    var scheduledStateTime = StorageData.ReadDateTime(Data, ref iIndex);
                    var scheduledMealType = (MealType)StorageData.ReadInt32(Data, ref iIndex);
                    float travelTimeToWork = StorageData.ReadFloat(Data, ref iIndex);
                    float travelTimeToSchool = StorageData.ReadFloat(Data, ref iIndex);

                    var workShift = (WorkShift)StorageData.ReadInt32(Data, ref iIndex);
                    float workShiftStartHour = StorageData.ReadFloat(Data, ref iIndex);
                    float workShiftEndHour = StorageData.ReadFloat(Data, ref iIndex);
                    bool worksOnWeekends = StorageData.ReadBool(Data, ref iIndex);

                    var schoolClass = (SchoolClass)StorageData.ReadInt32(Data, ref iIndex);
                    float schoolClassStartHour = StorageData.ReadFloat(Data, ref iIndex);
                    float schoolClassEndHour = StorageData.ReadFloat(Data, ref iIndex);

                    schedule.UpdateScheduleState(scheduledState, lastScheduledState, scheduledStateTime, scheduledMealType);
                    schedule.UpdateTravelTimeToWork(travelTimeToWork);
                    schedule.UpdateTravelTimeToSchool(travelTimeToSchool);
                    schedule.UpdateWorkShift(workShift, workShiftStartHour, workShiftEndHour, worksOnWeekends);
                    schedule.UpdateSchoolClass(schoolClass, schoolClassStartHour, schoolClassEndHour);

                    if (schedule.WorkShift != WorkShift.Unemployed
                        && schedule.WorkShift != WorkShift.Event
                        && citizens[citizenId].m_workBuilding != 0)
                    {
                        schedule.UpdateWorkShiftHours(schedule.WorkShift, citizens[citizenId].m_workBuilding);
                    }

                    if (schedule.SchoolClass != SchoolClass.NoSchool)
                    {
                        schedule.UpdateSchoolClassHours(schedule.SchoolClass);
                    }

                    residentSchedules[citizenId] = schedule;

                    uint maybeEndTuple = StorageData.ReadUInt32(Data, ref iIndex);

                    if (maybeEndTuple != uiTUPLE_END)
                    {
                        StorageData.ReadUInt32(Data, ref iIndex);

                        CheckEndTuple($"Buffer({i})", iCitizenScheduleVersion, Data, ref iIndex);
                    }
                }
            }
        }

        private static void CheckStartTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleStart = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleStart != uiTUPLE_START)
                {
                    throw new Exception($"CitizenSchedule Buffer start tuple not found at: {sTupleLocation}");
                }
            }
        }

        private static void CheckEndTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleEnd = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleEnd != uiTUPLE_END)
                {
                    throw new Exception($"CitizenSchedule Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }
    }
}
