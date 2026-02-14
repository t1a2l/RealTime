// HotelAIPatch.cs

namespace RealTime.Patches.BuildingAIPatches
{
    using System;
    using ColossalFramework;
    using HarmonyLib;
    using UnityEngine;

    /// <summary>
    /// A static class that provides the patch objects for the hotel advertisement AI game methods.
    /// </summary>
    [HarmonyPatch]
    internal static class HotelAIPatch
    {
        [HarmonyPatch(typeof(HotelAI), "ProduceGoods")]
        [HarmonyPrefix]
        public static void ProduceGoodsPrefix(ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, int finalProductionRate, ref Citizen.BehaviourData guestBehaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveGuestCount, int totalGuestCount, int guestPlaceCount)
        {
            // Remove tourist with no hotel or bad location from hotel building
            var instance = Singleton<CitizenManager>.instance;
            uint num = buildingData.m_citizenUnits;
            int num2 = 0;
            while (num != 0)
            {
                if ((instance.m_units.m_buffer[num].m_flags & CitizenUnit.Flags.Hotel) != 0)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        uint citizen = instance.m_units.m_buffer[num].GetCitizen(i);
                        if (Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizen].m_hotelBuilding == 0)
                        {
                            instance.m_citizens.m_buffer[citizen].RemoveFromUnit(citizen, ref instance.m_units.m_buffer[num]);
                        }
                        else if (Singleton<CitizenManager>.instance.m_citizens.m_buffer[citizen].CurrentLocation == Citizen.Location.Home)
                        {
                            instance.m_citizens.m_buffer[citizen].RemoveFromUnit(citizen, ref instance.m_units.m_buffer[num]);
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
        }

        [HarmonyPatch(typeof(HotelAI), "ProduceGoods")]
        [HarmonyPostfix]
        public static void ProduceGoodsPostfix(HotelAI __instance, ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, int finalProductionRate, ref Citizen.BehaviourData guestBehaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveGuestCount, int totalGuestCount, int guestPlaceCount)
        {
            int aliveCount = 0;
            int hotelTotalCount = 0;
            Citizen.BehaviourData behaviour = default;
            CommonBuildingAIPatch.GetHotelBehaviour(__instance, buildingID, ref buildingData, ref behaviour, ref aliveCount, ref hotelTotalCount);
            buildingData.m_roomUsed = (ushort)hotelTotalCount;
        }

        [HarmonyPatch(typeof(HotelAdvertisementAI), "GetEndFrame")]
        [HarmonyPrefix]
        private static bool GetEndFrame(HotelAdvertisementAI __instance, ushort eventID, ref EventData data, ref uint __result)
        {
            uint num = (uint)Mathf.RoundToInt(__instance.m_eventDuration * SimulationManager.DAYTIME_HOUR_TO_FRAME);
            __result = data.m_startFrame + num;
            return false;
        }

    }
}
