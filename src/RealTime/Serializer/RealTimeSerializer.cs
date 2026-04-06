// RealTimeSerializer.cs

namespace RealTime.Serializer
{
    using System;
    using ICities;
    using RealTime.Config;
    using UnityEngine;

    public class RealTimeSerializer : ISerializableDataExtension
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        public const ushort DataVersion = 2;
        public const string DataID = "RealTime";
        public static ushort SaveGameFileVersion;
        private const ushort SeparateCitizenScheduleVersion = 2;

        public static RealTimeSerializer instance = null;
        private ISerializableData m_serializableData = null;

        public void OnCreated(ISerializableData serializedData)
        {
            instance = this;
            m_serializableData = serializedData;
        }

        public void OnLoadData()
        {
            try
            {
                if (m_serializableData == null)
                {
                    Debug.Log("m_serializableData is null");
                    return;
                }

                byte[] data = m_serializableData.LoadData(DataID);

                if (data == null || data.Length == 0)
                {
                    Debug.Log("Data is null");

                    return;
                }

                int Index = 0;
                SaveGameFileVersion = StorageData.ReadUInt16(data, ref Index);

                Debug.Log("RealTime LoadData - DataID: " + DataID + "; Data length: " + data.Length.ToString() + "; Data Version: " + SaveGameFileVersion);

                if (SaveGameFileVersion > DataVersion)
                {
                    string sMessage = "This saved game was saved with a newer version of RealTime.\r\n";
                    sMessage += "\r\n";
                    sMessage += "Unable to load settings.\r\n";
                    sMessage += "\r\n";
                    sMessage += "Saved game data version: " + SaveGameFileVersion + "\r\n";
                    sMessage += "MOD data version: " + DataVersion + "\r\n";
                    Debug.Log(sMessage);
                    return;
                }


                while (Index < data.Length)
                {
                    CheckStartTuple("FireBurnStartTimeSerializer", SaveGameFileVersion, data, ref Index);
                    FireBurnTimeSerializer.LoadData(SaveGameFileVersion, data, ref Index);
                    CheckEndTuple("FireBurnStartTimeSerializer", SaveGameFileVersion, data, ref Index);

                    if (Index == data.Length)
                    {
                        break;
                    }

                    CheckStartTuple("BuildingWorkTimeSerializer", SaveGameFileVersion, data, ref Index);
                    BuildingWorkTimeSerializer.LoadData(SaveGameFileVersion, data, ref Index);
                    CheckEndTuple("BuildingWorkTimeSerializer", SaveGameFileVersion, data, ref Index);

                    if (Index == data.Length)
                    {
                        break;
                    }

                    CheckStartTuple("GraduationSerializer", SaveGameFileVersion, data, ref Index);
                    AcademicYearSerializer.LoadData(SaveGameFileVersion, data, ref Index);
                    CheckEndTuple("GraduationSerializer", SaveGameFileVersion, data, ref Index);

                    if (Index == data.Length)
                    {
                        break;
                    }

                    CheckStartTuple("GarbageSlowdownSerializer", SaveGameFileVersion, data, ref Index);
                    ResourceSlowdownSerializer.LoadData(SaveGameFileVersion, data, ref Index);
                    CheckEndTuple("GarbageSlowdownSerializer", SaveGameFileVersion, data, ref Index);

                    if (Index == data.Length)
                    {
                        break;
                    }

                    CheckStartTuple("EventRouteTimeSerializer", SaveGameFileVersion, data, ref Index);
                    EventRouteTimeSerializer.LoadData(SaveGameFileVersion, data, ref Index);
                    CheckEndTuple("EventRouteTimeSerializer", SaveGameFileVersion, data, ref Index);

                    if (Index == data.Length)
                    {
                        break;
                    }

                    CheckStartTuple("CommercialBuildingTypesSerializer", SaveGameFileVersion, data, ref Index);
                    CommercialBuildingTypesSerializer.LoadData(SaveGameFileVersion, data, ref Index);
                    CheckEndTuple("CommercialBuildingTypesSerializer", SaveGameFileVersion, data, ref Index);
                    break;
                }

                if (SaveGameFileVersion >= SeparateCitizenScheduleVersion)
                {
                    CitizenScheduleSerializer.LoadData(m_serializableData);
                }
            }
            catch (Exception ex)
            {
                string sErrorMessage = "Loading of RealTime save game settings failed with the following error:\r\n";
                sErrorMessage += "\r\n";
                sErrorMessage += ex.Message;
                Debug.LogError(sErrorMessage);
            }
        }

        public void OnSaveData()
        {
            Debug.Log("RealTime OnSaveData - Start, Data Version: " + DataVersion);
            try
            {
                if (m_serializableData != null)
                {
                    var Data = new FastList<byte>();
                    // Always write out data version first
                    StorageData.WriteUInt16(DataVersion, Data);

                    // buildings fire burn settings
                    StorageData.WriteUInt32(uiTUPLE_START, Data);
                    FireBurnTimeSerializer.SaveData(Data);
                    StorageData.WriteUInt32(uiTUPLE_END, Data);

                    // buildings work time settings
                    StorageData.WriteUInt32(uiTUPLE_START, Data);
                    BuildingWorkTimeSerializer.SaveData(Data);
                    StorageData.WriteUInt32(uiTUPLE_END, Data);

                    // academic year data settings
                    StorageData.WriteUInt32(uiTUPLE_START, Data);
                    AcademicYearSerializer.SaveData(Data);
                    StorageData.WriteUInt32(uiTUPLE_END, Data);

                    // garbage slowndown settings
                    StorageData.WriteUInt32(uiTUPLE_START, Data);
                    ResourceSlowdownSerializer.SaveData(Data);
                    StorageData.WriteUInt32(uiTUPLE_END, Data);

                    // event route time settings
                    StorageData.WriteUInt32(uiTUPLE_START, Data);
                    EventRouteTimeSerializer.SaveData(Data);
                    StorageData.WriteUInt32(uiTUPLE_END, Data);

                    // commercial building types settings
                    StorageData.WriteUInt32(uiTUPLE_START, Data);
                    CommercialBuildingTypesSerializer.SaveData(Data);
                    StorageData.WriteUInt32(uiTUPLE_END, Data);

                    // citizen schedules
                    CitizenScheduleSerializer.SaveData(m_serializableData);

                    BuildingWorkTimeGlobalConfig.Config.Serialize();

                    m_serializableData.SaveData(DataID, Data.ToArray());
                }
            }
            catch (Exception ex)
            {
                Debug.Log("RealTime Could not save data. " + ex.Message);
            }
            Debug.Log("RealTime OnSaveData - Finish");
        }

        private void CheckStartTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleStart = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleStart != uiTUPLE_START)
                {
                    throw new Exception($"RealTime Start tuple not found at: {sTupleLocation}");
                }
            }
        }

        private void CheckEndTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleEnd = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleEnd != uiTUPLE_END)
                {
                    throw new Exception($"RealTime End tuple not found at: {sTupleLocation}");
                }
            }
        }

        public void OnReleased() => instance = null;

    }
}
