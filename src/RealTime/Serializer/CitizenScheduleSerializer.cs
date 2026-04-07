namespace RealTime.Serializer
{
    using System;
    using System.IO;
    using ColossalFramework;
    using ICities;
    using RealTime.CustomAI;
    using UnityEngine;

    public class CitizenScheduleSerializer
    {
        private const ushort iCITIZEN_SCHEDULE_DATA_VERSION = 1;

        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const string HeaderDataId = "RealTime.CitizenSchedule.Header";
        private const string ChunkDataIdPrefix = "RealTime.CitizenSchedule.";

        private const int CitizensPerChunk = 5000;

        internal static CitizenSchedule[] residentSchedules;

        internal static RealTimeResidentAI<ResidentAI, Citizen> RealTimeResidentAI { get; set; }

        public static void SaveData(ISerializableData serializableData)
        {
            Debug.Log("RealTime CitizenSchedule OnSaveData - Start");

            if (serializableData == null)
            {
                Debug.LogError("RealTime CitizenSchedule OnSaveData failed: serializableData is null.");
                return;
            }

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

            var schedules = RealTimeResidentAI.GetResidentSchedules();
            var citizens = citizenMgr.m_citizens.m_buffer;

            if (schedules.Length < citizens.Length)
            {
                Debug.LogError($"RealTime CitizenSchedule OnSaveData failed: residentSchedules.Length={schedules.Length}, citizens.Length={citizens.Length}");
                return;
            }

            int totalSavedCount = 0;
            int chunkCount = 0;

            var chunkData = new FastList<byte>();
            int chunkRecordCount = 0;

            WriteChunkHeader(chunkData, 0); // placeholder record count

            for (uint citizenId = 0; citizenId < citizens.Length; citizenId++)
            {
                var flags = citizens[citizenId].m_flags;
                if ((flags & Citizen.Flags.Created) == 0 || (flags & Citizen.Flags.DummyTraffic) != 0)
                {
                    continue;
                }

                ref var schedule = ref schedules[citizenId];

                WriteCitizenSchedule(chunkData, citizenId, ref schedule);

                chunkRecordCount++;
                totalSavedCount++;

                if (chunkRecordCount >= CitizensPerChunk)
                {
                    UpdateChunkRecordCount(chunkData, chunkRecordCount);
                    serializableData.SaveData(GetChunkDataId(chunkCount), chunkData.ToArray());

                    Debug.Log($"RealTime CitizenSchedule saved chunk {chunkCount}, records={chunkRecordCount}, bytes={chunkData.m_size}");

                    chunkCount++;

                    chunkData = new FastList<byte>();
                    chunkRecordCount = 0;
                    WriteChunkHeader(chunkData, 0);
                }
            }

            if (chunkRecordCount > 0)
            {
                UpdateChunkRecordCount(chunkData, chunkRecordCount);
                serializableData.SaveData(GetChunkDataId(chunkCount), chunkData.ToArray());

                Debug.Log($"RealTime CitizenSchedule saved chunk {chunkCount}, records={chunkRecordCount}, bytes={chunkData.m_size}");

                chunkCount++;
            }

            SaveMainHeader(serializableData, chunkCount);

            Debug.Log($"RealTime CitizenSchedule OnSaveData - End. totalSavedCount={totalSavedCount}, totalChunks={chunkCount}");
        }

        public static void LoadData(ISerializableData serializableData)
        {
            if (serializableData == null)
            {
                Debug.LogError("RealTime CitizenSchedule OnLoadData failed: serializableData is null.");
                return;
            }

            var citizenMgr = Singleton<CitizenManager>.instance;
            if (citizenMgr == null)
            {
                Debug.LogError("RealTime CitizenSchedule OnLoadData failed: CitizenManager is null.");
                return;
            }

            residentSchedules = new CitizenSchedule[citizenMgr.m_citizens.m_size];

            byte[] headerBytes = serializableData.LoadData(HeaderDataId);
            if (headerBytes == null || headerBytes.Length == 0)
            {
                Debug.Log("RealTime CitizenSchedule OnLoadData: no chunked header found.");
                return;
            }

            int headerIndex = 0;
            ushort version = StorageData.ReadUInt16(headerBytes, ref headerIndex);
            int chunkCount = StorageData.ReadInt32(headerBytes, ref headerIndex);

            Debug.Log($"RealTime CitizenSchedule OnLoadData: version={version}, chunkCount={chunkCount}");

            var citizens = citizenMgr.m_citizens.m_buffer;

            for (int chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
            {
                byte[] chunkBytes = serializableData.LoadData(GetChunkDataId(chunkIndex));
                if (chunkBytes == null || chunkBytes.Length == 0)
                {
                    Debug.LogWarning($"RealTime CitizenSchedule missing chunk {chunkIndex}");
                    continue;
                }

                int index = 0;
                int chunkVersion = StorageData.ReadUInt16(chunkBytes, ref index);
                int recordCount = StorageData.ReadInt32(chunkBytes, ref index);

                Debug.Log($"RealTime CitizenSchedule loading chunk {chunkIndex}, recordCount={recordCount}, bytes={chunkBytes.Length}");

                for (int i = 0; i < recordCount; ++i)
                {
                    CheckStartTuple($"Chunk({chunkIndex}) Buffer({i})", chunkVersion, chunkBytes, ref index);

                    uint citizenId = StorageData.ReadUInt32(chunkBytes, ref index);
                    if (citizenId >= residentSchedules.Length)
                    {
                        throw new InvalidDataException($"Citizen id {citizenId} is outside resident schedule buffer.");
                    }

                    var schedule = residentSchedules[citizenId];

                    schedule.CurrentState = (ResidentState)StorageData.ReadInt32(chunkBytes, ref index);
                    schedule.Hint = (ScheduleHint)StorageData.ReadInt32(chunkBytes, ref index);
                    schedule.EventBuilding = StorageData.ReadUInt16(chunkBytes, ref index);

                    schedule.WorkStatus = (WorkStatus)StorageData.ReadInt32(chunkBytes, ref index);
                    schedule.SchoolStatus = (SchoolStatus)StorageData.ReadInt32(chunkBytes, ref index);
                    schedule.FindVisitPlaceAttempts = StorageData.ReadInt32(chunkBytes, ref index);
                    schedule.VacationDaysLeft = StorageData.ReadByte(chunkBytes, ref index);

                    schedule.WorkBuilding = StorageData.ReadUInt16(chunkBytes, ref index);
                    schedule.SchoolBuilding = StorageData.ReadUInt16(chunkBytes, ref index);
                    schedule.DepartureTime = StorageData.ReadDateTime(chunkBytes, ref index);

                    var scheduledState = (ResidentState)StorageData.ReadInt32(chunkBytes, ref index);
                    var lastScheduledState = (ResidentState)StorageData.ReadInt32(chunkBytes, ref index);
                    var scheduledStateTime = StorageData.ReadDateTime(chunkBytes, ref index);
                    var scheduledMealType = (MealType)StorageData.ReadInt32(chunkBytes, ref index);
                    var lastScheduledMealType = (MealType)StorageData.ReadInt32(chunkBytes, ref index);
                    float travelTimeToWork = StorageData.ReadFloat(chunkBytes, ref index);
                    float travelTimeToSchool = StorageData.ReadFloat(chunkBytes, ref index);

                    var workShift = (WorkShift)StorageData.ReadInt32(chunkBytes, ref index);
                    float workShiftStartHour = StorageData.ReadFloat(chunkBytes, ref index);
                    float workShiftEndHour = StorageData.ReadFloat(chunkBytes, ref index);
                    bool worksOnWeekends = StorageData.ReadBool(chunkBytes, ref index);

                    var schoolClass = (SchoolClass)StorageData.ReadInt32(chunkBytes, ref index);
                    float schoolClassStartHour = StorageData.ReadFloat(chunkBytes, ref index);
                    float schoolClassEndHour = StorageData.ReadFloat(chunkBytes, ref index);

                    schedule.UpdateScheduleState(scheduledState, lastScheduledState, scheduledStateTime, scheduledMealType, lastScheduledMealType);
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

                    CheckEndTuple($"Chunk({chunkIndex}) Buffer({i})", chunkVersion, chunkBytes, ref index);
                }
            }

            RealTimeResidentAI?.ApplyLoadedSchedules(residentSchedules);
        }

        private static void SaveMainHeader(ISerializableData serializableData, int chunkCount)
        {
            var header = new FastList<byte>();
            StorageData.WriteUInt16(iCITIZEN_SCHEDULE_DATA_VERSION, header);
            StorageData.WriteInt32(chunkCount, header);
            serializableData.SaveData(HeaderDataId, header.ToArray());
        }

        private static void WriteChunkHeader(FastList<byte> data, int recordCount)
        {
            StorageData.WriteUInt16(iCITIZEN_SCHEDULE_DATA_VERSION, data);
            StorageData.WriteInt32(recordCount, data);
        }

        private static void UpdateChunkRecordCount(FastList<byte> data, int recordCount)
        {
            byte[] bytes = BitConverter.GetBytes(recordCount);
            Buffer.BlockCopy(bytes, 0, data.m_buffer, 2, 4);
        }

        private static string GetChunkDataId(int chunkIndex) => ChunkDataIdPrefix + chunkIndex;

        private static void WriteCitizenSchedule(FastList<byte> Data, uint citizenId, ref CitizenSchedule schedule)
        {
            StorageData.WriteUInt32(uiTUPLE_START, Data);

            StorageData.WriteUInt32(citizenId, Data);

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
            StorageData.WriteInt32((int)schedule.LastScheduledMealType, Data);
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
