// PostOfficeAIPatch.cs
namespace RealTime.Patches.BuildingAIPatches
{
    using HarmonyLib;
    using RealTime.AI;
    using System.Reflection;

    [HarmonyPatch(typeof(PostOfficeAI))]
    public static class PostOfficeAIPatch
    {
        private delegate void PlayerBuildingAICreateBuildingDelegate(PlayerBuildingAI __instance, ushort buildingID, ref Building data);
        private static readonly PlayerBuildingAICreateBuildingDelegate BaseCreateBuilding = AccessTools.MethodDelegate<PlayerBuildingAICreateBuildingDelegate>(typeof(PlayerBuildingAI).GetMethod("CreateBuilding", BindingFlags.Instance | BindingFlags.Public), null, false);

        private delegate void PlayerBuildingAIBuildingLoadedDelegate(PlayerBuildingAI __instance, ushort buildingID, ref Building data, uint version);
        private static readonly PlayerBuildingAIBuildingLoadedDelegate BaseBuildingLoaded = AccessTools.MethodDelegate<PlayerBuildingAIBuildingLoadedDelegate>(typeof(PlayerBuildingAI).GetMethod("BuildingLoaded", BindingFlags.Instance | BindingFlags.Public), null, false);

        private delegate void PlayerBuildingAIEndRelocatingDelegate(PlayerBuildingAI __instance, ushort buildingID, ref Building data);
        private static readonly PlayerBuildingAIEndRelocatingDelegate BaseEndRelocating = AccessTools.MethodDelegate<PlayerBuildingAIEndRelocatingDelegate>(typeof(PlayerBuildingAI).GetMethod("EndRelocating", BindingFlags.Instance | BindingFlags.Public), null, false);

        [HarmonyPatch(typeof(PostOfficeAI), "CreateBuilding")]
        [HarmonyPrefix]
        public static bool CreateBuilding(PostOfficeAI __instance, ushort buildingID, ref Building data)
        {
            if (data.Info.GetAI() is ExtendedPostOfficeAI)
            {
                BaseCreateBuilding(__instance, buildingID, ref data);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PostOfficeAI), "BuildingLoaded")]
        [HarmonyPrefix]
        public static bool BuildingLoaded(PostOfficeAI __instance, ushort buildingID, ref Building data, uint version)
        {
            if (data.Info.GetAI() is ExtendedPostOfficeAI)
            {
                BaseBuildingLoaded(__instance, buildingID, ref data, version);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PostOfficeAI), "EndRelocating")]
        [HarmonyPrefix]
        public static bool EndRelocating(PostOfficeAI __instance, ushort buildingID, ref Building data)
        {
            if (data.Info.GetAI() is ExtendedPostOfficeAI)
            {
                BaseEndRelocating(__instance, buildingID, ref data);
                return false;
            }
            return true;
        }
    }
}
