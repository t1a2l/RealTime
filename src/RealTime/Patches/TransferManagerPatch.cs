// TransferManagerPatch.cs

namespace RealTime.Patches
{
    using ColossalFramework;
    using HarmonyLib;
    using RealTime.CustomAI;
    using RealTime.Managers;

    /// <summary>
    /// A static class that provides the patch objects for the game's transfer manager.
    /// </summary>
    [HarmonyPatch]
    internal static class TransferManagerPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        [HarmonyPatch(typeof(TransferManager), "AddOutgoingOffer")]
        [HarmonyPrefix]
        private static bool AddOutgoingOfferPrefix(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI == null)
            {
                return true;
            }

            switch (material)
            {
                case TransferManager.TransferReason.Entertainment:
                case TransferManager.TransferReason.EntertainmentB:
                case TransferManager.TransferReason.EntertainmentC:
                case TransferManager.TransferReason.EntertainmentD:
                case TransferManager.TransferReason.TouristA:
                case TransferManager.TransferReason.TouristB:
                case TransferManager.TransferReason.TouristC:
                case TransferManager.TransferReason.TouristD:
                case TransferManager.TransferReason.BusinessA:
                case TransferManager.TransferReason.BusinessB:
                case TransferManager.TransferReason.BusinessC:
                case TransferManager.TransferReason.BusinessD:
                case TransferManager.TransferReason.NatureA:
                case TransferManager.TransferReason.NatureB:
                case TransferManager.TransferReason.NatureC:
                case TransferManager.TransferReason.NatureD:
                    return RealTimeBuildingAI.IsEntertainmentTarget(offer.Building);

                case TransferManager.TransferReason.Shopping:
                case TransferManager.TransferReason.ShoppingB:
                case TransferManager.TransferReason.ShoppingC:
                case TransferManager.TransferReason.ShoppingD:
                case TransferManager.TransferReason.ShoppingE:
                case TransferManager.TransferReason.ShoppingF:
                case TransferManager.TransferReason.ShoppingG:
                case TransferManager.TransferReason.ShoppingH:
                    return RealTimeBuildingAI.IsShoppingTarget(offer.Building);

                case TransferManager.TransferReason.Mail: // buildings request to send or recieve mail
                case TransferManager.TransferReason.UnsortedMail: // post offices request to pick up unsorted mail
                    return RealTimeBuildingAI.IsMailHours(offer.Building);

                case TransferManager.TransferReason.Garbage: // buildings sends outgoing offers for garbage
                    return RealTimeBuildingAI.IsGarbageHours(offer.Building);

                case TransferManager.TransferReason.Crime: // buildings request police services
                    return !IsManagedBuilding(offer.Building);

                default:
                    return true;
            }
        }

        [HarmonyPatch(typeof(TransferManager), "AddIncomingOffer")]
        [HarmonyPrefix]
        private static bool AddIncomingOfferPrefix(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
        {
            if (RealTimeBuildingAI == null)
            {
                return true;
            }

            switch (material)
            {
                case TransferManager.TransferReason.SortedMail: // post offices request to send sorted mail
                    return RealTimeBuildingAI.IsMailHours(offer.Building);

                case TransferManager.TransferReason.RoadMaintenance: // road segments request road maintenance services
                    return RealTimeBuildingAI.IsRoadMaintenanceServiceHours(offer.NetSegment);

                case TransferManager.TransferReason.Snow: // road segments request snow services
                    return RealTimeBuildingAI.IsSnowServiceHours(offer.NetSegment);

                case TransferManager.TransferReason.ParkMaintenance: // park buildings request maintenance
                    return RealTimeBuildingAI.IsParkMaintenanceHours(offer.Building);

                default:
                    return true;
            }
        }

        private static bool IsManagedBuilding(ushort buildingId)
        {
            ref var data = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingId];
            var ai = data.Info?.GetAI();
            if( ai is CommonBuildingAI  // covers growables
                || ai is AirportEntranceAI
                || ai is ParkGateAI
                || ai is MainCampusBuildingAI
                || ai is MainIndustryBuildingAI
                || ai is RaceStartBuildingAI)
            {
                ResourceSlowdownManager.PendingCrimeDispatch.Add(buildingId);
                return true;
            }
            return false;
            // sub-buildings don't dispatch police themselves so no need to list them
        }

    }
}
