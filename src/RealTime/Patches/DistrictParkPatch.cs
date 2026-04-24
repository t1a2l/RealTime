// DistrictParkPatch.cs

namespace RealTime.Patches
{
    using ColossalFramework;
    using System;
    using HarmonyLib;
    using RealTime.CustomAI;
    using RealTime.GameConnection;
    using ColossalFramework.Threading;
    using UnityEngine;
    using ColossalFramework.Math;
    using SkyTools.Tools;
    using RealTime.Config;
    using RealTime.Managers;

    /// <summary>
    /// A static class that provides the patch objects for the Park Life DLC related methods.
    /// </summary>
    [HarmonyPatch]
    internal static class DistrictParkPatch
    {
        /// <summary>Gets or sets the city spare time behavior.</summary>
        public static ISpareTimeBehavior SpareTimeBehavior { get; set; }

        /// <summary>Gets or sets the city time behavior.</summary>
        public static TimeInfo TimeInfo { get; set; }

        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        /// <summary>Gets or sets the mod configuration.</summary>
        public static RealTimeConfig RealTimeConfig { get; set; }

        [HarmonyPatch(typeof(DistrictPark), "SimulationStep")]
        [HarmonyPostfix]
        private static void Postfix(byte parkID)
        {
            if (parkID != 0)
            {
                ref var park = ref DistrictManager.instance.m_parks.m_buffer[parkID];

                if (SpareTimeBehavior != null && !SpareTimeBehavior.AreFireworksAllowed)
                {
                    park.m_flags &= ~DistrictPark.Flags.SpecialMode;
                    return;
                }

                if (park.m_dayNightCount == 6 || (park.m_parkPolicies & DistrictPolicies.Park.FireworksBoost) != 0)
                {
                    park.m_flags |= DistrictPark.Flags.SpecialMode;
                }
            }
        }

        [HarmonyPatch(typeof(DistrictPark), "CampusSimulationStep")]
        [HarmonyPrefix]
        [HarmonyAfter(["t1a2l.CombinedAIS"])]
        private static bool CampusSimulationStepPrefix(DistrictPark __instance, byte parkID)
        {
            var instance = Singleton<SimulationManager>.instance;
            var instance2 = Singleton<DistrictManager>.instance;
                
            if (parkID == 0 || __instance.m_parkType == DistrictPark.ParkType.GenericCampus)
            {
                return false;
            }
            ref var campus = ref instance2.m_parks.m_buffer[parkID];

            campus.m_finalGateCount = campus.m_tempGateCount;
            campus.m_finalVisitorCapacity = campus.m_tempVisitorCapacity;
            campus.m_finalMainCapacity = campus.m_tempMainCapacity;
            campus.m_tempEntertainmentAccumulation = 0;
            campus.m_tempAttractivenessAccumulation = 0;
            campus.m_tempGateCount = 0;
            campus.m_tempVisitorCapacity = 0;
            campus.m_tempMainCapacity = 0;
            campus.m_studentCount = 0u;
            campus.m_studentCapacity = 0u;

            var instance3 = Singleton<BuildingManager>.instance;
            var serviceBuildings = instance3.GetServiceBuildings(ItemClass.Service.PlayerEducation);
            for (int i = 0; i < serviceBuildings.m_size; i++)
            {
                ushort num = serviceBuildings.m_buffer[i];
                byte park = Singleton<DistrictManager>.instance.GetPark(instance3.m_buildings.m_buffer[num].m_position);
                if (park == parkID)
                {
                    int count = 0;
                    int capacity = 0;
                    var campusBuildingAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[serviceBuildings[i]].Info.m_buildingAI as CampusBuildingAI;
                    var uniqueFacultyAI = Singleton<BuildingManager>.instance.m_buildings.m_buffer[serviceBuildings[i]].Info.m_buildingAI as UniqueFacultyAI;
                    campusBuildingAI?.GetStudentCount(serviceBuildings[i], ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[serviceBuildings[i]], out count, out capacity, out int global);
                    uniqueFacultyAI?.GetStudentCount(serviceBuildings[i], ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[serviceBuildings[i]], out count, out capacity, out global);
                    campus.m_studentCount += (uint)count;
                    campus.m_studentCapacity += (uint)capacity;
                }
            }
            int num2 = 0;
            if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[campus.m_mainGate].m_productionRate > 0 && (Singleton<BuildingManager>.instance.m_buildings.m_buffer[campus.m_mainGate].m_flags & Building.Flags.Collapsed) == 0 && (campus.m_parkPolicies & DistrictPolicies.Park.UniversalEducation) == 0)
            {
                campus.m_finalTicketIncome = campus.m_studentCount * Singleton<DistrictManager>.instance.m_properties.m_parkProperties.m_campusLevelInfo[(uint)campus.m_parkLevel].m_tuitionMoneyPerStudent;
                int amount = (int)(campus.m_finalTicketIncome / 16 * 100);
                Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.PublicIncome, amount, Singleton<BuildingManager>.instance.m_buildings.m_buffer[campus.m_mainGate].Info.m_class);
            }
            else
            {
                campus.m_finalTicketIncome = 0u;
            }
            num2 += (int)campus.m_finalTicketIncome;
            if (campus.m_mainGate != 0)
            {
                float academicYearProgress = __instance.GetAcademicYearProgress();
                if (campus.m_previousYearProgress > academicYearProgress)
                {
                    campus.m_previousYearProgress = academicYearProgress;
                }
                float num3 = academicYearProgress - campus.m_previousYearProgress;
                campus.m_previousYearProgress = academicYearProgress;
                campus.m_academicStaffCount = (byte)Mathf.Clamp(campus.m_academicStaffCount, Singleton<DistrictManager>.instance.m_properties.m_parkProperties.m_campusProperties.m_academicStaffCountMin, Singleton<DistrictManager>.instance.m_properties.m_parkProperties.m_campusProperties.m_academicStaffCountMax);
                float num4 = (campus.m_academicStaffCount - Singleton<DistrictManager>.instance.m_properties.m_parkProperties.m_campusProperties.m_academicStaffCountMin) / (float)(Singleton<DistrictManager>.instance.m_properties.m_parkProperties.m_campusProperties.m_academicStaffCountMax - Singleton<DistrictManager>.instance.m_properties.m_parkProperties.m_campusProperties.m_academicStaffCountMin);
                campus.m_academicStaffAccumulation += num3 * num4;
                campus.m_academicStaffAccumulation = Mathf.Clamp(campus.m_academicStaffAccumulation, 0f, 1f);
                int num5 = (int)(__instance.CalculateAcademicStaffWages() / 0.16f) / 100;
                Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Maintenance, num5, ItemClass.Service.PlayerEducation, campus.CampusTypeToSubservice(), ItemClass.Level.None);
                num2 -= num5;
                if (campus.m_awayMatchesDone == null || campus.m_awayMatchesDone.Length != 5)
                {
                    campus.m_awayMatchesDone = new bool[5];
                }
                for (int j = 0; j < 5; j++)
                {
                    if (!campus.m_awayMatchesDone[j] && academicYearProgress > 1f / 6f * (j + 1))
                    {
                        SimulateVarsityAwayGame(campus, parkID);
                        campus.m_awayMatchesDone[j] = true;
                    }
                }

