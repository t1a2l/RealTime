// BuildingWorkTimeSerializer.cs

namespace RealTime.Serializer
{
    using System;
    using RealTime.Managers;
    using UnityEngine;

    public class BuildingWorkTimeSerializer
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const ushort iBUILDING_WORK_TIME_DATA_VERSION = 7;

        public static void SaveData(FastList<byte> Data)
        {
            Debug.Log("RealTime BuildingWorkTime OnSaveData - Start");

            // Write out metadata
            StorageData.WriteUInt16(iBUILDING_WORK_TIME_DATA_VERSION, Data);

            StorageData.WriteUInt32(uiTUPLE_START, Data);

            StorageData.WriteInt32(BuildingWorkTimeManager.BuildingsWorkTime.Count, Data);

            // Write out each buffer settings
            foreach (var kvp in BuildingWorkTimeManager.BuildingsWorkTime)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write actual settings
                StorageData.WriteUInt16(kvp.Key, Data);

                StorageData.WriteInt32(kvp.Value.WorkDays.Length, Data);

                foreach (var day in kvp.Value.WorkDays)
                {
                    StorageData.WriteInt32((int)day, Data);
                }

                StorageData.WriteInt32(kvp.Value.WorkShifts.Length, Data);

                foreach (var shift in kvp.Value.WorkShifts)
                {
                    StorageData.WriteFloat(shift.StartTime, Data);
                    StorageData.WriteFloat(shift.EndTime, Data);
                }

                StorageData.WriteBool(kvp.Value.IsDefault, Data);
                StorageData.WriteBool(kvp.Value.IsPrefab, Data);
                StorageData.WriteBool(kvp.Value.IsGlobal, Data);
                StorageData.WriteBool(kvp.Value.IsLocked, Data);

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }

            StorageData.WriteUInt32(uiTUPLE_END, Data);

            // -- prefab write

            StorageData.WriteUInt32(uiTUPLE_START, Data);

            StorageData.WriteInt32(BuildingWorkTimeManager.BuildingsWorkTimePrefabs.Count, Data);

            foreach (var kvp in BuildingWorkTimeManager.BuildingsWorkTimePrefabs)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write actual settings
                StorageData.WriteString(kvp.InfoName, Data);
                StorageData.WriteString(kvp.BuildingAI, Data);

                StorageData.WriteInt32(kvp.WorkDays.Length, Data);

                foreach (var day in kvp.WorkDays)
                {
                    StorageData.WriteInt32((int)day, Data);
                }

                StorageData.WriteInt32(kvp.WorkShifts.Length, Data);

