// EventRouteTimeSerializer.cs

namespace RealTime.Serializer
{
    using System;
    using RealTime.Managers;
    using UnityEngine;

    public class EventRouteTimeSerializer
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const ushort iEVENT_ROUTE_TIME_DATA_VERSION = 1;

        public static void SaveData(FastList<byte> Data)
        {
            Debug.Log("RealTime EventRouteTime OnSaveData - Start");

            // Write out metadata
            StorageData.WriteUInt16(iEVENT_ROUTE_TIME_DATA_VERSION, Data);
            StorageData.WriteInt32(EventRouteTimeManager.TimeSchedules.Count, Data);

            // Write out each buffer settings
            foreach (var kvp in EventRouteTimeManager.TimeSchedules)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write key
                StorageData.WriteUInt16(kvp.Key, Data);

                // Write Value length
                StorageData.WriteInt32(kvp.Value.Length, Data);

                for (int i = 0; i < kvp.Value.Length; i++)
                {
                    StorageData.WriteByte(kvp.Value[i].StartHour, Data);
                    StorageData.WriteByte(kvp.Value[i].StartMinute, Data);
                    StorageData.WriteByte(kvp.Value[i].Frequency, Data);
                    StorageData.WriteBool(kvp.Value[i].AutoOccur, Data);
                }

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }

            Debug.Log("RealTime EventRouteTime OnSaveData - End");
        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int iEventRouteTimeVersion = StorageData.ReadUInt16(Data, ref iIndex);
                Debug.Log("RealTime EventRouteTime - Global: " + iGlobalVersion + " BufferVersion: " + iEventRouteTimeVersion + " DataLength: " + Data.Length + " Index: " + iIndex);
                EventRouteTimeManager.TimeSchedules ??= [];

                if (EventRouteTimeManager.TimeSchedules.Count > 0)
                {
                    EventRouteTimeManager.TimeSchedules.Clear();
                }

                int FireBurnStartTime_Count = StorageData.ReadInt32(Data, ref iIndex);
                for (int i = 0; i < FireBurnStartTime_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", iEventRouteTimeVersion, Data, ref iIndex);

                    ushort EventRouteID = StorageData.ReadUInt16(Data, ref iIndex);

                    int length = StorageData.ReadInt32(Data, ref iIndex);

                    var eventTimeSchedule = new EventRouteTimeManager.EventTimeSchedule[length];

                    for (int j = 0; j < eventTimeSchedule.Length; j++)
                    {
                        eventTimeSchedule[j].StartHour = StorageData.ReadByte(Data, ref iIndex);
                        eventTimeSchedule[j].StartMinute = StorageData.ReadByte(Data, ref iIndex);
                        eventTimeSchedule[j].Frequency = StorageData.ReadByte(Data, ref iIndex);
                        eventTimeSchedule[j].AutoOccur = StorageData.ReadBool(Data, ref iIndex);
                    }

                    EventRouteTimeManager.TimeSchedules.Add(EventRouteID, eventTimeSchedule);

                    CheckEndTuple($"Buffer({i})", iEventRouteTimeVersion, Data, ref iIndex);
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
                    throw new Exception($"EventRouteTime Buffer start tuple not found at: {sTupleLocation}");
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
                    throw new Exception($"EventRouteTime Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

    }
}
