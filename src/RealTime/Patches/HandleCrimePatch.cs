namespace RealTime.Patches
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.Managers;
    using UnityEngine;

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
            typeof(MainIndustryBuildingAI),
            typeof(RaceStartBuildingAI)
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
                typeof(ParkGateAI),
                typeof(RaceBuildingAI)
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

        public static void Prefix(ushort buildingID, ref Building data, out HandleCrimeAccumulator __state, MethodBase __originalMethod)
        {
            var declaringType = __originalMethod.DeclaringType;
            bool isSubBuilding = declaringType != typeof(CommonBuildingAI) && !_mainBuildingTypes.Contains(declaringType);
            ushort mainBuildingId = 0;
            ushort mainCrime = 0;

            if (isSubBuilding)
            {
                mainBuildingId = data.Info?.GetAI() is RaceBuildingAI ? RaceBuildingAI.GetMainBuilding(buildingID, ref data) : GetMainBuildingId(ref data);

                if (mainBuildingId != 0)
                {
                    mainCrime = Singleton<BuildingManager>.instance.m_buildings.m_buffer[mainBuildingId].m_crimeBuffer;
                }
                else
                {
                    // Sentinel: sub-building with no main — let vanilla run, skip our logic
                    mainBuildingId = ushort.MaxValue;
                }
            }

            __state = new HandleCrimeAccumulator
            {
                Crime = data.m_crimeBuffer,
                MainBuildingId = mainBuildingId,
                MainBuildingCrime = mainCrime
            };
        }

        public static void Postfix(ushort buildingID, ref Building data, int citizenCount, HandleCrimeAccumulator __state)
        {
            if (__state.MainBuildingId == ushort.MaxValue)
            {
                return;
            }

            // no main building slow building — apply slowdown to this building directly
            if (__state.MainBuildingId == 0)
            {
                ResourceSlowdownManager.ApplyCrimeSlowdown(buildingID, ref data, __state.Crime);
                if (data.Info?.GetAI() is not RaceStartBuildingAI)
                {
                    AddCrimeOffer(buildingID, ref data, citizenCount);
                }
                return;
            }

            ref var mainBuildingData = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[__state.MainBuildingId];

            ResourceSlowdownManager.ApplyCrimeSlowdown(__state.MainBuildingId, ref mainBuildingData, __state.MainBuildingCrime);
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

        private static void AddCrimeOffer(ushort buildingID, ref Building data, int citizenCount)
        {
            if (citizenCount != 0 && data.m_crimeBuffer > citizenCount * 25 && ResourceSlowdownManager.PendingCrimeDispatch.Contains(buildingID))
            {
                ResourceSlowdownManager.PendingCrimeDispatch.Remove(buildingID);
                int count = 0;
                int cargo = 0;
                int capacity = 0;
                int outside = 0;
                CalculateGuestVehicles(buildingID, ref data, TransferManager.TransferReason.Crime, ref count, ref cargo, ref capacity, ref outside);
                int num = 0;

                if (data.Info.GetAI() is AirportEntranceAI)
                {
                    num = (data.m_crimeBuffer >= citizenCount * 90) ? 5 : ((data.m_crimeBuffer >= citizenCount * 60) ? 3 : ((data.m_crimeBuffer < citizenCount * 25) ? 1 : 2));
                }

                if (count == num)
                {
                    var offer = new TransferManager.TransferOffer
                    {
                        Priority = data.m_crimeBuffer / Mathf.Max(1, citizenCount * 10),
                        Building = buildingID,
                        Position = data.m_position,
                        Amount = 1
                    };
                    Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Crime, offer);
                }
            }
        }

        private static void CalculateGuestVehicles(ushort buildingID, ref Building data, TransferManager.TransferReason material, ref int count, ref int cargo, ref int capacity, ref int outside)
        {
            var instance = Singleton<VehicleManager>.instance;
            ushort num = data.m_guestVehicles;
            int num2 = 0;
            while (num != 0)
            {
                if ((TransferManager.TransferReason)instance.m_vehicles.m_buffer[num].m_transferType == material)
                {
                    var info = instance.m_vehicles.m_buffer[num].Info;
                    info.m_vehicleAI.GetSize(num, ref instance.m_vehicles.m_buffer[num], out var size, out var max);
                    cargo += Mathf.Min(size, max);
                    capacity += max;
                    count++;
                    if ((instance.m_vehicles.m_buffer[num].m_flags & (Vehicle.Flags.Importing | Vehicle.Flags.Exporting)) != 0)
                    {
                        outside++;
                    }
                }
                num = instance.m_vehicles.m_buffer[num].m_nextGuestVehicle;
                if (++num2 > 16384)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
        }
    }
}
