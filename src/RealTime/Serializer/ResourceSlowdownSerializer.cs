// GarbageSlowdownSerializer.cs

namespace RealTime.Serializer
{
    using System;
    using RealTime.Managers;
    using UnityEngine;

    public class ResourceSlowdownSerializer
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const ushort iRESOURCE_SLOWDOWN_DATA_VERSION = 1;

        public static void SaveData(FastList<byte> Data)
        {
            // Write out metadata
            StorageData.WriteUInt16(iRESOURCE_SLOWDOWN_DATA_VERSION, Data);

            // ----------- Write out GarbageAccumulator data -----------

            StorageData.WriteUInt32(uiTUPLE_START, Data);

            int GarbageAccumulatorCount = 0;

            for (int i = 0; i < ResourceSlowdownManager.GarbageAccumulator.Length; i++)
            {
                if (ResourceSlowdownManager.GarbageAccumulator[i] != 0f)
                {
                    GarbageAccumulatorCount++;
                }
            }

            StorageData.WriteInt32(GarbageAccumulatorCount, Data);

            // Write only non-zero entries (buildingID + value pairs)
            for (ushort i = 0; i < ResourceSlowdownManager.GarbageAccumulator.Length; i++)
            {
                if (ResourceSlowdownManager.GarbageAccumulator[i] != 0f)
                {
                    StorageData.WriteUInt32(uiTUPLE_START, Data);
                    StorageData.WriteUInt16(i, Data);
                    StorageData.WriteFloat(ResourceSlowdownManager.GarbageAccumulator[i], Data);
                    StorageData.WriteUInt32(uiTUPLE_END, Data);
                }
            }

            StorageData.WriteUInt32(uiTUPLE_END, Data);

            // ----------- Write out MailAccumulator data -----------

            StorageData.WriteUInt32(uiTUPLE_START, Data);

            int MailAccumulatorCount = 0;

            for (int i = 0; i < ResourceSlowdownManager.MailAccumulator.Length; i++)
            {
                if (ResourceSlowdownManager.MailAccumulator[i] != 0f)
                {
                    MailAccumulatorCount++;
                }
            }

            StorageData.WriteInt32(MailAccumulatorCount, Data);

            // Write only non-zero entries (buildingID + value pairs)
            for (ushort i = 0; i < ResourceSlowdownManager.MailAccumulator.Length; i++)
            {
                if (ResourceSlowdownManager.MailAccumulator[i] != 0f)
                {
                    StorageData.WriteUInt32(uiTUPLE_START, Data);
                    StorageData.WriteUInt16(i, Data);
                    StorageData.WriteFloat(ResourceSlowdownManager.MailAccumulator[i], Data);
                    StorageData.WriteUInt32(uiTUPLE_END, Data);
                }
            }

            StorageData.WriteUInt32(uiTUPLE_END, Data);
        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int iResourceSlowdownVersion = StorageData.ReadUInt16(Data, ref iIndex);
                Debug.Log("Global: " + iGlobalVersion + " BufferVersion: " + iResourceSlowdownVersion + " DataLength: " + Data.Length + " Index: " + iIndex);

                Array.Clear(ResourceSlowdownManager.GarbageAccumulator, 0, ResourceSlowdownManager.GarbageAccumulator.Length);
                Array.Clear(ResourceSlowdownManager.MailAccumulator, 0, ResourceSlowdownManager.MailAccumulator.Length);

                // ----------- Read GarbageAccumulator data -----------

                CheckStartTuple($"GarbageAccumulator Start", iResourceSlowdownVersion, Data, ref iIndex);

                int GarbageAccumulator_Count = StorageData.ReadInt32(Data, ref iIndex);

                for (int i = 0; i < GarbageAccumulator_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", iResourceSlowdownVersion, Data, ref iIndex);

                    ushort BuildingId = StorageData.ReadUInt16(Data, ref iIndex);
                    float value = StorageData.ReadFloat(Data, ref iIndex);

                    if (BuildingId < ResourceSlowdownManager.GarbageAccumulator.Length)
                    {
                        ResourceSlowdownManager.GarbageAccumulator[BuildingId] = value;
                    }

                    CheckEndTuple($"Buffer({i})", iResourceSlowdownVersion, Data, ref iIndex);
                }

                CheckEndTuple($"GarbageAccumulator End", iResourceSlowdownVersion, Data, ref iIndex);

                // ----------- Read MailAccumulator data -----------

                CheckStartTuple($"MailAccumulator Start", iResourceSlowdownVersion, Data, ref iIndex);

                int MailAccumulator_Count = StorageData.ReadInt32(Data, ref iIndex);

                for (int i = 0; i < MailAccumulator_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", iResourceSlowdownVersion, Data, ref iIndex);

                    ushort BuildingId = StorageData.ReadUInt16(Data, ref iIndex);
                    float value = StorageData.ReadFloat(Data, ref iIndex);

                    if (BuildingId < ResourceSlowdownManager.MailAccumulator.Length)
                    {
                        ResourceSlowdownManager.MailAccumulator[BuildingId] = value;
                    }

                    CheckEndTuple($"Buffer({i})", iResourceSlowdownVersion, Data, ref iIndex);
                }

                CheckEndTuple($"MailAccumulator End", iResourceSlowdownVersion, Data, ref iIndex);

            }
        }

        private static void CheckStartTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleStart = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleStart != uiTUPLE_START)
                {
                    throw new Exception($"ResourceSlowdown Buffer start tuple not found at: {sTupleLocation}");
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
                    throw new Exception($"ResourceSlowdown Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

    }
}
