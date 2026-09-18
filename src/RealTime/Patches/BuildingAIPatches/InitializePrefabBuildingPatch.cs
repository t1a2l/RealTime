namespace RealTime.Patches.BuildingAIPatches
{
    using HarmonyLib;
    using RealTime.AI;
    using RealTime.Utils;
    using UnityEngine;

    [HarmonyPatch]
    public static class InitializePrefabBuildingPatch
    {
        [HarmonyPatch(typeof(BuildingInfo), "InitializePrefab")]
        [HarmonyPrefix]
        public static void xxx(BuildingInfo __instance)
        {
            var oldAI = __instance.GetComponent<PrefabAI>();
            if (__instance.m_class.m_service == ItemClass.Service.PublicTransport && __instance.m_class.m_subService == ItemClass.SubService.PublicTransportPost && oldAI is not ExtendedPostOfficeAI)
            {
                var newAI = (PrefabAI)__instance.gameObject.AddComponent<ExtendedPostOfficeAI>();
                PrefabUtils.TryCopyAttributes(oldAI, newAI, false);
                Object.DestroyImmediate(oldAI);
                if (newAI is ExtendedPostOfficeAI extendedPostOfficeAI)
                {
                    if (__instance.name.Contains("Post Office 01"))
                    {
                        extendedPostOfficeAI.m_visitPlaceCount0 = 10;
                        extendedPostOfficeAI.m_visitPlaceCount1 = 10;
                        extendedPostOfficeAI.m_visitPlaceCount2 = 10;
                    }
                    else
                    {
                        if(extendedPostOfficeAI.m_visitPlaceCount0 == 0)
                        {
                            extendedPostOfficeAI.m_visitPlaceCount0 = 10;
                        }
                        if (extendedPostOfficeAI.m_visitPlaceCount1 == 0)
                        {
                            extendedPostOfficeAI.m_visitPlaceCount1 = 10;
                        }
                        if (extendedPostOfficeAI.m_visitPlaceCount2 == 0)
                        {
                            extendedPostOfficeAI.m_visitPlaceCount2 = 10;
                        }
                    }
                }
            }

            if (__instance.m_class.m_service == ItemClass.Service.PoliceDepartment && __instance.m_class.m_subService == ItemClass.SubService.PoliceDepartmentBank && oldAI is not ExtendedBankOfficeAI)
            {
                var newAI = (PrefabAI)__instance.gameObject.AddComponent<ExtendedBankOfficeAI>();
                PrefabUtils.TryCopyAttributes(oldAI, newAI, false);
                Object.DestroyImmediate(oldAI);
                if (newAI is ExtendedBankOfficeAI extendedBankOfficeAI)
                {
                    if (__instance.name.Contains("Bank 01"))
                    {
                        extendedBankOfficeAI.m_visitPlaceCount0 = 10;
                        extendedBankOfficeAI.m_visitPlaceCount1 = 10;
                        extendedBankOfficeAI.m_visitPlaceCount2 = 10;
                    }
                    else if (__instance.name.Contains("Bank 02"))
                    {
                        extendedBankOfficeAI.m_visitPlaceCount0 = 20;
                        extendedBankOfficeAI.m_visitPlaceCount1 = 20;
                        extendedBankOfficeAI.m_visitPlaceCount2 = 20;
                    }
                    else if (__instance.name.Contains("Bank 03"))
                    {
                        extendedBankOfficeAI.m_visitPlaceCount0 = 30;
                        extendedBankOfficeAI.m_visitPlaceCount1 = 30;
                        extendedBankOfficeAI.m_visitPlaceCount2 = 30;
                    }
                    else
                    {
                        if (extendedBankOfficeAI.m_visitPlaceCount0 == 0)
                        {
                            extendedBankOfficeAI.m_visitPlaceCount0 = 10;
                        }
                        if (extendedBankOfficeAI.m_visitPlaceCount1 == 0)
                        {
                            extendedBankOfficeAI.m_visitPlaceCount1 = 10;
                        }
                        if (extendedBankOfficeAI.m_visitPlaceCount2 == 0)
                        {
                            extendedBankOfficeAI.m_visitPlaceCount2 = 10;
                        }
                    }
                }
            }
        }
    }
}
