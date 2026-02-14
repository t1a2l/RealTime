// AcademicYearSerializer.cs

namespace RealTime.Serializer
{
    using System;
    using RealTime.Managers;
    using UnityEngine;

    public class AcademicYearSerializer
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const ushort iACADEMIC_YEAR_DATA_VERSION = 1;

        public static void SaveData(FastList<byte> Data)
        {
            // Write out metadata
            StorageData.WriteUInt16(iACADEMIC_YEAR_DATA_VERSION, Data);

            StorageData.WriteInt32(AcademicYearManager.MainCampusBuildingsList.Count, Data);

            // Write out each buffer settings
            foreach (var kvp in AcademicYearManager.MainCampusBuildingsList)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write actual settings
                StorageData.WriteUInt16(kvp.Key, Data);
                StorageData.WriteBool(kvp.Value.DidLastYearEnd, Data);
                StorageData.WriteBool(kvp.Value.DidGraduationStart, Data);
                StorageData.WriteFloat(kvp.Value.GraduationStartTime, Data);
                StorageData.WriteUInt32(kvp.Value.ActualAcademicYearEndFrame, Data);
                StorageData.WriteBool(kvp.Value.IsFirstAcademicYear, Data);

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }
        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int iAcademicYearVersion = StorageData.ReadUInt16(Data, ref iIndex);
                Debug.Log("Global: " + iGlobalVersion + " BufferVersion: " + iAcademicYearVersion + " DataLength: " + Data.Length + " Index: " + iIndex);

                if (AcademicYearManager.MainCampusBuildingsList.Count > 0)
                {
                    AcademicYearManager.MainCampusBuildingsList.Clear();
                }

                int MainCampusBuildingsList_Count = StorageData.ReadInt32(Data, ref iIndex);
                for (int i = 0; i < MainCampusBuildingsList_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", iAcademicYearVersion, Data, ref iIndex);

                    ushort BuildingId = StorageData.ReadUInt16(Data, ref iIndex);

                    bool DidLastYearEnd = StorageData.ReadBool(Data, ref iIndex);
                    bool DidGraduationStart = StorageData.ReadBool(Data, ref iIndex);
                    float GraduationStartTime = StorageData.ReadFloat(Data, ref iIndex);
                    uint ActualAcademicYearEndFrame = StorageData.ReadUInt32(Data, ref iIndex);
                    bool IsFirstAcademicYear = StorageData.ReadBool(Data, ref iIndex);

                    var academicYearData = new AcademicYearManager.AcademicYearData()
                    {
                        DidLastYearEnd = DidLastYearEnd,
                        DidGraduationStart = DidGraduationStart,
                        GraduationStartTime = GraduationStartTime,
                        ActualAcademicYearEndFrame = ActualAcademicYearEndFrame,
                        IsFirstAcademicYear = IsFirstAcademicYear
                    };

                    AcademicYearManager.MainCampusBuildingsList.Add(BuildingId, academicYearData);

                    CheckEndTuple($"Buffer({i})", iAcademicYearVersion, Data, ref iIndex);
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
                    throw new Exception($"AcademicYearData Buffer start tuple not found at: {sTupleLocation}");
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
                    throw new Exception($"AcademicYearData Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

    }
}