                foreach (var shift in kvp.WorkShifts)
                {
                    StorageData.WriteFloat(shift.StartTime, Data);
                    StorageData.WriteFloat(shift.EndTime, Data);
                }

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }

            StorageData.WriteUInt32(uiTUPLE_END, Data);

            Debug.Log("RealTime BuildingWorkTime OnSaveData - End");
        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int version = StorageData.ReadUInt16(Data, ref iIndex);
                Debug.Log("RealTime BuildingWorkTime - Global: " + iGlobalVersion + " BufferVersion: " + version + " DataLength: " + Data.Length + " Index: " + iIndex);

                BuildingWorkTimeManager.BuildingsWorkTime ??= [];
                BuildingWorkTimeManager.BuildingsWorkTime.Clear();

                BuildingWorkTimeManager.BuildingsWorkTimePrefabs ??= [];
                BuildingWorkTimeManager.BuildingsWorkTimePrefabs.Clear();

                switch (version)
                {
                    case < 4:
                        LoadLegacyBuffers(version, Data, ref iIndex);
                        return;

                    case < 6:
                        LoadLegacyBuffers(version, Data, ref iIndex);
                        LoadLegacyPrefabs(version, Data, ref iIndex);
                        return;

                    default:
                        LoadCurrentBuffers(version, Data, ref iIndex);
                        LoadCurrentPrefabs(version, Data, ref iIndex);
                        return;
                }
            }
        }

        private static void LoadCurrentBuffers(int version, byte[] data, ref int index)
        {
            CheckStartTuple("BuildingsWorkTime Start", version, data, ref index);

            int workTimeCount = StorageData.ReadInt32(data, ref index);

            for (int i = 0; i < workTimeCount; i++)
            {
                CheckStartTuple($"Buffer({i})", version, data, ref index);

                ushort buildingId = StorageData.ReadUInt16(data, ref index);

                var workTime = ReadCurrentWorkTime(data, ref index);

                if (version < 7)
                {
                    // Obsolete IgnorePolicy field.
                    StorageData.ReadBool(data, ref index);
                }

                BuildingWorkTimeManager.BuildingsWorkTime.Add(buildingId, workTime);

                CheckEndTuple($"Buffer({i})", version, data, ref index);
            }

            CheckEndTuple("BuildingsWorkTime End", version, data, ref index);
        }

        private static void LoadCurrentPrefabs(int version, byte[] data, ref int index)
        {
            CheckEndTuple("BuildingsWorkTime End", version, data, ref index);

            CheckStartTuple("BuildingsWorkTimePrefabs Start", version, data, ref index);

            int prefabCount = StorageData.ReadInt32(data, ref index);

            for (int i = 0; i < prefabCount; i++)
            {
                CheckStartTuple($"Prefab({i})", version, data, ref index);

                var workTimePrefab = ReadCurrentWorkTimePrefab(version,data, ref index);

                BuildingWorkTimeManager.BuildingsWorkTimePrefabs.Add(workTimePrefab);

                CheckEndTuple($"Prefab({i})", version, data, ref index);
            }

            CheckEndTuple("BuildingsWorkTimePrefabs End", version, data, ref index);
        }

        private static void LoadLegacyBuffers(int version, byte[] data, ref int index)
        {
            if (version >= 4)
            {
                CheckStartTuple("BuildingsWorkTime Start", version, data, ref index);
            }

            int count = StorageData.ReadInt32(data, ref index);

            for (int i = 0; i < count; i++)
            {
                CheckStartTuple($"Buffer({i})", version, data, ref index);

                ushort buildingId = StorageData.ReadUInt16(data, ref index);

                var workTime = ReadLegacyWorkTime(version, data, ref index);

                BuildingWorkTimeManager.BuildingsWorkTime.Add(buildingId, workTime);

                CheckEndTuple($"Buffer({i})", version, data, ref index);
            }

            CheckEndTuple("BuildingsWorkTime End", version, data, ref index);
        }

        private static void LoadLegacyPrefabs(int version, byte[] data, ref int index)
        {
            CheckEndTuple("BuildingsWorkTime End", version, data, ref index);

            CheckStartTuple("BuildingsWorkTimePrefabs Start", version, data, ref index);

            int prefabCount = StorageData.ReadInt32(data, ref index);

            for (int i = 0; i < prefabCount; i++)
            {
                CheckStartTuple($"Prefab({i})", version, data, ref index);

                var workTimePrefab = ReadLegacyWorkTimePrefab(version, data, ref index);

                BuildingWorkTimeManager.BuildingsWorkTimePrefabs.Add(workTimePrefab);

                CheckEndTuple($"Prefab({i})", version, data, ref index);
            }

            CheckEndTuple("BuildingsWorkTimePrefabs End", version, data, ref index);
        }

        private static void CheckStartTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleStart = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleStart != uiTUPLE_START)
                {
                    throw new Exception($"BuildingWorkTime Buffer start tuple not found at: {sTupleLocation}");
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
                    throw new Exception($"BuildingWorkTime Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

        private static BuildingWorkTimeManager.WorkTime ReadCurrentWorkTime(byte[] data, ref int index)
        {
            int workDayCount = StorageData.ReadInt32(data, ref index);
            var workDays = new DayOfWeek[workDayCount];

            for (int i = 0; i < workDayCount; i++)
            {
                workDays[i] = (DayOfWeek)StorageData.ReadInt32(data, ref index);
            }

            int workShiftCount = StorageData.ReadInt32(data, ref index);
            var workShifts = new BuildingWorkTimeManager.WorkShiftTime[workShiftCount];

            for (int i = 0; i < workShiftCount; i++)
            {
                workShifts[i].StartTime = StorageData.ReadFloat(data, ref index);
                workShifts[i].EndTime = StorageData.ReadFloat(data, ref index);
            }

            return new BuildingWorkTimeManager.WorkTime
            {
                WorkDays = workDays,
                WorkShifts = workShifts,
                IsDefault = StorageData.ReadBool(data, ref index),
                IsPrefab = StorageData.ReadBool(data, ref index),
                IsGlobal = StorageData.ReadBool(data, ref index),
                IsLocked = StorageData.ReadBool(data, ref index)
            };
        }

        private static BuildingWorkTimeManager.WorkTimePrefab ReadCurrentWorkTimePrefab(int version, byte[] data, ref int index)
        {
            string infoName = StorageData.ReadString(data, ref index);

            string buildingAI = StorageData.ReadString(data, ref index);

            if (version < 7)
            {
                // Version 6 only: obsolete IgnorePolicy field.
                StorageData.ReadBool(data, ref index);
            }

            int workDayCount = StorageData.ReadInt32(data, ref index);

            var workDays = new DayOfWeek[workDayCount];

            for (int i = 0; i < workDayCount; i++)
            {
                workDays[i] = (DayOfWeek)StorageData.ReadInt32(data, ref index);
            }

            int workShiftCount = StorageData.ReadInt32(data, ref index);

            var workShifts = new BuildingWorkTimeManager.WorkShiftTime[workShiftCount];

            for (int i = 0; i < workShiftCount; i++)
            {
                workShifts[i].StartTime = StorageData.ReadFloat(data, ref index);

                workShifts[i].EndTime = StorageData.ReadFloat(data, ref index);
            }

            return new BuildingWorkTimeManager.WorkTimePrefab
            {
                InfoName = infoName,
                BuildingAI = buildingAI,
                WorkDays = workDays,
                WorkShifts = workShifts
            };
        }

        private static BuildingWorkTimeManager.WorkTime ReadLegacyWorkTime(int version, byte[] data, ref int index)
        {
            StorageData.ReadBool(data, ref index); // obsolete WorkAtNight
            bool workAtWeekends = StorageData.ReadBool(data, ref index);
            bool extendedShift = StorageData.ReadBool(data, ref index);
            bool continuousShift = StorageData.ReadBool(data, ref index);
            int shiftCount = StorageData.ReadInt32(data, ref index);

            var workTime = new BuildingWorkTimeManager.WorkTime
            {
                IsDefault = true,
                IsPrefab = false,
                IsGlobal = false,
                IsLocked = false
            };

            if (version >= 2)
            {
                workTime.IsDefault = StorageData.ReadBool(data, ref index);
                workTime.IsPrefab = StorageData.ReadBool(data, ref index);
                workTime.IsGlobal = StorageData.ReadBool(data, ref index);
            }

            if (version >= 3)
            {
                workTime.IsLocked = StorageData.ReadBool(data, ref index);
            }

            if (version >= 5)
            {
                StorageData.ReadBool(data, ref index); // obsolete IgnorePolicy
            }

            workTime = BuildingWorkTimeManager.LegacyToWorkTime(workTime, workAtWeekends, extendedShift, continuousShift, shiftCount);

            return workTime;
        }

        private static BuildingWorkTimeManager.WorkTimePrefab ReadLegacyWorkTimePrefab(int version, byte[] data, ref int index)
        {
            string infoName = StorageData.ReadString(data, ref index);

            string buildingAI = StorageData.ReadString(data, ref index);

            // Obsolete WorkAtNight field
            StorageData.ReadBool(data, ref index);

            bool workAtWeekends = StorageData.ReadBool(data, ref index);

            bool hasExtendedWorkShift = StorageData.ReadBool(data, ref index);

            bool hasContinuousWorkShift = StorageData.ReadBool(data, ref index);

            int workShiftCount = StorageData.ReadInt32(data, ref index);

            if (version >= 5)
            {
                // Obsolete IgnorePolicy field.
                StorageData.ReadBool(data, ref index);
            }

            var workTimePrefab = new BuildingWorkTimeManager.WorkTimePrefab
            {
                InfoName = infoName,
                BuildingAI = buildingAI
            };

            return BuildingWorkTimeManager.LegacyToWorkTimePrefab(workTimePrefab, workAtWeekends, hasExtendedWorkShift, hasContinuousWorkShift, workShiftCount);
        }

    }
}
