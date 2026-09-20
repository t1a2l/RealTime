namespace RealTime.Managers
{
    using System;
    using System.Collections.Generic;

    public static class BankPostOfficeVisitManager
    {
        public static Dictionary<uint, BankVisitData> BankVisitTracker;

        public static Dictionary<uint, PostOfficeVisitData> PostOfficeVisitTracker;

        public struct BankVisitData
        {
            public string Reason;
            public DateTime LastVisit;
        }

        public struct PostOfficeVisitData
        {
            public string Reason;
            public DateTime LastVisit;
        }

        public static string[] postOfficeReasons =
        [
            "Getting a package from a relative",
            "Sending a letter to the tax office",
            "Picking up an online order",
            "Mailing a birthday gift",
            "Renewing a PO Box subscription",
            "Shipping a package",
            "Buying a book of stamps",
            "Sending a postcard to a friend"
        ];

        public static string[] bankReasons =
        [
            "Depositing a paycheck",
            "Withdrawing cash",
            "Applying for a small business loan",
            "Opening a new savings account",
            "Consulting with a financial advisor",
            "Exchanging foreign currency",
            "Updating a mortgage agreement",
            "Replacing a lost credit card"
        ];

        public static void Init()
        {
            BankVisitTracker ??= [];
            PostOfficeVisitTracker ??= [];
        }

        public static void Deinit()
        {
            BankVisitTracker = [];
            PostOfficeVisitTracker = [];
        }

        public static bool CitizenBankVisitDataExist(uint citizenID) => BankVisitTracker.ContainsKey(citizenID);

        public static bool CitizenPostOfficeVisitDataExist(uint citizenID) => PostOfficeVisitTracker.ContainsKey(citizenID);

        public static BankVisitData GetCitizenBankVisitData(uint citizenID) => !BankVisitTracker.TryGetValue(citizenID, out var bankVisitData) ? default : bankVisitData;

        public static PostOfficeVisitData GetCitizenPostOfficeVisitData(uint citizenID) => !PostOfficeVisitTracker.TryGetValue(citizenID, out var postOfficeVisitData) ? default : postOfficeVisitData;

        public static void CreateBankVisitData(uint citizenID, DateTime visitTime)
        {
            if (!BankVisitTracker.ContainsKey(citizenID))
            {
                int index = SimulationManager.instance.m_randomizer.Int32((uint)bankReasons.Length);
                BankVisitTracker[citizenID] = new BankVisitData
                {
                    Reason = bankReasons[index],
                    LastVisit = visitTime
                };
            }
        }

        public static void CreatePostOfficeVisitData(uint citizenID, DateTime visitTime)
        {
            if (!PostOfficeVisitTracker.ContainsKey(citizenID))
            {
                int index = SimulationManager.instance.m_randomizer.Int32((uint)postOfficeReasons.Length);
                PostOfficeVisitTracker[citizenID] = new PostOfficeVisitData
                {
                    Reason = postOfficeReasons[index],
                    LastVisit = visitTime
                };
            }
        }

        public static void SetBankVisitData(uint citizenID, DateTime visitTime)
        {
            if (BankVisitTracker.ContainsKey(citizenID))
            {
                var bankVisitData = BankVisitTracker[citizenID];

                int index = SimulationManager.instance.m_randomizer.Int32((uint)bankReasons.Length);

                bankVisitData.Reason = bankReasons[index];
                bankVisitData.LastVisit = visitTime;

                BankVisitTracker[citizenID] = bankVisitData;
            }
        }

        public static void SetPostOfficeVisitData(uint citizenID, DateTime visitTime)
        {
            if (PostOfficeVisitTracker.ContainsKey(citizenID))
            {
                var postOfficeVisitData = PostOfficeVisitTracker[citizenID];

                int index = SimulationManager.instance.m_randomizer.Int32((uint)postOfficeReasons.Length);

                postOfficeVisitData.Reason = postOfficeReasons[index];
                postOfficeVisitData.LastVisit = visitTime;

                PostOfficeVisitTracker[citizenID] = postOfficeVisitData;
            }
        }

        public static void RemoveBankVisitData(uint citizenID) => BankVisitTracker.Remove(citizenID);

        public static void RemovePostOfficeVisitData(uint citizenID) => PostOfficeVisitTracker.Remove(citizenID);
    }
}
