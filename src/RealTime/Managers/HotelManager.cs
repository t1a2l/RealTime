namespace RealTime.Managers
{
    using System;
    using System.Collections.Generic;
    using ColossalFramework;
    using ColossalFramework.Math;
    using UnityEngine;

    internal static class HotelManager
    {
        private static List<ushort> HotelsList;

        public static void Init() => HotelsList = [];

        public static bool HotelExist(ushort buidlingId)
        {
            int index = HotelsList.FindIndex(item => item == buidlingId);
            return index != -1;
        }

        public static void AddHotel(ushort buidlingId) => HotelsList.Add(buidlingId);

        public static void RemoveHotel(ushort buidlingId) => HotelsList.Remove(buidlingId);

        public static ushort FindRandomHotel()
        {
            if (HotelsList.Count == 0)
            {
                return 0;
            }

            for (int i = HotelsList.Count - 1; i >= 0; i--)
            {
                var hotelBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[HotelsList[i]];
                if (hotelBuilding.m_roomUsed >= hotelBuilding.m_roomMax)
                {
                    HotelsList.RemoveAt(i);
                }
            }

            if (HotelsList.Count == 0)
            {
                return 0;
            }

            int index = Singleton<SimulationManager>.instance.m_randomizer.Int32(0, HotelsList.Count - 1);
            return HotelsList[index];
        }

        public static void HotelCheck(ushort buildingID, ref Building data, string state)
        {
            if (data.Info.GetAI() is PrivateBuildingAI privateBuildingAI)
            {
                data.m_level = (byte)Mathf.Max(data.m_level, (int)privateBuildingAI.m_info.m_class.m_level);
                privateBuildingAI.CalculateWorkplaceCount((ItemClass.Level)data.m_level, new Randomizer(buildingID), data.Width, data.Length, out int level, out int level2, out int level3, out int level4);
                privateBuildingAI.AdjustWorkplaceCount(buildingID, ref data, ref level, ref level2, ref level3, ref level4);
                int workCount = level + level2 + level3 + level4;
                int visitCount = privateBuildingAI.CalculateVisitplaceCount((ItemClass.Level)data.m_level, new Randomizer(buildingID), data.Width, data.Length);
                int hotelRoomCount = visitCount;
                if (BuildingWorkTimeManager.HotelNamesList.ContainsKey(data.Info.name))
                {
                    hotelRoomCount = BuildingWorkTimeManager.HotelNamesList[data.Info.name];
                }
                visitCount = hotelRoomCount * 20 / 100;
                if (state == "create")
                {
                    Singleton<CitizenManager>.instance.CreateUnits(out data.m_citizenUnits, ref Singleton<SimulationManager>.instance.m_randomizer, buildingID, 0, 0, workCount, visitCount, 0, 0, hotelRoomCount);
                }
                else
                {
                    EnsureCitizenUnits(buildingID, ref data, 0, workCount, visitCount, 0, hotelRoomCount);
                }   
                data.m_roomMax = (ushort)hotelRoomCount;
                if (!HotelExist(buildingID))
                {
                    if(state == "create" || state == "loaded" && data.m_roomUsed < data.m_roomMax)
                    {
                        AddHotel(buildingID);
                    }
                }
            }
            else if (!HotelExist(buildingID))
            {
                AddHotel(buildingID);
            }
        }

        private static void EnsureCitizenUnits(ushort buildingID, ref Building data, int homeCount = 0, int workCount = 0, int visitCount = 0, int studentCount = 0, int hotelCount = 0)
        {
            if ((data.m_flags & (Building.Flags.Abandoned | Building.Flags.Collapsed)) != 0)
            {
                return;
            }
            var wealthLevel = Citizen.GetWealthLevel((ItemClass.Level)data.m_level);
            var instance = Singleton<CitizenManager>.instance;
            uint num = 0u;
            uint num2 = data.m_citizenUnits;
            int num3 = 0;
            while (num2 != 0)
            {
                var flags = instance.m_units.m_buffer[num2].m_flags;
                if ((flags & CitizenUnit.Flags.Home) != 0)
                {
                    instance.m_units.m_buffer[num2].SetWealthLevel(wealthLevel);
                    homeCount--;
                }
                if ((flags & CitizenUnit.Flags.Work) != 0)
                {
                    workCount -= 5;
                }
                if ((flags & CitizenUnit.Flags.Visit) != 0)
                {
                    visitCount -= 5;
                }
                if ((flags & CitizenUnit.Flags.Student) != 0)
                {
                    studentCount -= 5;
                }
                if ((flags & CitizenUnit.Flags.Hotel) != 0)
                {
                    hotelCount -= 5;
                }
                num = num2;
                num2 = instance.m_units.m_buffer[num2].m_nextUnit;
                if (++num3 > Singleton<CitizenManager>.instance.m_units.m_size)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                    break;
                }
            }
            homeCount = Mathf.Max(0, homeCount);
            workCount = Mathf.Max(0, workCount);
            visitCount = Mathf.Max(0, visitCount);
            studentCount = Mathf.Max(0, studentCount);
            hotelCount = Mathf.Max(0, hotelCount);
            if (homeCount == 0 && workCount == 0 && visitCount == 0 && studentCount == 0 && hotelCount == 0)
            {
                return;
            }
            if (instance.CreateUnits(out uint firstUnit, ref Singleton<SimulationManager>.instance.m_randomizer, buildingID, 0, homeCount, workCount, visitCount, 0, studentCount, hotelCount))
            {
                if (num != 0)
                {
                    instance.m_units.m_buffer[num].m_nextUnit = firstUnit;
                }
                else
                {
                    data.m_citizenUnits = firstUnit;
                }
            }
        }
    }
}
