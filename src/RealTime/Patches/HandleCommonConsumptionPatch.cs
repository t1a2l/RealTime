namespace RealTime.Patches
{
    using System.Collections.Generic;
    using System.Reflection;
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.Managers;
    using UnityEngine;
    using static RealTime.Patches.HandleCommonConsumptionPatch;

    [HarmonyPatch]
    public static class HandleCommonConsumptionPatch
    {
        public struct Accumulator
        {
            public ushort Garbage;
            public ushort Mail;
            public ushort MainBuildingGarbage;
            public ushort MainBuildingMail;
            public ushort MainBuildingId;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            var types = new[]
            {
                typeof(AirportAuxBuildingAI),
                typeof(AirportCargoGateAI),
                typeof(AirportGateAI),
                typeof(CampusBuildingAI),
                typeof(CommonBuildingAI),
                typeof(IndustryBuildingAI),
                typeof(MuseumAI),
                typeof(ParkBuildingAI)
            };

            var paramTypes = new[]
            {
                typeof(ushort),
                typeof(Building).MakeByRefType(),        // ref Building
                typeof(Building.Frame).MakeByRefType(),  // ref Building.Frame
                typeof(int).MakeByRefType(),             // ref int electricityConsumption
                typeof(int).MakeByRefType(),             // ref int heatingConsumption
                typeof(int).MakeByRefType(),             // ref int waterConsumption
                typeof(int).MakeByRefType(),             // ref int sewageAccumulation
                typeof(int).MakeByRefType(),             // ref int garbageAccumulation
                typeof(int).MakeByRefType(),             // ref int mailAccumulation
                typeof(int),                             // int maxMail (NOT ref)
                typeof(DistrictPolicies.Services)        // DistrictPolicies.Services (NOT ref)
            };

            foreach (var type in types)
            {
                var method = AccessTools.Method(type, "HandleCommonConsumption", paramTypes);
                if (method != null && method.DeclaringType == type) // only if this type owns the override
                {
                    yield return method;
                }
            }
        }

        public static void Prefix(ref Building data, out Accumulator __state, MethodBase __originalMethod)
        {
            bool isComplexBuilding = __originalMethod.DeclaringType != typeof(CommonBuildingAI);
            ushort mainBuildingId = 0;
            ushort mainGarbage = 0;
            ushort mainMail = 0;

            if (isComplexBuilding)
            {
                mainBuildingId = GetMainBuildingId(ref data);
            }
            else
            {
                if (data.Info.GetAI() is RaceStartBuildingAI)
                {
                    Debug.Log("Race start building garbage before: " + data.m_garbageBuffer);
                }
            }

            if (mainBuildingId != 0)
            {
                var mb = Singleton<BuildingManager>.instance.m_buildings.m_buffer[mainBuildingId];
                mainGarbage = mb.m_garbageBuffer;
                mainMail = mb.m_mailBuffer;
            }

            __state = new Accumulator
            {
                Garbage = data.m_garbageBuffer,
                Mail = data.m_mailBuffer,
                MainBuildingId = mainBuildingId,
                MainBuildingGarbage = mainGarbage,
                MainBuildingMail = mainMail
            };
        }

        public static void Postfix(ushort buildingID, ref Building data, Accumulator __state)
        {
            // Always slow the building itself
            ResourceSlowdownManager.ApplyGarbageSlowdown(buildingID, ref data, __state.Garbage);
            ResourceSlowdownManager.ApplyMailSlowdown(buildingID, ref data, __state.Mail);

            if (data.Info.GetAI() is AirportEntranceAI || data.Info.GetAI() is RaceStartBuildingAI)
            {
               return;
            }

            if (__state.MainBuildingId == 0)
            {
                return;
            }

            ref var mainBuildingData = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[__state.MainBuildingId];

            float multiplier;

            if (mainBuildingData.Info.GetAI() is MainCampusBuildingAI)
            {
                // Campus main building — strongest suppression
                multiplier = 0.02f;
            }
            else
            {
                // Other complex main buildings (park gate, industry main, etc.)
                multiplier = 0.08f;
            }

            ResourceSlowdownManager.ApplyGarbageSlowdown(__state.MainBuildingId, ref mainBuildingData, __state.MainBuildingGarbage, multiplier);
            ResourceSlowdownManager.ApplyMailSlowdown(__state.MainBuildingId, ref mainBuildingData, __state.MainBuildingMail, multiplier);
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

    // Separate patch for the RaceBuildingAI 12-parameter overload
    [HarmonyPatch]
    public static class CommonBuildingAIHandleCommonConsumptionPatch
    {
        public static MethodBase TargetMethod()
        {
            var paramTypes = new[]
            {
                typeof(ushort),
                typeof(Building).MakeByRefType(),        // ref Building
                typeof(Building.Frame).MakeByRefType(),  // ref Building.Frame
                typeof(int).MakeByRefType(),             // ref int electricityConsumption
                typeof(int).MakeByRefType(),             // ref int heatingConsumption
                typeof(int).MakeByRefType(),             // ref int waterConsumption
                typeof(int).MakeByRefType(),             // ref int sewageAccumulation
                typeof(int).MakeByRefType(),             // ref int garbageAccumulation
                typeof(int).MakeByRefType(),             // ref int mailAccumulation
                typeof(int),                             // int maxMail (NOT ref)
                typeof(DistrictPolicies.Services),        // DistrictPolicies.Services (NOT ref)
                typeof(ushort)
            };

            return AccessTools.Method(typeof(CommonBuildingAI), "HandleCommonConsumption", paramTypes);
        }

        public static void Prefix(ref Building data, out Accumulator __state, ushort mainBuildingID)
        {
            ushort mainGarbage = 0;
            ushort mainMail = 0;
            if (mainBuildingID != 0)
            {
                ref var mainBuilding = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[mainBuildingID];
                mainGarbage = mainBuilding.m_garbageBuffer;
                mainMail = mainBuilding.m_mailBuffer;
            }
            __state = new Accumulator
            {
                Garbage = data.m_garbageBuffer,
                Mail = data.m_mailBuffer,
                MainBuildingId = mainBuildingID,
                MainBuildingGarbage = mainGarbage,
                MainBuildingMail = mainMail
            };
        }

        public static void Postfix(Accumulator __state, ushort mainBuildingID)
        {
            if (mainBuildingID == 0)
            {
                return;
            }

            ref var mainBuilding = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[__state.MainBuildingId];

            ResourceSlowdownManager.ApplyGarbageSlowdown(__state.MainBuildingId, ref mainBuilding, __state.MainBuildingGarbage, 0.001f);
            ResourceSlowdownManager.ApplyMailSlowdown(__state.MainBuildingId, ref mainBuilding, __state.MainBuildingMail, 0.001f);
        }
    }
}