                var academicYearData = AcademicYearManager.GetAcademicYearData(campus.m_mainGate);

                Log.Debug(LogCategory.Events, TimeInfo.Now, $"DidLastYearEnd: {academicYearData.DidLastYearEnd}, graduation: {(campus.m_flags & DistrictPark.Flags.Graduation) != 0}, DidGraduationStart: {academicYearData.DidGraduationStart}, currentHour: {TimeInfo.CurrentHour}, GraduationStartTime: {academicYearData.GraduationStartTime}");
                if (academicYearData.DidLastYearEnd && (campus.m_flags & DistrictPark.Flags.Graduation) == 0 && !academicYearData.DidGraduationStart)
                {
                    bool shouldStartGraduation = true;
                    if (TimeInfo.IsNightTime || TimeInfo.Now.IsWeekend())
                    {
                        shouldStartGraduation = false;
                    }
                    if (TimeInfo.CurrentHour < 9f || TimeInfo.CurrentHour > 12f)
                    {
                        shouldStartGraduation = false;
                    }
                    if(shouldStartGraduation)
                    {
                        campus.m_flags |= DistrictPark.Flags.Graduation;
                        academicYearData.GraduationStartTime = TimeInfo.CurrentHour;
                        academicYearData.DidGraduationStart = true;
                        AcademicYearManager.SetAcademicYearData(campus.m_mainGate, academicYearData);
                        Log.Debug(LogCategory.Events, TimeInfo.Now, $"GraduationStartTime: {academicYearData.GraduationStartTime}, DidGraduationStart: {academicYearData.DidGraduationStart}");
                    }
                }
                if (academicYearData.DidLastYearEnd && (campus.m_flags & DistrictPark.Flags.Graduation) != 0
                    && academicYearData.DidGraduationStart && TimeInfo.CurrentHour - academicYearData.GraduationStartTime > 3f)
                {
                    academicYearData.GraduationStartTime = 0;
                    AcademicYearManager.SetAcademicYearData(campus.m_mainGate, academicYearData);
                    campus.m_flags &= ~DistrictPark.Flags.Graduation;
                    Log.Debug(LogCategory.Events, TimeInfo.Now, $"GraduationStartTime: {academicYearData.GraduationStartTime}, DidGraduationStart: {academicYearData.DidGraduationStart}");
                }

