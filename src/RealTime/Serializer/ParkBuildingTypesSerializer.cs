// CommercialBuildingTypesSerializer.cs

namespace RealTime.Serializer
{
    using System;
    using RealTime.CustomAI;
    using RealTime.Managers;
    using UnityEngine;

    public class ParkBuildingTypesSerializer
    { 
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const ushort iPARK_BUILDING_TYPES_DATA_VERSION = 1;

        public static void SaveData(FastList<byte> Data)
        {
            Debug.Log("RealTime ParkBuildingTypes OnSaveData - Start");

            // Write out metadata
            StorageData.WriteUInt16(iPARK_BUILDING_TYPES_DATA_VERSION, Data);
            StorageData.WriteInt32(ParkBuildingTypesManager.ParkBuildingTypes.Count, Data);

            // Write out each buffer settings
            foreach (var kvp in ParkBuildingTypesManager.ParkBuildingTypes)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write actual settings
                StorageData.WriteUInt16(kvp.Key, Data);
                StorageData.WriteByte((byte)kvp.Value, Data);

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }

            Debug.Log("RealTime ParkBuildingTypes OnSaveData - End");
        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int iParkBuildingTypesVersion = StorageData.ReadUInt16(Data, ref iIndex);
                Debug.Log("RealTime ParkBuildingTypes - Global: " + iGlobalVersion + " BufferVersion: " + iParkBuildingTypesVersion + " DataLength: " + Data.Length + " Index: " + iIndex);
                ParkBuildingTypesManager.ParkBuildingTypes ??= [];

                if (ParkBuildingTypesManager.ParkBuildingTypes.Count > 0)
                {
                    ParkBuildingTypesManager.ParkBuildingTypes.Clear();
                }

                int ParkBuildingTypes_Count = StorageData.ReadInt32(Data, ref iIndex);
                for (int i = 0; i < ParkBuildingTypes_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", iParkBuildingTypesVersion, Data, ref iIndex);

                    ushort BuildingId = StorageData.ReadUInt16(Data, ref iIndex);

                    var type = (ParkBuildingType)StorageData.ReadByte(Data, ref iIndex);

                    ParkBuildingTypesManager.ParkBuildingTypes.Add(BuildingId, type);

                    CheckEndTuple($"Buffer({i})", iParkBuildingTypesVersion, Data, ref iIndex);
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
                    throw new Exception($"ParkBuildingTypes Buffer start tuple not found at: {sTupleLocation}");
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
                    throw new Exception($"ParkBuildingTypes Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

    }
}
