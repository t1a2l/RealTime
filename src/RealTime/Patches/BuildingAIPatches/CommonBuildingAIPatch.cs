namespace RealTime.Patches.BuildingAIPatches
{
    using System;
    using System.Runtime.CompilerServices;
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.CustomAI;
    using RealTime.GameConnection;
    using RealTime.Managers;
    using UnityEngine;

    [HarmonyPatch]
    internal class CommonBuildingAIPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        [HarmonyPatch(typeof(CommonBuildingAI), "GetColor")]
        [HarmonyPostfix]
        public static void GetColor(ushort buildingID, ref Building data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode, ref Color __result)
        {
            var negativeColor = InfoManager.instance.m_properties.m_modeProperties[(int)InfoManager.InfoMode.TrafficRoutes].m_negativeColor;
            var targetColor = InfoManager.instance.m_properties.m_modeProperties[(int)InfoManager.InfoMode.TrafficRoutes].m_targetColor;
            switch (infoMode)
            {
                case InfoManager.InfoMode.TrafficRoutes:
                    float f = 0;
                    if (RealTimeBuildingAI != null)
                    {
                        f = RealTimeBuildingAI.GetBuildingReachingTroubleFactor(buildingID);
                    }
                    __result = Color.Lerp(negativeColor, targetColor, f);
                    return;

                case InfoManager.InfoMode.Wind:
                    if (RealTimeBuildingAI != null)
                    {
                        __result = RealTimeBuildingAI.IsBuildingWorking(buildingID) ? Color.green : Color.red;
                    }
                    return;

                case InfoManager.InfoMode.NaturalResources:
                    if (RealTimeBuildingAI != null && !RealTimeBuildingAI.IsBuildingWorking(buildingID))
                    {
                        var instance = Singleton<CitizenManager>.instance;
                        var instance1 = Singleton<BuildingManager>.instance;
                        uint units = instance1.m_buildings.m_buffer[buildingID].m_citizenUnits;
                        int num = 0;
                        while (units != 0)
                        {
                            uint nextUnit = instance.m_units.m_buffer[units].m_nextUnit;
                            for (int i = 0; i < 5; i++)
                            {
                                uint citizenId = instance.m_units.m_buffer[units].GetCitizen(i);
                                var citizen = instance.m_citizens.m_buffer[citizenId];
                                if (citizenId != 0U && citizen.CurrentLocation != Citizen.Location.Moving && citizen.GetBuildingByLocation() == buildingID)
                                {
                                    __result = Color.blue;
                                    return;
                                }
                            }
                            units = nextUnit;
                            if (++num > 524288)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                        __result = Singleton<InfoManager>.instance.m_properties.m_neutralColor;
                    }
                    return;

                case InfoManager.InfoMode.None:
                    if (RealTimeBuildingAI != null && RealTimeBuildingAI.ShouldSwitchBuildingLightsOff(buildingID))
                    {
                        __result.a = 0;
                    }
                    return;
            }
        }

        [HarmonyPatch(typeof(CommonBuildingAI), "EmptyBuilding")]
        [HarmonyPrefix]
        public static bool EmptyBuilding(CommonBuildingAI __instance, ushort buildingID, ref Building data, CitizenUnit.Flags flags, bool onlyMoving)
        {
            if (data.m_fireIntensity != 0)
            {
                var instance = Singleton<CitizenManager>.instance;
                uint num = data.m_citizenUnits;
                int num2 = 0;
                while (num != 0)
                {
                    if ((instance.m_units.m_buffer[num].m_flags & flags) != 0)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            uint citizen = instance.m_units.m_buffer[num].GetCitizen(i);
                            if (citizen == 0)
                            {
                                continue;
                            }
                            ushort instance2 = instance.m_citizens.m_buffer[citizen].m_instance;
                            if ((onlyMoving || instance.m_citizens.m_buffer[citizen].GetBuildingByLocation() != buildingID) && (instance2 == 0 || instance.m_instances.m_buffer[instance2].m_targetBuilding != buildingID || (instance.m_instances.m_buffer[instance2].m_flags & CitizenInstance.Flags.TargetIsNode) != 0) || instance.m_citizens.m_buffer[citizen].Collapsed)
                            {
                                continue;
                            }
                            ushort num3 = 0;
                            if (instance.m_citizens.m_buffer[citizen].m_workBuilding == buildingID)
                            {
                                num3 = instance.m_citizens.m_buffer[citizen].m_homeBuilding;
                            }
                            else if (instance.m_citizens.m_buffer[citizen].m_visitBuilding == buildingID)
                            {
                                if (instance.m_citizens.m_buffer[citizen].Arrested)
                                {
                                    instance.m_citizens.m_buffer[citizen].Arrested = false;
                                    if (instance2 != 0)
                                    {
                                        instance.ReleaseCitizenInstance(instance2);
                                    }
                                }
                                instance.m_citizens.m_buffer[citizen].SetVisitplace(citizen, 0, 0u);
                                num3 = instance.m_citizens.m_buffer[citizen].m_homeBuilding;
                            }
                            if (num3 != 0)
                            {
                                var citizenInfo = instance.m_citizens.m_buffer[citizen].GetCitizenInfo(citizen);
                                var humanAI = citizenInfo.m_citizenAI as HumanAI;
                                if (humanAI != null)
                                {
                                    instance.m_citizens.m_buffer[citizen].m_flags &= ~Citizen.Flags.Evacuating;
                                    humanAI.StartMoving(citizen, ref instance.m_citizens.m_buffer[citizen], buildingID, num3);
                                }
                            }
                        }
                    }
                    num = instance.m_units.m_buffer[num].m_nextUnit;
                    if (++num2 > 524288)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CommonBuildingAI), "HandleFire")]
        [HarmonyPrefix]
        public static void HandleFirePostfix(ushort buildingID, ref Building.Frame frameData)
        {
            // If fire is close to extinguishing but shouldn't yet
            if (frameData.m_fireDamage >= 210 &&
                frameData.m_fireDamage < 240 && // Not yet fully extinguished
                RealTimeBuildingAI != null &&
                !RealTimeBuildingAI.ShouldExtinguishFire(buildingID))
            {
                // Keep fire burning at a safe level
                frameData.m_fireDamage = 150;
            }
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(CommonBuildingAI), "GetVisitBehaviour")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void GetVisitBehaviour(object instance, ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount)
        {
            string message = "GetVisitBehaviour reverse Harmony patch wasn't applied";
            Debug.LogError(message);
            throw new NotImplementedException(message);
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(CommonBuildingAI), "GetHotelBehaviour")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void GetHotelBehaviour(object instance, ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveCount, ref int totalCount)
        {
            string message = "GetHotelBehaviour reverse Harmony patch wasn't applied";
            Debug.LogError(message);
            throw new NotImplementedException(message);
        }

        [HarmonyPatch(typeof(CommonBuildingAI), "ReleaseBuilding")]
        [HarmonyPostfix]
        public static void ReleaseBuildingPostfix(ushort buildingID, ref Building data)
        {
            if (BuildingWorkTimeManager.BuildingWorkTimeExist(buildingID))
            {
                BuildingWorkTimeManager.RemoveBuildingWorkTime(buildingID);
            }
            if (BuildingManagerConnection.IsHotel(buildingID) && HotelManager.HotelExist(buildingID))
            {
                HotelManager.RemoveHotel(buildingID);
            }
            if (data.Info.GetAI() is MainCampusBuildingAI && AcademicYearManager.MainCampusBuildingExist(buildingID))
            {
                AcademicYearManager.DeleteAcademicYearData(buildingID);
            }
            if (CommercialBuildingTypesManager.CommercialBuildingTypeExist(buildingID))
            {
                CommercialBuildingTypesManager.RemoveCommercialBuildingType(buildingID);
            }
            ResourceSlowdownManager.GarbageAccumulator[buildingID] = 0f;
            ResourceSlowdownManager.MailAccumulator[buildingID] = 0f;
        }
    }
}
