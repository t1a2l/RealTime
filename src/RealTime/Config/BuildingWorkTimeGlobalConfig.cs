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
                Serialize();
            }
        }
         
        public void CreateGlobalSettings(BuildingWorkTimeGlobal buildingWorkTimeGlobal)
        {
            int index = GetIndex(buildingWorkTimeGlobal.InfoName, buildingWorkTimeGlobal.BuildingAI);
            if (index == -1)
            {
                BuildingWorkTimeGlobalSettingsV2.Add(buildingWorkTimeGlobal);
                Serialize();
            }
        }

        public void RemoveGlobalSettings(BuildingWorkTimeGlobal buildingWorkTimeGlobal)
        {
            int index = GetIndex(buildingWorkTimeGlobal.InfoName, buildingWorkTimeGlobal.BuildingAI);
            if (index != -1)
            {
                BuildingWorkTimeGlobalSettingsV2.RemoveAt(index);
                Serialize();
            }
        }

        public static BuildingWorkTimeGlobalConfig Deserialize()
        {
            try
            {
                string path = GetXMLPath();

                if (!File.Exists(path))
                {
                    return null;
                }

                using FileStream stream = new(path, FileMode.Open, FileAccess.Read);

                if (Ser_.Deserialize(stream) is not BuildingWorkTimeGlobalConfig config)
                {
                    return null;
                }

                if (config.BuildingWorkTimeGlobalSettings.Count > 0)
                {
                    Debug.Log("RealTimeMod: Migrating legacy global work time settings.");
                    config.MigrateLegacySettings();
                    config.Serialize();
                }

                return config;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RealTime] Failed to load global work-time settings: {ex}");
                return null;
            }
        }

        public void Serialize()
        {
            string path = GetXMLPath();
            string tempPath = path + ".tmp";

            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                {
                    Ser_.Serialize(stream, this);
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(tempPath, path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RealTime] Failed to save global work-time settings: {ex}");

                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        public static string GetXMLPath()
        {
            string LocalDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string CO_path = Path.Combine(LocalDataPath, "Colossal Order");
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


                var migrated = new BuildingWorkTimeGlobal
                {
                    InfoName = legacy.InfoName,
                    BuildingAI = legacy.BuildingAI,
                    WorkDays = BuildingWorkTimeManager.GetWorkDays(openOnWeekends),
                    WorkShifts = xmlShifts
                };

                if (GetIndex(migrated.InfoName, migrated.BuildingAI) == -1)
                {
                    BuildingWorkTimeGlobalSettingsV2.Add(migrated);
                }

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
        [XmlAttribute("StartTime")]
        public float StartTime { get; set; }

        [XmlAttribute("EndTime")]
        public float EndTime { get; set; }

        public WorkShiftTimeXml() { } // parameterless ctor required by XmlSerializer

        public WorkShiftTimeXml(BuildingWorkTimeManager.WorkShiftTime s)
        {
            StartTime = s.StartTime;
            EndTime = s.EndTime;
        }

        public BuildingWorkTimeManager.WorkShiftTime ToWorkShiftTime() => new() { StartTime = StartTime, EndTime = EndTime };
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
