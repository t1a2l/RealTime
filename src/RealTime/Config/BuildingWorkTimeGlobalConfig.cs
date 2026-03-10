namespace RealTime.Config
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;
    using UnityEngine;

    public class BuildingWorkTimeGlobalConfig
    {
        public List<BuildingWorkTimeGlobal> BuildingWorkTimeGlobalSettings = [];

        private const string optionsFileName = "RealTimeOperationHoursGlobal.xml";

        public bool ShowPanel { get; set; } = false;

        private static XmlSerializer Ser_ => new(typeof(BuildingWorkTimeGlobalConfig));

        public static BuildingWorkTimeGlobalConfig Config { get => field ??= Deserialize() ?? new BuildingWorkTimeGlobalConfig(); private set; }

        public static void Reset() => Config = new BuildingWorkTimeGlobalConfig();

        public int GetIndex(string infoName, string buildingAIstr)
        {
            string defaultBuildingAIstr = "";
            if (buildingAIstr == "ExtendedBankOfficeAI")
            {
                defaultBuildingAIstr = "BankOfficeAI";
            }
            else if (buildingAIstr == "BankOfficeAI")
            {
                defaultBuildingAIstr = "ExtendedBankOfficeAI";
            }
            else if (buildingAIstr == "ExtendedPostOfficeAI")
            {
                defaultBuildingAIstr = "PostOfficeAI";
            }
            else if (buildingAIstr == "PostOfficeAI")
            {
                defaultBuildingAIstr = "ExtendedPostOfficeAI";
            }
            int index = BuildingWorkTimeGlobalSettings.FindIndex(item => item.InfoName == infoName &&
            defaultBuildingAIstr != "" ? (item.BuildingAI == buildingAIstr || item.BuildingAI == defaultBuildingAIstr) : item.BuildingAI == buildingAIstr);
            return index;
        }

        public bool GlobalSettingsExist(BuildingInfo buildingInfo)
        {
            string BuildingAIstr = buildingInfo.GetAI().GetType().Name;
            int index = GetIndex(buildingInfo.name, BuildingAIstr);
            return index != -1;
        }

        public BuildingWorkTimeGlobal GetGlobalSettings(BuildingInfo buildingInfo)
        {
            string BuildingAIstr = buildingInfo.GetAI().GetType().Name;
            int index = GetIndex(buildingInfo.name, BuildingAIstr);
            return index != -1 ? BuildingWorkTimeGlobalSettings[index] : null;
        }

        public void SetGlobalSettings(BuildingWorkTimeGlobal buildingWorkTimeGlobal)
        {
            int index = GetIndex(buildingWorkTimeGlobal.InfoName, buildingWorkTimeGlobal.BuildingAI);
            if (index != -1)
            {
                BuildingWorkTimeGlobalSettings[index] = buildingWorkTimeGlobal;
            }
        }
         
        public void CreateGlobalSettings(BuildingWorkTimeGlobal buildingWorkTimeGlobal)
        {
            int index = GetIndex(buildingWorkTimeGlobal.InfoName, buildingWorkTimeGlobal.BuildingAI);
            if (index == -1)
            {
                BuildingWorkTimeGlobalSettings.Add(buildingWorkTimeGlobal);
            }
        }

        public void RemoveGlobalSettings(BuildingWorkTimeGlobal buildingWorkTimeGlobal)
        {
            int index = GetIndex(buildingWorkTimeGlobal.InfoName, buildingWorkTimeGlobal.BuildingAI);
            if (index != -1)
            {
                BuildingWorkTimeGlobalSettings.RemoveAt(index);
            }
        }

        public static BuildingWorkTimeGlobalConfig Deserialize()
        {
            try
            {
                if (File.Exists(GetXMLPath()))
                {
                    using FileStream stream = new(GetXMLPath(), FileMode.Open, FileAccess.Read);
                    return Ser_.Deserialize(stream) as BuildingWorkTimeGlobalConfig;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("RealTimeMod: " + ex.Message);
            }
            return null;
        }

        public void Serialize()
        {
            try
            {
                using var stream = new FileStream(GetXMLPath(), FileMode.Create, FileAccess.Write);
                Ser_.Serialize(stream, this);
            }
            catch (Exception ex)
            {
                Debug.LogError("RealTimeMod: " + ex.Message);
            }
        }

        public static string GetXMLPath()
        {
            string fileName = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string CO_path = Path.Combine(fileName, "Colossal Order");
            string CS_path = Path.Combine(CO_path, "Cities_Skylines");
            string file_path = Path.Combine(CS_path, optionsFileName);
            return file_path;
        }

    }

    [XmlRoot("BuildingWorkTimeGlobal")]
    public class BuildingWorkTimeGlobal
    {
        [XmlAttribute("InfoName")]
        public string InfoName { get; set; }

        [XmlAttribute("BuildingAI")]
        public string BuildingAI { get; set; }

        [XmlAttribute("WorkAtNight")]
        public bool WorkAtNight { get; set; }

        [XmlAttribute("WorkAtWeekands")]
        public bool WorkAtWeekands { get; set; }

        [XmlAttribute("HasExtendedWorkShift")]
        public bool HasExtendedWorkShift { get; set; }

        [XmlAttribute("HasContinuousWorkShift")]
        public bool HasContinuousWorkShift { get; set; }

        [XmlAttribute("IgnorePolicy")]
        public bool IgnorePolicy { get; set; }

        [XmlAttribute("WorkShifts")]
        public int WorkShifts { get; set; }
    }
}
