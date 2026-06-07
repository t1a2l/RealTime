namespace RealTime.CustomAI
{
    using System.Collections.Generic;
    using ColossalFramework;
    using RealTime.Config;
    using RealTime.Managers;

    internal static class UpdateBuildingSettings
    {
        internal static void ChangeBuildingLockStatus(ushort buildingID, bool LockStatus)
        {
            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            buildingWorkTime.IsLocked = LockStatus;

            BuildingWorkTimeManager.SetBuildingWorkTime(buildingID, buildingWorkTime);
        }

        internal static void SaveNewSettings(ushort buildingID, BuildingWorkTimeManager.WorkTime workTime)
        {
            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            buildingWorkTime.WorkDays = workTime.WorkDays;
            buildingWorkTime.WorkShifts = workTime.WorkShifts;
            buildingWorkTime.IgnorePolicy = workTime.IgnorePolicy;
            buildingWorkTime.IsDefault = false;
            buildingWorkTime.IsPrefab = false;
            buildingWorkTime.IsGlobal = false;

            BuildingWorkTimeManager.SetBuildingWorkTime(buildingID, buildingWorkTime);
        }

        internal static void SetBuildingToPrefab(ushort buildingID, BuildingWorkTimeManager.WorkTimePrefab workTimePrefab)
        {
            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            buildingWorkTime.WorkDays = workTimePrefab.WorkDays;
            buildingWorkTime.WorkShifts = workTimePrefab.WorkShifts;
            buildingWorkTime.IgnorePolicy = workTimePrefab.IgnorePolicy;
            buildingWorkTime.IsDefault = false;
            buildingWorkTime.IsPrefab = true;
            buildingWorkTime.IsGlobal = false;

            BuildingWorkTimeManager.SetBuildingWorkTime(buildingID, buildingWorkTime);
        }

        internal static void SetBuildingToGlobal(ushort buildingID, BuildingWorkTimeGlobal buildingWorkTimeGlobalConfig)
        {
            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            buildingWorkTime.WorkDays = buildingWorkTimeGlobalConfig.WorkDays;
            buildingWorkTime.WorkShifts = ConvertXMLToWorkShifts(buildingWorkTimeGlobalConfig.WorkShifts);
            buildingWorkTime.IgnorePolicy = buildingWorkTimeGlobalConfig.IgnorePolicy;
            buildingWorkTime.IsDefault = false;
            buildingWorkTime.IsPrefab = false;
            buildingWorkTime.IsGlobal = true;

            BuildingWorkTimeManager.SetBuildingWorkTime(buildingID, buildingWorkTime);
        }

        internal static void UpdateBuildingToDefaultSettings(ushort buildingID, BuildingWorkTimeManager.WorkTime newDefaultWorkTime)
        {
            var buildingWorkTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingID);

            buildingWorkTime.WorkDays = newDefaultWorkTime.WorkDays;
            buildingWorkTime.WorkShifts = newDefaultWorkTime.WorkShifts;
            buildingWorkTime.IgnorePolicy = newDefaultWorkTime.IgnorePolicy;
            buildingWorkTime.IsDefault = true;
            buildingWorkTime.IsPrefab = false;
            buildingWorkTime.IsGlobal = false;

            BuildingWorkTimeManager.SetBuildingWorkTime(buildingID, buildingWorkTime);
        }

        internal static void CreatePrefabSettings(ushort buildingID, BuildingWorkTimeManager.WorkTime newWorkTime)
        {
            var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;
            string BuildingAIstr = buildingInfo.GetAI().GetType().Name;
            string defaultBuildingAIstr = BuildingAIstr switch
            {
                "ExtendedBankOfficeAI" => "BankOfficeAI",
                "BankOfficeAI" => "ExtendedBankOfficeAI",
                "ExtendedPostOfficeAI" => "PostOfficeAI",
                "PostOfficeAI" => "ExtendedPostOfficeAI",
                _ => ""
            };
            var buildingsIdsList = new List<ushort>();

            foreach (var item in BuildingWorkTimeManager.BuildingsWorkTime)
            {
                var Info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[item.Key].Info;
                string buildingAIName = Info.GetAI().GetType().Name;
                if(Info.name == buildingInfo.name && !item.Value.IsLocked)
                {
                    if(defaultBuildingAIstr != "")
                    {
                        if(defaultBuildingAIstr == buildingAIName || BuildingAIstr == buildingAIName)
                        {
                            buildingsIdsList.Add(item.Key);
                        }
                    }
                    else if(BuildingAIstr == buildingAIName)
                    {
                        buildingsIdsList.Add(item.Key);
                    }
                }
            }

            // set new prefab settings according to the building current settings
            var buildingWorkTimePrefab = new BuildingWorkTimeManager.WorkTimePrefab
            {
                InfoName = buildingInfo.name,
                BuildingAI = BuildingAIstr,
                IgnorePolicy = newWorkTime.IgnorePolicy,
                WorkDays = newWorkTime.WorkDays,
                WorkShifts = newWorkTime.WorkShifts
            };

            foreach (ushort buildingId in buildingsIdsList)
            {
                var workTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingId);
                
                workTime.WorkDays = buildingWorkTimePrefab.WorkDays;
                workTime.WorkShifts = buildingWorkTimePrefab.WorkShifts;
                workTime.IgnorePolicy = buildingWorkTimePrefab.IgnorePolicy;
                workTime.IsDefault = false;
                workTime.IsPrefab = true;
                workTime.IsGlobal = false;

                BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
            }

            if (BuildingWorkTimeManager.PrefabExist(buildingInfo))
            {
                // update the prefab
                var prefabRecord = BuildingWorkTimeManager.GetPrefab(buildingInfo);

                prefabRecord.IgnorePolicy = buildingWorkTimePrefab.IgnorePolicy;
                prefabRecord.WorkDays = buildingWorkTimePrefab.WorkDays;
                prefabRecord.WorkShifts = buildingWorkTimePrefab.WorkShifts;

                BuildingWorkTimeManager.SetPrefab(prefabRecord);
            }
            else
            {
                // create new prefab
                BuildingWorkTimeManager.CreatePrefab(buildingWorkTimePrefab);
            }
        }

        internal static void CreateGlobalSettings(ushort buildingID, BuildingWorkTimeManager.WorkTime newWorkTime)
        {
            var buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info;
            string BuildingAIstr = buildingInfo.GetAI().GetType().Name;
            string defaultBuildingAIstr = BuildingAIstr switch
            {
                "ExtendedBankOfficeAI" => "BankOfficeAI",
                "BankOfficeAI" => "ExtendedBankOfficeAI",
                "ExtendedPostOfficeAI" => "PostOfficeAI",
                "PostOfficeAI" => "ExtendedPostOfficeAI",
                _ => ""
            };
            var buildingsIdsList = new List<ushort>();

            foreach (var item in BuildingWorkTimeManager.BuildingsWorkTime)
            {
                var Info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[item.Key].Info;
                string buildingAIName = Info.GetAI().GetType().Name;
                if (Info.name == buildingInfo.name && !item.Value.IsLocked)
                {
                    if (defaultBuildingAIstr != "")
                    {
                        if (defaultBuildingAIstr == buildingAIName || BuildingAIstr == buildingAIName)
                        {
                            buildingsIdsList.Add(item.Key);
                        }
                    }
                    else if (BuildingAIstr == buildingAIName)
                    {
                        buildingsIdsList.Add(item.Key);
                    }
                }
            }

            // set new global settings according to the building current settings
            var buildingWorkTimeGlobal = new BuildingWorkTimeGlobal
            {
                InfoName = buildingInfo.name,
                BuildingAI = BuildingAIstr,
                IgnorePolicy = newWorkTime.IgnorePolicy,
                WorkDays = newWorkTime.WorkDays,
                WorkShifts = ConvertWorkShiftsToXML(newWorkTime.WorkShifts)
            };

            foreach (ushort buildingId in buildingsIdsList)
            {
                var workTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingId);

                workTime.WorkShifts = ConvertXMLToWorkShifts(buildingWorkTimeGlobal.WorkShifts);
                workTime.WorkDays = buildingWorkTimeGlobal.WorkDays;
                workTime.IgnorePolicy = buildingWorkTimeGlobal.IgnorePolicy;
                workTime.IsDefault = false;
                workTime.IsPrefab = false;
                workTime.IsGlobal = true;

                BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
            }

            if (BuildingWorkTimeGlobalConfig.Config.GlobalSettingsExist(buildingInfo))
            {
                var globalRecord = BuildingWorkTimeGlobalConfig.Config.GetGlobalSettings(buildingInfo);

                globalRecord.IgnorePolicy = buildingWorkTimeGlobal.IgnorePolicy;
                globalRecord.WorkDays = buildingWorkTimeGlobal.WorkDays;
                globalRecord.WorkShifts = buildingWorkTimeGlobal.WorkShifts;

                BuildingWorkTimeGlobalConfig.Config.SetGlobalSettings(globalRecord);
            }
            else
            {
                BuildingWorkTimeGlobalConfig.Config.CreateGlobalSettings(buildingWorkTimeGlobal);
            }
        }

        internal static BuildingWorkTimeManager.WorkShiftTime[] ConvertXMLToWorkShifts(WorkShiftTimeXml[] xmlShifts)
        {
            if (xmlShifts == null || xmlShifts.Length == 0)
            {
                return [];
            }

            var result = new BuildingWorkTimeManager.WorkShiftTime[xmlShifts.Length];

            for (int i = 0; i < xmlShifts.Length; i++)
            {
                result[i] = new BuildingWorkTimeManager.WorkShiftTime
                {
                    StartTime = xmlShifts[i].StartTime,
                    EndTime = xmlShifts[i].EndTime
                };
            }

            return result;
        }

        internal static WorkShiftTimeXml[] ConvertWorkShiftsToXML(BuildingWorkTimeManager.WorkShiftTime[] shifts)
        {
            if (shifts == null || shifts.Length == 0)
            {
                return [];
            }

            var result = new WorkShiftTimeXml[shifts.Length];

            for (int i = 0; i < shifts.Length; i++)
            {
                result[i] = new WorkShiftTimeXml    
                {
                    StartTime = shifts[i].StartTime,
                    EndTime = shifts[i].EndTime
                };
            }

            return result;
        }

    }
}
