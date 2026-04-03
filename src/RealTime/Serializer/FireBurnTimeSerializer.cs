// FireBurnTimeSerializer.cs

namespace RealTime.Serializer
{
    using System;
    using RealTime.Managers;
    using UnityEngine;

    public class FireBurnTimeSerializer
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const ushort iFIRE_BURN_START_TIME_DATA_VERSION = 1;

        public static void SaveData(FastList<byte> Data)
        {
            Debug.Log("RealTime FireBurnTime OnSaveData - Start");

            // Write out metadata
            StorageData.WriteUInt16(iFIRE_BURN_START_TIME_DATA_VERSION, Data);
            StorageData.WriteInt32(FireBurnTimeManager.FireBurnTime.Count, Data);

            // Write out each buffer settings
            foreach (var kvp in FireBurnTimeManager.FireBurnTime)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write actual settings
                StorageData.WriteUInt16(kvp.Key, Data);
                StorageData.WriteDateTime(kvp.Value.StartDate, Data);
                StorageData.WriteFloat(kvp.Value.StartTime, Data);
                StorageData.WriteFloat(kvp.Value.Duration, Data);

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }

            Debug.Log("RealTime FireBurnTime OnSaveData - End");
        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int iFireBurnStartTimeVersion = StorageData.ReadUInt16(Data, ref iIndex);
                Debug.Log("RealTime FireBurnTime - Global: " + iGlobalVersion + " BufferVersion: " + iFireBurnStartTimeVersion + " DataLength: " + Data.Length + " Index: " + iIndex);
                FireBurnTimeManager.FireBurnTime ??= [];

                if (FireBurnTimeManager.FireBurnTime.Count > 0)
                {
                    FireBurnTimeManager.FireBurnTime.Clear();
                }

                int FireBurnStartTime_Count = StorageData.ReadInt32(Data, ref iIndex);
                for (int i = 0; i < FireBurnStartTime_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", iFireBurnStartTimeVersion, Data, ref iIndex);

                    ushort BuildingId = StorageData.ReadUInt16(Data, ref iIndex);

                    var StartDate = StorageData.ReadDateTime(Data, ref iIndex);
                    float StartTime = StorageData.ReadFloat(Data, ref iIndex);
                    float Duration = StorageData.ReadFloat(Data, ref iIndex);

                    var burnTime = new FireBurnTimeManager.BurnTime()
                    {
                        StartDate = StartDate,
                        StartTime = StartTime,
                        Duration = Duration
                    };

                    FireBurnTimeManager.FireBurnTime.Add(BuildingId, burnTime);

                    //if end go to next item in the manager
                    // if not end read another number and then read the end
                    uint maybeEndTuple = StorageData.ReadUInt32(Data, ref iIndex);

                    if (maybeEndTuple != uiTUPLE_END)
                    {
                        StorageData.ReadUInt32(Data, ref iIndex);

                        CheckEndTuple($"Buffer({i})", iFireBurnStartTimeVersion, Data, ref iIndex);
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
                    throw new Exception($"FireBurnTime Buffer start tuple not found at: {sTupleLocation}");
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
                    throw new Exception($"FireBurnTime Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

    }
}
