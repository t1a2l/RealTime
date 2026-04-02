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

        private const ushort iBUILDING_WORK_TIME_DATA_VERSION = 5;

        public static void SaveData(FastList<byte> Data)
        {
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
                StorageData.WriteBool(kvp.Value.WorkAtNight, Data);
                StorageData.WriteBool(kvp.Value.WorkAtWeekands, Data);
                StorageData.WriteBool(kvp.Value.HasExtendedWorkShift, Data);
                StorageData.WriteBool(kvp.Value.HasContinuousWorkShift, Data);
                StorageData.WriteInt32(kvp.Value.WorkShifts, Data);
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
                StorageData.WriteBool(kvp.WorkAtNight, Data);
                StorageData.WriteBool(kvp.WorkAtWeekands, Data);
                StorageData.WriteBool(kvp.HasExtendedWorkShift, Data);
                StorageData.WriteBool(kvp.HasContinuousWorkShift, Data);
                StorageData.WriteBool(kvp.IgnorePolicy, Data);
                StorageData.WriteInt32(kvp.WorkShifts, Data);

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }

            StorageData.WriteUInt32(uiTUPLE_END, Data);
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

                    ushort BuildingId = StorageData.ReadUInt16(Data, ref iIndex);

                    bool WorkAtNight = StorageData.ReadBool(Data, ref iIndex);
                    bool WorkAtWeekands = StorageData.ReadBool(Data, ref iIndex);
                    bool HasExtendedWorkShift = StorageData.ReadBool(Data, ref iIndex);
                    bool HasContinuousWorkShift = StorageData.ReadBool(Data, ref iIndex);
                    int WorkShifts = StorageData.ReadInt32(Data, ref iIndex);

                    var workTime = new BuildingWorkTimeManager.WorkTime()
                    {
                        WorkAtNight = WorkAtNight,
                        WorkAtWeekands = WorkAtWeekands,
                        HasExtendedWorkShift = HasExtendedWorkShift,
                        HasContinuousWorkShift = HasContinuousWorkShift,
                        WorkShifts = WorkShifts,
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

                    BuildingWorkTimeManager.BuildingsWorkTime.Add(BuildingId, workTime);
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
                            WorkAtNight = WorkAtNight,
                            WorkAtWeekands = WorkAtWeekands,
                            HasExtendedWorkShift = HasExtendedWorkShift,
                            HasContinuousWorkShift = HasContinuousWorkShift,
                            WorkShifts = WorkShifts
                        };

                        if (iBuildingWorkTimeVersion >= 5)
                        {
                            workTimePrefab.IgnorePolicy = StorageData.ReadBool(Data, ref iIndex);
                        }

                        BuildingWorkTimeManager.BuildingsWorkTimePrefabs.Add(workTimePrefab);

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
