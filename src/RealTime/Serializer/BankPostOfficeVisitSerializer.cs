namespace RealTime.Serializer
{
    using System;
    using RealTime.Managers;
    using UnityEngine;

    public class BankPostOfficeVisitSerializer
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const ushort iBANK_POST_OFFICE_VISIT_DATA_VERSION = 1;

        public static void SaveData(FastList<byte> Data)
        {
            // Write out metadata
            StorageData.WriteUInt16(iBANK_POST_OFFICE_VISIT_DATA_VERSION, Data);

            // bank visit tracker is a dictionary of citizenId to BankPostOfficeVisitData, so we need to write out the count and then each item

            StorageData.WriteUInt32(uiTUPLE_START, Data);

            StorageData.WriteInt32(BankPostOfficeVisitManager.BankVisitTracker.Count, Data);

            // Write out each buffer settings
            foreach (var kvp in BankPostOfficeVisitManager.BankVisitTracker)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write actual settings
                StorageData.WriteUInt32(kvp.Key, Data);
                StorageData.WriteString(kvp.Value.Reason, Data);
                StorageData.WriteDateTime(kvp.Value.LastVisit, Data);

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }

            StorageData.WriteUInt32(uiTUPLE_END, Data);

            // post office visit tracker is a dictionary of citizenId to BankPostOfficeVisitData, so we need to write out the count and then each item

            StorageData.WriteUInt32(uiTUPLE_START, Data);

            StorageData.WriteInt32(BankPostOfficeVisitManager.PostOfficeVisitTracker.Count, Data);

            // Write out each buffer settings
            foreach (var kvp in BankPostOfficeVisitManager.PostOfficeVisitTracker)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write actual settings
                StorageData.WriteUInt32(kvp.Key, Data);
                StorageData.WriteString(kvp.Value.Reason, Data);
                StorageData.WriteDateTime(kvp.Value.LastVisit, Data);

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }

            StorageData.WriteUInt32(uiTUPLE_END, Data);
        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int iBankPostOfficeVisitVersion = StorageData.ReadUInt16(Data, ref iIndex);
                Debug.Log("Global: " + iGlobalVersion + " BufferVersion: " + iBankPostOfficeVisitVersion + " DataLength: " + Data.Length + " Index: " + iIndex);
                BankPostOfficeVisitManager.BankVisitTracker ??= [];

                if (BankPostOfficeVisitManager.BankVisitTracker.Count > 0)
                {
                    BankPostOfficeVisitManager.BankVisitTracker.Clear();
                }

                CheckStartTuple($"BankVisitTracker Start", iBankPostOfficeVisitVersion, Data, ref iIndex);

                BankPostOfficeVisitManager.PostOfficeVisitTracker ??= [];

                if (BankPostOfficeVisitManager.PostOfficeVisitTracker.Count > 0)
                {
                    BankPostOfficeVisitManager.PostOfficeVisitTracker.Clear();
                }

                int BankVisitData_Count = StorageData.ReadInt32(Data, ref iIndex);
                for (int i = 0; i < BankVisitData_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", iBankPostOfficeVisitVersion, Data, ref iIndex);

                    uint citizenId = StorageData.ReadUInt32(Data, ref iIndex);

                    string reason = StorageData.ReadString(Data, ref iIndex);
                    var lastVisit = StorageData.ReadDateTime(Data, ref iIndex);

                    BankPostOfficeVisitManager.BankVisitTracker.Add(citizenId, new BankPostOfficeVisitManager.BankVisitData
                    {
                        Reason = reason,
                        LastVisit = lastVisit
                    });

                    CheckEndTuple($"Buffer({i})", iBankPostOfficeVisitVersion, Data, ref iIndex);
                }

                CheckStartTuple($"BankVisitTracker End", iBankPostOfficeVisitVersion, Data, ref iIndex);

                // Now do the same for the post office visit tracker

                CheckStartTuple($"PostOfficeVisitTracker Start", iBankPostOfficeVisitVersion, Data, ref iIndex);

                int PostOfficeVisitData_Count = StorageData.ReadInt32(Data, ref iIndex);

                for (int i = 0; i < PostOfficeVisitData_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", iBankPostOfficeVisitVersion, Data, ref iIndex);

                    uint citizenId = StorageData.ReadUInt32(Data, ref iIndex);

                    string reason = StorageData.ReadString(Data, ref iIndex);
                    var lastVisit = StorageData.ReadDateTime(Data, ref iIndex);

                    BankPostOfficeVisitManager.PostOfficeVisitTracker.Add(citizenId, new BankPostOfficeVisitManager.PostOfficeVisitData
                    {
                        Reason = reason,
                        LastVisit = lastVisit
                    });

                    CheckEndTuple($"Buffer({i})", iBankPostOfficeVisitVersion, Data, ref iIndex);
                }

                CheckEndTuple($"PostOfficeVisitTracker End", iBankPostOfficeVisitVersion, Data, ref iIndex);

            }
        }

        private static void CheckStartTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleStart = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleStart != uiTUPLE_START)
                {
                    throw new Exception($"BankPostOfficeVisit Buffer start tuple not found at: {sTupleLocation}");
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
                    throw new Exception($"BankPostOfficeVisit Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

    }
}
