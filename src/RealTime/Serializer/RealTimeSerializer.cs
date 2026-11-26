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

        public const ushort DataVersion = 3;
        public const string DataID = "RealTime";
        public static ushort SaveGameFileVersion;
        private const ushort SeparateCitizenScheduleVersion = 2;

        public static RealTimeSerializer instance = null;
        private ISerializableData m_serializableData = null;

        // DataSerializer.WriteByteArray throws for arrays of this size or larger.
        private const int MaxSerializableDataBytes = 16711680;

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

                int index = 0;
                SaveGameFileVersion = StorageData.ReadUInt16(data, ref index);

                Debug.Log("RealTime LoadData - DataID: " + DataID + "; Data length: " + data.Length.ToString() + "; Data Version: " + SaveGameFileVersion);

                if (SaveGameFileVersion > DataVersion)
                {
                    Debug.Log($"This saved game was saved with a newer version of RealTime. Saved game data version: {SaveGameFileVersion}. Mod data version: {DataVersion}.");
                    return;
                }

                bool hasEmbeddedCitizenSchedules = SaveGameFileVersion < SeparateCitizenScheduleVersion;

                bool isOversized = data.Length >= MaxSerializableDataBytes;

                LoadMainRealTimeData(data, ref index);

                if (isOversized && hasEmbeddedCitizenSchedules)
                {
                    // The current loader does not read the citizen schedules embedded
                    // in this legacy main key. Remove the oversized legacy block so it
                    // cannot be written back and exceed DataSerializer's 16 MB limit.
                    m_serializableData.EraseData(DataID);
                    Debug.LogWarning($"Removed oversized legacy RealTime data block ({data.Length:N0} bytes). Citizen schedules will be rebuilt.");
                }
                else if (isOversized)
                {
                    Debug.LogError($"RealTime data key '{DataID}' is {data.Length:N0} bytes, which exceeds the game's 16 MB serialized-array limit. Do not erase it automatically; investigate the current data format.");
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

                    // park building types settings
                    StorageData.WriteUInt32(uiTUPLE_START, Data);
                    ParkBuildingTypesSerializer.SaveData(Data);
                    StorageData.WriteUInt32(uiTUPLE_END, Data);

                    // bank and post office visit settings
                    StorageData.WriteUInt32(uiTUPLE_START, Data);
                    BankPostOfficeVisitSerializer.SaveData(Data);
                    StorageData.WriteUInt32(uiTUPLE_END, Data);

                    // citizen schedules
                    CitizenScheduleSerializer.SaveData(m_serializableData);
                    
                    byte[] serializedData = Data.ToArray();

                    if (serializedData.Length >= MaxSerializableDataBytes)
                    {
                        Debug.LogError(
                            $"RealTime main data key would be {serializedData.Length:N0} bytes, "
                            + "which exceeds the game's 16 MB serialized-array limit. "
                            + "Saving the main RealTime key was skipped.");

                        return;
                    }

                    m_serializableData.SaveData(DataID, serializedData);
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

        private void LoadMainRealTimeData(byte[] data, ref int index)
        {
            while (index < data.Length)
            {
                CheckStartTuple("FireBurnStartTimeSerializer", SaveGameFileVersion, data, ref index);
                FireBurnTimeSerializer.LoadData(SaveGameFileVersion, data, ref index);
                CheckEndTuple("FireBurnStartTimeSerializer", SaveGameFileVersion, data, ref index);

                if (index == data.Length)
                {
                    break;
                }

                CheckStartTuple("BuildingWorkTimeSerializer", SaveGameFileVersion, data, ref index);
                BuildingWorkTimeSerializer.LoadData(SaveGameFileVersion, data, ref index);
                CheckEndTuple("BuildingWorkTimeSerializer", SaveGameFileVersion, data, ref index);

                if (index == data.Length)
                {
                    break;
                }

                CheckStartTuple("GraduationSerializer", SaveGameFileVersion, data, ref index);
                AcademicYearSerializer.LoadData(SaveGameFileVersion, data, ref index);
                CheckEndTuple("GraduationSerializer", SaveGameFileVersion, data, ref index);

                if (index == data.Length)
                {
                    break;
                }

                CheckStartTuple("GarbageSlowdownSerializer", SaveGameFileVersion, data, ref index);
                ResourceSlowdownSerializer.LoadData(SaveGameFileVersion, data, ref index);
                CheckEndTuple("GarbageSlowdownSerializer", SaveGameFileVersion, data, ref index);

                if (index == data.Length)
                {
                    break;
                }

                CheckStartTuple("EventRouteTimeSerializer", SaveGameFileVersion, data, ref index);
                EventRouteTimeSerializer.LoadData(SaveGameFileVersion, data, ref index);
                CheckEndTuple("EventRouteTimeSerializer", SaveGameFileVersion, data, ref index);

                if (index == data.Length)
                {
                    break;
                }

                CheckStartTuple("CommercialBuildingTypesSerializer", SaveGameFileVersion, data, ref index);
                CommercialBuildingTypesSerializer.LoadData(SaveGameFileVersion, data, ref index);
                CheckEndTuple("CommercialBuildingTypesSerializer", SaveGameFileVersion, data, ref index);

                if (index == data.Length)
                {
                    break;
                }

                CheckStartTuple("ParkBuildingTypesSerializer", SaveGameFileVersion, data, ref index);
                ParkBuildingTypesSerializer.LoadData(SaveGameFileVersion, data, ref index);
                CheckEndTuple("ParkBuildingTypesSerializer", SaveGameFileVersion, data, ref index);

                if (index == data.Length)
                {
                    break;
                }

                CheckStartTuple("BankPostOfficeVisitSerializer", SaveGameFileVersion, data, ref index);
                BankPostOfficeVisitSerializer.LoadData(SaveGameFileVersion, data, ref index);
                CheckEndTuple("BankPostOfficeVisitSerializer", SaveGameFileVersion, data, ref index);
                break;
            }
        }

        public void OnReleased() => instance = null;

    }
}
