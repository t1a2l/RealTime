namespace RealTime.Patches.BuildingAIPatches
{
    using HarmonyLib;

    [HarmonyPatch]
    internal class RaceBuildingAIPatch
    {
        [HarmonyPatch(typeof(RaceBuildingAI), "HandleSpectators")]
        [HarmonyPrefix]
        public static bool HandleSpectators(RaceBuildingAI __instance, ushort buildingID, ref Building buildingData, int finalProductionRate, int totalVisitorCount, bool spectatorsAlongRoute) => false;
    }
}