                if (!academicYearData.DidLastYearEnd && academicYearData.DidGraduationStart)
                {
                    academicYearData.DidGraduationStart = false;
                    AcademicYearManager.SetAcademicYearData(campus.m_mainGate, academicYearData);
                    Log.Debug(LogCategory.Events, TimeInfo.Now, $"DidGraduationStart: {academicYearData.DidGraduationStart}");
                }
            }
            if (campus.m_coachCount != 0)
            {
                int num6 = (int)(__instance.CalculateCoachingStaffCost() / 0.16f) / 100;
                Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Maintenance, num6, ItemClass.Service.PlayerEducation, campus.CampusTypeToSubservice(), ItemClass.Level.None);
                num2 -= num6;
            }
            int activeArenasCount = campus.GetActiveArenasCount();
            if (campus.m_cheerleadingBudget != 0)
            {
                int num7 = (int)(campus.m_cheerleadingBudget * activeArenasCount / 0.16f);
                Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Maintenance, num7, ItemClass.Service.PlayerEducation, campus.CampusTypeToSubservice(), ItemClass.Level.None);
                num2 -= num7;
            }
            if ((campus.m_parkPolicies & DistrictPolicies.Park.UniversalEducation) != 0)
            {
                campus.m_parkPoliciesEffect |= DistrictPolicies.Park.UniversalEducation;
            }
            if ((campus.m_parkPolicies & DistrictPolicies.Park.StudentHealthcare) != 0)
            {
                campus.m_parkPoliciesEffect |= DistrictPolicies.Park.StudentHealthcare;
                int num8 = (int)(campus.m_studentCount * 3125) / 100;
                Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, num8, ItemClass.Service.PlayerEducation, campus.CampusTypeToSubservice(), ItemClass.Level.None);
                num2 -= num8;
            }
            if ((campus.m_parkPolicies & DistrictPolicies.Park.FreeLunch) != 0)
            {
                campus.m_parkPoliciesEffect |= DistrictPolicies.Park.FreeLunch;
                int num9 = (int)(campus.m_studentCount * 625) / 100;
                Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, num9, ItemClass.Service.PlayerEducation, campus.CampusTypeToSubservice(), ItemClass.Level.None);
                num2 -= num9;
            }
            if ((campus.m_parkPolicies & DistrictPolicies.Park.VarsitySportsAds) != 0)
            {
                campus.m_parkPoliciesEffect |= DistrictPolicies.Park.VarsitySportsAds;
                int num10 = 0;
                int num11 = 1250;
                num10 = num11 * activeArenasCount;
                Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, num10, ItemClass.Service.PlayerEducation, campus.CampusTypeToSubservice(), ItemClass.Level.None);
                num2 -= num10;
            }
            if ((campus.m_parkPolicies & DistrictPolicies.Park.FreeFanMerchandise) != 0)
            {
                campus.m_parkPoliciesEffect |= DistrictPolicies.Park.FreeFanMerchandise;
                int num12 = 0;
                int num13 = 1125;
                num12 = num13 * activeArenasCount;
                Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, num12, ItemClass.Service.PlayerEducation, campus.CampusTypeToSubservice(), ItemClass.Level.None);
                num2 -= num12;
            }
            if (campus.CanAddExchangeStudentAttractiveness())
            {
                int num14 = campus.CalculateExchangeStudentAttractiveness();
                if (num14 != 0)
                {
                    num14 = num14 / 4 * 10;
                    float num15 = Singleton<ImmaterialResourceManager>.instance.CheckActualTourismResource();
                    num15 = (num15 * (float)num14 + 99f) / 100f;
                    Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.Attractiveness, Mathf.RoundToInt(num15));
                }
            }
            long num16 = campus.m_ledger.ReadCurrentTogaPartySeed();
            if (num16 != 0 && (TimeInfo.Now - new DateTime(num16)).Hours > RealTimeConfig.TogaPartyLength)
            {
                EndTogaParty(campus, parkID);
            }
            if (campus.m_mainGate != 0 && campus.m_ledger.ReadYearData(DistrictPark.AcademicYear.Last).m_reputationLevel != 0)
            {
                var properties = Singleton<GuideManager>.instance.m_properties;
                if (properties is not null)
                {
                    Singleton<DistrictManager>.instance.m_academicYearReportClosed.Activate(properties.m_academicYearReportClosed, campus.m_mainGate);
                }
            }
            if (num2 >= 0 && campus.m_studentCount >= 5000 && Singleton<SimulationManager>.instance.m_metaData.m_disableAchievements != SimulationMetaData.MetaBool.True)
            {
                ThreadHelper.dispatcher.Dispatch(delegate
                {
                    SteamHelper.UnlockAchievement("ForForProfitEducation");
                });
            }
            return false;
        }

        [HarmonyPatch(typeof(DistrictPark), "GetAcademicYearProgress")]
        [HarmonyPrefix]
        public static bool GetAcademicYearProgress(DistrictPark __instance, ref float __result)
        {
            var academicYearData = AcademicYearManager.GetAcademicYearData(__instance.m_mainGate);
            if (academicYearData.IsFirstAcademicYear)
            {
                __result = 0f;
                return false;
            }
            return true;
        }

        private static void EndTogaParty(DistrictPark __instance, byte parkID)
        {
            var instance2 = Singleton<DistrictManager>.instance;

            if(parkID == 0 || __instance.m_parkType == DistrictPark.ParkType.None || parkID >= instance2.m_parks.m_size || __instance.m_partyVenue == 0)
            {
                return;
            }

            ref var campus = ref instance2.m_parks.m_buffer[parkID];
            campus.m_flags &= ~DistrictPark.Flags.TogaParty;
            var instance = Singleton<BuildingManager>.instance;

            var building = instance.m_buildings.m_buffer[__instance.m_partyVenue];

            if(building.Info == null)
            {
                return;
            }

            if (building.Info.m_buildingAI is CampusBuildingAI)
            {
                for (int i = 0; i < __instance.m_arrivedAtParty; i++)
                {
                    CreatePartyReturner(__instance.m_partyVenue, ref instance.m_buildings.m_buffer[__instance.m_partyVenue], parkID);
                }
            }
            campus.m_partyVenue = 0;
            campus.m_arrivedAtParty = 0;
            campus.m_goingToParty = 0;
        }

        private static void SimulateVarsityAwayGame(DistrictPark __instance, byte parkID)
        {
            for (int i = 0; i < 5; i++)
            {
                var sport = (DistrictPark.SportIndex)i;
                if (__instance.GetArenas(sport).m_size != 0)
                {
                    if (GetMatchResult(__instance, ref Singleton<SimulationManager>.instance.m_randomizer))
                    {
                        __instance.m_ledger.WriteMatchWon(parkID, sport);
                    }
                    else
                    {
                        __instance.m_ledger.WriteMatchLost(parkID, sport);
                    }
                }
            }
        }

        private static bool GetMatchResult(DistrictPark __instance, ref Randomizer randomizer)
        {
            int randomChanceModifier = randomizer.Int32(0, Singleton<DistrictManager>.instance.m_properties.m_parkProperties.m_campusProperties.m_randomChanceModifier);
            float winProbability = __instance.GetWinProbability(randomChanceModifier);
            int num = randomizer.Int32(10000u);
            return (float)num < winProbability * 100f;
        }

        private static void CreatePartyReturner(ushort buildingID, ref Building data, byte campus)
        {
            var instance = Singleton<CitizenManager>.instance;
            var gender = (Citizen.Gender)Singleton<SimulationManager>.instance.m_randomizer.Int32(2u);
            var groupCitizenInfo = instance.GetGroupCitizenInfo(ref Singleton<SimulationManager>.instance.m_randomizer, ItemClass.Service.PlayerEducation, gender, Citizen.SubCulture.Hippie, Citizen.AgePhase.Adult0);
            if (groupCitizenInfo is not null && instance.CreateCitizenInstance(out ushort instance2, ref Singleton<SimulationManager>.instance.m_randomizer, groupCitizenInfo, 0u))
            {
                ushort randomCampusBuilding = Singleton<DistrictManager>.instance.m_parks.m_buffer[campus].GetRandomCampusBuilding(campus, instance2);
                if (randomCampusBuilding == 0)
                {
                    instance.ReleaseCitizenInstance(instance2);
                    return;
                }
                groupCitizenInfo.m_citizenAI.SetSource(instance2, ref instance.m_instances.m_buffer[instance2], buildingID);
                groupCitizenInfo.m_citizenAI.SetTarget(instance2, ref instance.m_instances.m_buffer[instance2], randomCampusBuilding);
            }
        }

    }
    
}
