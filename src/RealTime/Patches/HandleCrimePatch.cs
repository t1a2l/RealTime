namespace RealTime.Patches
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.Managers;

    public struct HandleCrimeAccumulator
    {
        public ushort Crime;
        public ushort MainBuildingCrime;
        public ushort MainBuildingId;
    }

    [HarmonyPatch]
    public static class HandleCrimePatch
    {
        private static readonly HashSet<Type> _mainBuildingTypes =
        [
            typeof(AirportEntranceAI),
            typeof(ParkGateAI),
            typeof(MainCampusBuildingAI),
            typeof(MainIndustryBuildingAI)
        ];

        public static IEnumerable<MethodBase> TargetMethods()
        {
            var types = new[]
            {
                typeof(AirportBuildingAI),
                typeof(AirportCargoGateAI),
                typeof(AirportGateAI),
                typeof(AirportEntranceAI),
                typeof(CampusBuildingAI),
                typeof(MainCampusBuildingAI),
                typeof(CommonBuildingAI),
                typeof(IndustryBuildingAI),
                typeof(MainIndustryBuildingAI),
                typeof(MuseumAI),
                typeof(ParkBuildingAI),
                typeof(ParkGateAI)
            };

            var paramTypes = new[]
            {
                typeof(ushort),                          //  ushort buildingID (NOT ref)
                typeof(Building).MakeByRefType(),        //  ref Building data
                typeof(int),                             //  int crimeAccumulation (NOT ref)
                typeof(int),                             //  int citizenCount (NOT ref)
            };

            foreach (var type in types)
            {
                var method = AccessTools.Method(type, "HandleCrime", paramTypes);
                if (method != null && method.DeclaringType == type) // only if this type owns the override
                {
                    yield return method;
                }
            }
        }

        public static void Prefix(ref Building data, out HandleCrimeAccumulator __state, MethodBase __originalMethod)
        {
            var declaringType = __originalMethod.DeclaringType;
            bool isSubBuilding = declaringType != typeof(CommonBuildingAI) && !_mainBuildingTypes.Contains(declaringType);
            ushort mainBuildingId = 0;
            ushort mainCrime = 0;

            if (isSubBuilding)
            {
                mainBuildingId = GetMainBuildingId(ref data);
            }

            if (mainBuildingId != 0)
            {
                mainCrime = Singleton<BuildingManager>.instance.m_buildings.m_buffer[mainBuildingId].m_crimeBuffer;
            }

            __state = new HandleCrimeAccumulator
            {
                Crime = data.m_crimeBuffer,
                MainBuildingId = mainBuildingId,
                MainBuildingCrime = mainCrime
            };
        }

        public static void Postfix(ushort buildingID, ref Building data, HandleCrimeAccumulator __state)
        {
            // no main building slow building — apply slowdown to this building directly
            if (__state.MainBuildingId == 0)
            {
                ResourceSlowdownManager.ApplyCrimeSlowdown(buildingID, ref data, __state.Crime);
                return;
            }

            ref var mainBuildingData = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[__state.MainBuildingId];

            float multiplier;

            if (mainBuildingData.Info?.GetAI() is RaceStartBuildingAI)
            {
                return;
            }

            if (mainBuildingData.Info?.GetAI() is MainCampusBuildingAI || mainBuildingData.Info?.GetAI() is MainIndustryBuildingAI)
            {
                // Campus main building — strongest suppression
                multiplier = 0.02f;
            }
            else if (mainBuildingData.Info?.GetAI() is AirportEntranceAI)
            {
                // Airport entrance — strongest suppression
                multiplier = 0.001f;
            }
            else
            {
                // Other complex main buildings (park gate, etc.)
                multiplier = 0.08f;
            }

            ResourceSlowdownManager.ApplyCrimeSlowdown(__state.MainBuildingId, ref mainBuildingData, __state.MainBuildingCrime, multiplier);
        }

        private static ushort GetMainBuildingId(ref Building data)
        {
            var instance = Singleton<DistrictManager>.instance;
            byte b = instance.GetPark(data.m_position);
            ushort mainBuildingId = 0;
            if (b != 0)
            {
                var parkType = instance.m_parks.m_buffer[b].m_parkType;
                bool isAirport = instance.m_parks.m_buffer[b].IsAirport;
                bool isCampus = instance.m_parks.m_buffer[b].IsCampus;
                bool isIndustry = instance.m_parks.m_buffer[b].IsIndustry;
                bool isPark = instance.m_parks.m_buffer[b].IsPark;
                if (!isAirport && !isCampus && !isIndustry && !isPark)
                {
                    b = 0;
                }

                bool genricOrWrongCampusType = data.Info.GetAI() is CampusBuildingAI campusBuildingAI && (campusBuildingAI.m_campusType == DistrictPark.ParkType.GenericCampus || campusBuildingAI.m_campusType != parkType);
                bool genericOrWrongIndustryType = data.Info.GetAI() is IndustryBuildingAI industryBuildingAI && (industryBuildingAI.m_industryType == DistrictPark.ParkType.Industry || industryBuildingAI.m_industryType != parkType);

                if (isCampus && genricOrWrongCampusType || isIndustry && genericOrWrongIndustryType)
                {
                    b = 0;
                }
            }
            if (b != 0)
            {
                mainBuildingId = instance.m_parks.m_buffer[b].m_randomGate;
                if (mainBuildingId == 0)
                {
                    mainBuildingId = instance.m_parks.m_buffer[b].m_mainGate;
                }
            }
            return mainBuildingId;
        }
    }

    // Separate patch for the RaceBuildingAI
    [HarmonyPatch]
    public static class RaceBuildingAIHandleCrimePatch
    {
        public static MethodBase TargetMethod()
        {
            var paramTypes = new[]
            {
                typeof(ushort),                          //  ushort buildingID (NOT ref)
                typeof(Building).MakeByRefType(),        //  ref Building data
                typeof(int),                             //  int crimeAccumulation (NOT ref)
                typeof(int),                             //  int citizenCount (NOT ref)
            };

            return AccessTools.Method(typeof(RaceBuildingAI), "HandleCrime", paramTypes);
        }

        public static void Prefix(ushort buildingID, ref Building data, out HandleCrimeAccumulator __state, MethodBase __originalMethod)
        {
            ushort mainBuildingID = RaceBuildingAI.GetMainBuilding(buildingID, ref data);
            ushort mainCrime = 0;

            if (mainBuildingID != 0)
            {
                mainCrime = Singleton<BuildingManager>.instance.m_buildings.m_buffer[mainBuildingID].m_crimeBuffer;
            }
            __state = new HandleCrimeAccumulator
            {
                Crime = data.m_crimeBuffer,
                MainBuildingId = mainBuildingID,
                MainBuildingCrime = mainCrime
            };
        }

        public static void Postfix(HandleCrimeAccumulator __state, ushort mainBuildingID)
        {
            if (mainBuildingID == 0)
            {
                return;
            }

            ref var mainBuilding = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[__state.MainBuildingId];

            ResourceSlowdownManager.ApplyCrimeSlowdown(__state.MainBuildingId, ref mainBuilding, __state.MainBuildingCrime, 0.001f);
        }
    }
}
