namespace RealTime.Config
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Serialization;
    using RealTime.Managers;
    using UnityEngine;

    public class BuildingWorkTimeGlobalConfig
    {
        public List<BuildingWorkTimeGlobalOld> BuildingWorkTimeGlobalSettings = [];

        public List<BuildingWorkTimeGlobal> BuildingWorkTimeGlobalSettingsV2 = [];

        private const string optionsFileName = "RealTimeOperationHoursGlobal.xml";

        public bool ShowPanel { get; set; } = false;

        private static readonly XmlSerializer Ser_ = new(typeof(BuildingWorkTimeGlobalConfig));

        public static BuildingWorkTimeGlobalConfig Config { get => field ??= Deserialize() ?? new BuildingWorkTimeGlobalConfig(); private set; }

        public static void Reset() => Config = new BuildingWorkTimeGlobalConfig();

        public int GetIndex(string infoName, string buildingAIstr)
        {
            string defaultBuildingAIstr = buildingAIstr switch
            {
                "ExtendedBankOfficeAI" => "BankOfficeAI",
                "BankOfficeAI" => "ExtendedBankOfficeAI",
                "ExtendedPostOfficeAI" => "PostOfficeAI",
                "PostOfficeAI" => "ExtendedPostOfficeAI",
                _ => ""
            };

            int index = BuildingWorkTimeGlobalSettingsV2.FindIndex(item => item.InfoName == infoName &&
            (defaultBuildingAIstr != "" ? (item.BuildingAI == buildingAIstr || item.BuildingAI == defaultBuildingAIstr) : item.BuildingAI == buildingAIstr));
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
            return index != -1 ? BuildingWorkTimeGlobalSettingsV2[index] : null;
        }

        public void SetGlobalSettings(BuildingWorkTimeGlobal buildingWorkTimeGlobal)
        {
            int index = GetIndex(buildingWorkTimeGlobal.InfoName, buildingWorkTimeGlobal.BuildingAI);
            if (index != -1)
            {
                BuildingWorkTimeGlobalSettingsV2[index] = buildingWorkTimeGlobal;
            }
        }
         
        public void CreateGlobalSettings(BuildingWorkTimeGlobal buildingWorkTimeGlobal)
        {
            int index = GetIndex(buildingWorkTimeGlobal.InfoName, buildingWorkTimeGlobal.BuildingAI);
            if (index == -1)
            {
                BuildingWorkTimeGlobalSettingsV2.Add(buildingWorkTimeGlobal);
            }
        }

        public void RemoveGlobalSettings(BuildingWorkTimeGlobal buildingWorkTimeGlobal)
        {
            int index = GetIndex(buildingWorkTimeGlobal.InfoName, buildingWorkTimeGlobal.BuildingAI);
            if (index != -1)
            {
                BuildingWorkTimeGlobalSettingsV2.RemoveAt(index);
            }
        }

        public static BuildingWorkTimeGlobalConfig Deserialize()
        {
            try
            {
                string path = GetXMLPath();
                if (File.Exists(path))
                {
                    using FileStream stream = new(path, FileMode.Open, FileAccess.Read);

                    if (Ser_.Deserialize(stream) is not BuildingWorkTimeGlobalConfig config)
                    {
                        return null;
                    }

                    if (config.BuildingWorkTimeGlobalSettings.Count > 0 && config.BuildingWorkTimeGlobalSettingsV2.Count == 0)
                    {
                        Debug.Log("RealTimeMod: Migrating legacy global work time settings.");
                        config.MigrateLegacySettings();
                        config.Serialize();
                    }
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

        private void MigrateLegacySettings()
        {
            foreach (var legacy in BuildingWorkTimeGlobalSettings)
            {
                bool openOnWeekends = legacy.WorkAtWeekands;
                bool extendedShift = legacy.HasExtendedWorkShift;
                bool continuousShift = legacy.HasContinuousWorkShift;
                int shiftCount = legacy.WorkAtNight ? 3 : Math.Max(1, legacy.WorkShifts);

                var xmlShifts = BuildingWorkTimeManager.GetShifts(extendedShift, continuousShift, shiftCount).Select(s => new WorkShiftTimeXml(s)).ToArray();


                var buildingWorkTimeGlobal = new BuildingWorkTimeGlobal
                {
                    InfoName = legacy.InfoName,
                    BuildingAI = legacy.BuildingAI,
                    IgnorePolicy = legacy.IgnorePolicy,
                    WorkDays = BuildingWorkTimeManager.GetWorkDays(openOnWeekends),
                    WorkShifts = xmlShifts
                };

                BuildingWorkTimeGlobalSettingsV2.Add(buildingWorkTimeGlobal);
            }

            // Old list no longer needed
            BuildingWorkTimeGlobalSettings.Clear();
        }

    }

    [XmlRoot("BuildingWorkTimeGlobal")]
    public class BuildingWorkTimeGlobal
    {
        [XmlAttribute("InfoName")]
        public string InfoName { get; set; }

        [XmlAttribute("BuildingAI")]
        public string BuildingAI { get; set; }

        [XmlAttribute("IgnorePolicy")]
        public bool IgnorePolicy { get; set; }

        // DayOfWeek[] as a comma-separated string attribute — simplest approach
        [XmlIgnore]
        public DayOfWeek[] WorkDays { get; set; }

        [XmlAttribute("WorkDays")]
        public string WorkDaysSerialized
        {
            get => WorkDays == null ? "" : string.Join(",", Array.ConvertAll(WorkDays, d => d.ToString()));
            set => WorkDays = string.IsNullOrEmpty(value) ? [] : Array.ConvertAll(value.Split(','), s => (DayOfWeek)Enum.Parse(typeof(DayOfWeek), s.Trim()));
        }

        // WorkShiftTime[] as child elements
        [XmlArray("WorkShifts")]
        [XmlArrayItem("Shift")]
        public WorkShiftTimeXml[] WorkShifts { get; set; }

        [XmlIgnore]
        public BuildingWorkTimeManager.WorkShiftTime[] WorkShiftTimes
        {
            get => WorkShifts?.Select(s => s.ToWorkShiftTime()).ToArray();
            set => WorkShifts = value?.Select(s => new WorkShiftTimeXml(s)).ToArray();
        }
    }


    // XML-friendly wrapper for WorkShiftTime
    public class WorkShiftTimeXml
    {
        [XmlAttribute("Start")]
        public float StartHour { get; set; }

        [XmlAttribute("End")]
        public float EndHour { get; set; }

        public WorkShiftTimeXml() { } // parameterless ctor required by XmlSerializer

        public WorkShiftTimeXml(BuildingWorkTimeManager.WorkShiftTime s)
        {
            StartHour = s.StartHour;
            EndHour = s.EndHour;
        }

        public BuildingWorkTimeManager.WorkShiftTime ToWorkShiftTime() => new() { StartHour = StartHour, EndHour = EndHour };
    }

    [XmlRoot("BuildingWorkTimeGlobal")]
    public class BuildingWorkTimeGlobalOld
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
