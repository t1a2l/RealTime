namespace RealTime.Patches
{
    using HarmonyLib;
    using RealTime.CustomAI;

    /// <summary>
    /// Refreshes building shift restrictions after policy changes.
    /// </summary>
    [HarmonyPatch]
    internal static class DistrictManagerPatch
    {
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        [HarmonyPatch(typeof(DistrictManager), "SetCityPolicy")]
        [HarmonyPostfix]
        private static void SetCityPolicyPostfix(DistrictPolicies.Policies policy) => RealTimeBuildingAI.RefreshPolicyRestrictions(policy, RealTimeBuildingAI.PolicyScope.City);

        [HarmonyPatch(typeof(DistrictManager), "UnsetCityPolicy")]
        [HarmonyPostfix]
        private static void UnsetCityPolicyPostfix(DistrictPolicies.Policies policy) => RealTimeBuildingAI.RefreshPolicyRestrictions(policy, RealTimeBuildingAI.PolicyScope.City);

        [HarmonyPatch(typeof(DistrictManager), "SetDistrictPolicy")]
        [HarmonyPostfix]
        private static void SetDistrictPolicyPostfix(DistrictPolicies.Policies policy, byte district) => RealTimeBuildingAI.RefreshPolicyRestrictions(policy, RealTimeBuildingAI.PolicyScope.District, district);

        [HarmonyPatch(typeof(DistrictManager), "UnsetDistrictPolicy")]
        [HarmonyPostfix]
        private static void UnsetDistrictPolicyPostfix(DistrictPolicies.Policies policy, byte district) => RealTimeBuildingAI.RefreshPolicyRestrictions(policy, RealTimeBuildingAI.PolicyScope.District, district);

        [HarmonyPatch(typeof(DistrictManager), "SetParkPolicy")]
        [HarmonyPostfix]
        private static void SetParkPolicyPostfix(DistrictPolicies.Policies policy, byte park) => RealTimeBuildingAI.RefreshPolicyRestrictions(policy, RealTimeBuildingAI.PolicyScope.Park, park);

        [HarmonyPatch(typeof(DistrictManager), "UnsetParkPolicy")]
        [HarmonyPostfix]
        private static void UnsetParkPolicyPostfix(DistrictPolicies.Policies policy, byte park) => RealTimeBuildingAI.RefreshPolicyRestrictions(policy, RealTimeBuildingAI.PolicyScope.Park, park);
    }
}
