// BuildingAIPatch.cs

namespace RealTime.Patches.BuildingAIPatches
{
    using ColossalFramework.Math;
    using HarmonyLib;
    using RealTime.CustomAI;
    using RealTime.Simulation;
    using UnityEngine;

    /// <summary>
    /// A static class that provides the patch objects for the building AI game methods.
    /// </summary>
    ///
    [HarmonyPatch]
    internal static class BuildingAIPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeBuildingAI { get; set; }

        /// <summary>Gets or sets the weather information service.</summary>
        public static IWeatherInfo WeatherInfo { get; set; }

        [HarmonyPatch(typeof(BuildingAI), "CalculateUnspawnPosition",
                [typeof(ushort), typeof(Building), typeof(Randomizer), typeof(CitizenInfo), typeof(ushort), typeof(Vector3), typeof(Vector3), typeof(Vector2), typeof(CitizenInstance.Flags)],
                [ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out])]
        [HarmonyPostfix]
        private static void CalculateUnspawnPosition(BuildingAI __instance, ushort buildingID, ref Building data, ref Randomizer randomizer, CitizenInfo info, ref Vector3 position, ref Vector3 target, ref CitizenInstance.Flags specialFlags)
        {
            if (WeatherInfo == null || WeatherInfo != null && !WeatherInfo.IsBadWeather || data.Info == null || data.Info.m_enterDoors == null)
            {
                return;
            }

            var enterDoors = data.Info.m_enterDoors;
            bool doorFound = false;
            for (int i = 0; i < enterDoors.Length; ++i)
            {
                var prop = enterDoors[i].m_finalProp;
                if (prop == null)
                {
                    continue;
                }

                if (prop.m_doorType == PropInfo.DoorType.Enter || prop.m_doorType == PropInfo.DoorType.Both)
                {
                    doorFound = true;
                    break;
                }
            }

            if (!doorFound)
            {
                return;
            }

            __instance.CalculateSpawnPosition(buildingID, ref data, ref randomizer, info, out var spawnPosition, out var spawnTarget);

            position = spawnPosition;
            target = spawnTarget;
            specialFlags &= ~(CitizenInstance.Flags.HangAround | CitizenInstance.Flags.SittingDown);
        }

    }
}

