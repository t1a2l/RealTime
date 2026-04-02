// CommercialBuildingTypesSerializer.cs

namespace RealTime.Serializer
{
    using System;
    using RealTime.CustomAI;
    using RealTime.Managers;
    using UnityEngine;

    public class CommercialBuildingTypesSerializer
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const ushort iCOMMERCIAL_BUILDING_TYPES_DATA_VERSION = 1;

        public static void SaveData(FastList<byte> Data)
        {
            // Write out metadata
            StorageData.WriteUInt16(iCOMMERCIAL_BUILDING_TYPES_DATA_VERSION, Data);
            StorageData.WriteInt32(CommercialBuildingTypesManager.CommercialBuildingTypes.Count, Data);

            // Write out each buffer settings
            foreach (var kvp in CommercialBuildingTypesManager.CommercialBuildingTypes)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write actual settings
                StorageData.WriteUInt16(kvp.Key, Data);
                StorageData.WriteByte((byte)kvp.Value, Data);

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }
        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int iCommercialBuildingTypesVersion = StorageData.ReadUInt16(Data, ref iIndex);
                Debug.Log("RealTime CommercialBuildingTypes - Global: " + iGlobalVersion + " BufferVersion: " + iCommercialBuildingTypesVersion + " DataLength: " + Data.Length + " Index: " + iIndex);
                CommercialBuildingTypesManager.CommercialBuildingTypes ??= [];

                if (CommercialBuildingTypesManager.CommercialBuildingTypes.Count > 0)
                {
                    CommercialBuildingTypesManager.CommercialBuildingTypes.Clear();
                }

                int CommercialBuildingTypes_Count = StorageData.ReadInt32(Data, ref iIndex);
                for (int i = 0; i < CommercialBuildingTypes_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", iCommercialBuildingTypesVersion, Data, ref iIndex);

                    ushort BuildingId = StorageData.ReadUInt16(Data, ref iIndex);

                    var type = (CommercialBuildingType)StorageData.ReadByte(Data, ref iIndex);

                    CommercialBuildingTypesManager.CommercialBuildingTypes.Add(BuildingId, type);

                    //if end go to next item in the manager
                    // if not end read another number and then read the end
                    uint maybeEndTuple = StorageData.ReadUInt32(Data, ref iIndex);

                    if (maybeEndTuple != uiTUPLE_END)
                    {
                        StorageData.ReadUInt32(Data, ref iIndex);

                        CheckEndTuple($"Buffer({i})", iCommercialBuildingTypesVersion, Data, ref iIndex);
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
                    throw new Exception($"CommercialBuildingTypes Buffer start tuple not found at: {sTupleLocation}");
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
                    throw new Exception($"CommercialBuildingTypes Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

    }
}
