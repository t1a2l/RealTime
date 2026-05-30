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

        private const ushort iBUILDING_WORK_TIME_DATA_VERSION = 6;

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
                    StorageData.WriteFloat(shift.StartHour, Data);
                    StorageData.WriteFloat(shift.EndHour, Data);
                }

                StorageData.WriteBool(kvp.Value.IsDefault, Data);
                StorageData.WriteBool(kvp.Value.IsPrefab, Data);
                StorageData.WriteBool(kvp.Value.IsGlobal, Data);
                StorageData.WriteBool(kvp.Value.IsLocked, Data);
                StorageData.WriteBool(kvp.Value.IgnorePolicy, Data);

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
                StorageData.WriteBool(kvp.IgnorePolicy, Data);

                StorageData.WriteInt32(kvp.WorkDays.Length, Data);

                foreach (var day in kvp.WorkDays)
                {
                    StorageData.WriteInt32((int)day, Data);
                }

                StorageData.WriteInt32(kvp.WorkShifts.Length, Data);

                foreach (var shift in kvp.WorkShifts)
                {
                    StorageData.WriteFloat(shift.StartHour, Data);
                    StorageData.WriteFloat(shift.EndHour, Data);
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
                int iBuildingWorkTimeVersion = StorageData.ReadUInt16(Data, ref iIndex);
                Debug.Log("RealTime BuildingWorkTime - Global: " + iGlobalVersion + " BufferVersion: " + iBuildingWorkTimeVersion + " DataLength: " + Data.Length + " Index: " + iIndex);

                BuildingWorkTimeManager.BuildingsWorkTime ??= [];

                if (BuildingWorkTimeManager.BuildingsWorkTime.Count > 0)
                {
                    BuildingWorkTimeManager.BuildingsWorkTime.Clear();
                }

                if (iBuildingWorkTimeVersion >= 4)
                {
                    CheckStartTuple($"BuildingsWorkTime Start", iBuildingWorkTimeVersion, Data, ref iIndex);

                    BuildingWorkTimeManager.BuildingsWorkTimePrefabs ??= [];

                    if (BuildingWorkTimeManager.BuildingsWorkTimePrefabs.Count > 0)
                    {
                        BuildingWorkTimeManager.BuildingsWorkTimePrefabs.Clear();
                    }
                }

                int BuildingsWorkTime_Count = StorageData.ReadInt32(Data, ref iIndex);
                for (int i = 0; i < BuildingsWorkTime_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", iBuildingWorkTimeVersion, Data, ref iIndex);

                    if (iBuildingWorkTimeVersion < 6)
                    {
                        ushort BuildingId = StorageData.ReadUInt16(Data, ref iIndex);
                        bool _ = StorageData.ReadBool(Data, ref iIndex);
                        bool WorkAtWeekands = StorageData.ReadBool(Data, ref iIndex);
                        bool HasExtendedWorkShift = StorageData.ReadBool(Data, ref iIndex);
                        bool HasContinuousWorkShift = StorageData.ReadBool(Data, ref iIndex);
                        int WorkShifts = StorageData.ReadInt32(Data, ref iIndex);

                        var workTime = new BuildingWorkTimeManager.WorkTime()
                        {
                            IsDefault = true,
                            IsPrefab = false,
                            IsGlobal = false,
                            IsLocked = false,
                            IgnorePolicy = false
                        };

                        if (iBuildingWorkTimeVersion >= 2)
                        {
                            workTime.IsDefault = StorageData.ReadBool(Data, ref iIndex);
                            workTime.IsPrefab = StorageData.ReadBool(Data, ref iIndex);
                            workTime.IsGlobal = StorageData.ReadBool(Data, ref iIndex);
                        }

                        if (iBuildingWorkTimeVersion >= 3)
                        {
                            workTime.IsLocked = StorageData.ReadBool(Data, ref iIndex);
                        }

                        if (iBuildingWorkTimeVersion >= 5)
                        {
                            workTime.IgnorePolicy = StorageData.ReadBool(Data, ref iIndex);
                        }

                        workTime = BuildingWorkTimeManager.LegacyToWorkTime(workTime, WorkAtWeekands, HasExtendedWorkShift, HasContinuousWorkShift, WorkShifts);
                        BuildingWorkTimeManager.BuildingsWorkTime.Add(BuildingId, workTime);
                    }
                    else
                    {
                        ushort BuildingId = StorageData.ReadUInt16(Data, ref iIndex);

                        int WorkDays_Length = StorageData.ReadInt32(Data, ref iIndex);

                        var days = new DayOfWeek[WorkDays_Length];

                        for (int j = 0; j < WorkDays_Length; j++)
                        {
                            days[j] = (DayOfWeek)StorageData.ReadInt32(Data, ref iIndex);
                        }

                        int WorkShifts_Length = StorageData.ReadInt32(Data, ref iIndex);

                        var shifts = new BuildingWorkTimeManager.WorkShiftTime[WorkShifts_Length];

                        for (int j = 0; j < WorkShifts_Length; j++)
                        {
                            shifts[j].StartHour = StorageData.ReadFloat(Data, ref iIndex);
                            shifts[j].EndHour = StorageData.ReadFloat(Data, ref iIndex);
                        }

                        var workTime = new BuildingWorkTimeManager.WorkTime()
                        {
                            WorkDays = days,
                            WorkShifts = shifts,
                            IsDefault = true,
                            IsPrefab = false,
                            IsGlobal = false,
                            IsLocked = false,
                            IgnorePolicy = false
                        };
                        BuildingWorkTimeManager.BuildingsWorkTime.Add(BuildingId, workTime);
                    }

                    CheckEndTuple($"Buffer({i})", iBuildingWorkTimeVersion, Data, ref iIndex);
                }

                if (iBuildingWorkTimeVersion >= 4)
                {
                    CheckEndTuple($"BuildingsWorkTime End", iBuildingWorkTimeVersion, Data, ref iIndex);

                    CheckStartTuple($"BuildingsWorkTimePrefabs Start", iBuildingWorkTimeVersion, Data, ref iIndex);

                    int BuildingsWorkTimePrefabs_Count = StorageData.ReadInt32(Data, ref iIndex);
                    for (int i = 0; i < BuildingsWorkTimePrefabs_Count; i++)
                    {
                        CheckStartTuple($"Buffer({i})", iBuildingWorkTimeVersion, Data, ref iIndex);

                        if (iBuildingWorkTimeVersion < 6)
                        {
                            string InfoName = StorageData.ReadString(Data, ref iIndex);
                            string BuildingAI = StorageData.ReadString(Data, ref iIndex);
                            bool WorkAtNight = StorageData.ReadBool(Data, ref iIndex);
                            bool WorkAtWeekands = StorageData.ReadBool(Data, ref iIndex);
                            bool HasExtendedWorkShift = StorageData.ReadBool(Data, ref iIndex);
                            bool HasContinuousWorkShift = StorageData.ReadBool(Data, ref iIndex);
                            int WorkShifts = StorageData.ReadInt32(Data, ref iIndex);

                            var workTimePrefab = new BuildingWorkTimeManager.WorkTimePrefab()
                            {
                                InfoName = InfoName,
                                BuildingAI = BuildingAI,
                            };

                            if (iBuildingWorkTimeVersion >= 5)
                            {
                                workTimePrefab.IgnorePolicy = StorageData.ReadBool(Data, ref iIndex);
                            }

                            workTimePrefab = BuildingWorkTimeManager.LegacyToWorkTimePrefab(workTimePrefab, WorkAtWeekands, HasExtendedWorkShift, HasContinuousWorkShift, WorkShifts);

                            BuildingWorkTimeManager.BuildingsWorkTimePrefabs.Add(workTimePrefab);
                        }
                        else
                        {
                            string InfoName = StorageData.ReadString(Data, ref iIndex);
                            string BuildingAI = StorageData.ReadString(Data, ref iIndex);
                            bool IgnorePolicy = StorageData.ReadBool(Data, ref iIndex);

                            int WorkDays_Length = StorageData.ReadInt32(Data, ref iIndex);

                            var days = new DayOfWeek[WorkDays_Length];

                            for (int j = 0; j < WorkDays_Length; j++)
                            {
                                days[j] = (DayOfWeek)StorageData.ReadInt32(Data, ref iIndex);
                            }

                            int WorkShifts_Length = StorageData.ReadInt32(Data, ref iIndex);

                            var shifts = new BuildingWorkTimeManager.WorkShiftTime[WorkShifts_Length];

                            for (int j = 0; j < WorkShifts_Length; j++)
                            {
                                shifts[j].StartHour = StorageData.ReadFloat(Data, ref iIndex);
                                shifts[j].EndHour = StorageData.ReadFloat(Data, ref iIndex);
                            }

                            var workTimePrefab = new BuildingWorkTimeManager.WorkTimePrefab()
                            {
                                InfoName = InfoName,
                                BuildingAI = BuildingAI,
                                IgnorePolicy = IgnorePolicy,
                                WorkDays = days,
                                WorkShifts = shifts
                            };

                            BuildingWorkTimeManager.BuildingsWorkTimePrefabs.Add(workTimePrefab);
                        }

                        CheckEndTuple($"Buffer({i})", iBuildingWorkTimeVersion, Data, ref iIndex);
                    }

                    CheckEndTuple($"BuildingsWorkTimePrefabs End", iBuildingWorkTimeVersion, Data, ref iIndex);
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

    }
}
