namespace RealTime.Managers
{
    using System;
    using System.Collections.Generic;
    using ColossalFramework.UI;
    using RealTime.CustomAI;
    using RealTime.Localization;
    using RealTime.Simulation;
    using RealTime.Utils.UIUtils;
    using SkyTools.Localization;
    using UnityEngine;

    internal static class ParkBuildingTypesManager
    {
        internal static Dictionary<ushort, ParkBuildingType> ParkBuildingTypes;

        internal static void Init() => ParkBuildingTypes ??= [];

        internal static void Deinit() => ParkBuildingTypes = [];

        internal static bool ParkBuildingTypeExist(ushort buildingID) => ParkBuildingTypes.ContainsKey(buildingID);

        internal static ParkBuildingType GetParkBuildingType(ushort buildingID) => !ParkBuildingTypes.TryGetValue(buildingID, out var parkBuildingType) ? default : parkBuildingType;

        internal static void CreateParkBuildingType(ushort buildingID, ParkBuildingType type)
        {
            if (!ParkBuildingTypes.TryGetValue(buildingID, out _))
            {
                ParkBuildingTypes.Add(buildingID, type);
            }
        }

        internal static void SetParkBuildingType(ushort buildingID, ParkBuildingType type) => ParkBuildingTypes[buildingID] = type;

        internal static void RemoveParkBuildingType(ushort buildingID) => ParkBuildingTypes.Remove(buildingID);

        internal static void CreateUI(UIComponent parent, ref UIDropDown panel, float xPos, float yPos, ILocalizationProvider LocalizationProvider)
        {
            if (panel == null)
            {
                panel = UIDropDowns.AddLabelledDropDown(parent, xPos, yPos, "ParkBuildingTypeDropdown", LocalizationProvider.Translate(TranslationKeys.ParkBuildingTypeLabel), LocalizationProvider.Translate(TranslationKeys.ParkBuildingTypeTooltip), 220f, 24f);
                panel.textColor = new Color32(255, 255, 255, 255);
                panel.disabledTextColor = new Color32(142, 142, 142, 255);
                panel.items = [
                    LocalizationProvider.Translate(TranslationKeys.ParkBuildingTypeGeneric),
                    LocalizationProvider.Translate(TranslationKeys.ParkBuildingTypePlayground),
                    LocalizationProvider.Translate(TranslationKeys.ParkBuildingTypeDogPark),
                    LocalizationProvider.Translate(TranslationKeys.ParkBuildingTypePlaza),
                    LocalizationProvider.Translate(TranslationKeys.ParkBuildingTypeGarden),
                    LocalizationProvider.Translate(TranslationKeys.ParkBuildingTypeSports)
                ];
                panel.Hide();
            }
        }

        internal static void OnParkBuildingTypeDropdownIndexChanged(int value, ushort buildingID, bool isUpdating)
        {
            if (isUpdating)
            {
                return;
            }

            if (buildingID == 0)
            {
                return;
            }

            SetParkBuildingType(buildingID, (ParkBuildingType)value);
        }

        internal static void UpdateParkBuildingTypeDropdown(UIDropDown panel, ushort buildingID, ref bool isUpdating)
        {
            if (panel == null)
            {
                return;
            }
            if (buildingID == 0)
            {
                panel.Hide();
                return;
            }
            if (!ParkBuildingTypeExist(buildingID))
            {
                panel.Hide();
                return;
            }
            var parkBuildingType = GetParkBuildingType(buildingID);

            if(panel.selectedIndex != (int)parkBuildingType)
            {
                isUpdating = true;
                panel.selectedIndex = (int)parkBuildingType;
                isUpdating = false;
            }

            panel.Show();
        }

        internal static ParkBuildingType GetPreferredParkType(Citizen.AgeGroup age, IRandomizer Random)
        {
            const uint scale = 100u;

            uint generic = (uint)Math.Max(1, Math.Round(GetParkTypeWeight(age, ParkBuildingType.Generic) * scale));
            uint playground = (uint)Math.Max(1, Math.Round(GetParkTypeWeight(age, ParkBuildingType.Playground) * scale));
            uint dogPark = (uint)Math.Max(1, Math.Round(GetParkTypeWeight(age, ParkBuildingType.DogPark) * scale));
            uint plaza = (uint)Math.Max(1, Math.Round(GetParkTypeWeight(age, ParkBuildingType.Plaza) * scale));
            uint garden = (uint)Math.Max(1, Math.Round(GetParkTypeWeight(age, ParkBuildingType.Garden) * scale));
            uint sports = (uint)Math.Max(1, Math.Round(GetParkTypeWeight(age, ParkBuildingType.Sports) * scale));

            uint total = generic + playground + dogPark + plaza + garden + sports;
            uint random = (uint)Random.GetRandomValue(total - 1);

            if (random < generic)
            {
                return ParkBuildingType.Generic;
            }

            random -= generic;

            if (random < playground)
            {
                return ParkBuildingType.Playground;
            }

            random -= playground;

            if (random < dogPark)
            {
                return ParkBuildingType.DogPark;
            }

            random -= dogPark;

            if (random < plaza)
            {
                return ParkBuildingType.Plaza;
            }

            random -= plaza;

            if (random < garden)
            {
                return ParkBuildingType.Garden;
            }

            random -= garden;

            return ParkBuildingType.Sports;
        }

        internal static float GetParkTypeWeight(Citizen.AgeGroup age, ParkBuildingType parkType)
        {
            float weight = parkType switch
            {
                ParkBuildingType.Generic => 1f,

                ParkBuildingType.Playground => age switch
                {
                    Citizen.AgeGroup.Child => 3f,
                    Citizen.AgeGroup.Teen => 0.5f,
                    Citizen.AgeGroup.Young => 0.3f,
                    Citizen.AgeGroup.Adult => 0.1f,
                    Citizen.AgeGroup.Senior => 0.2f,
                    _ => 1f
                },

                ParkBuildingType.DogPark => age switch
                {
                    Citizen.AgeGroup.Child => 0.5f,
                    Citizen.AgeGroup.Teen => 0.7f,
                    Citizen.AgeGroup.Young => 1f,
                    Citizen.AgeGroup.Adult => 1.1f,
                    Citizen.AgeGroup.Senior => 0.9f,
                    _ => 1f
                },

                ParkBuildingType.Plaza => age switch
                {
                    Citizen.AgeGroup.Child => 0.3f,
                    Citizen.AgeGroup.Teen => 1.15f,
                    Citizen.AgeGroup.Young => 1.5f,
                    Citizen.AgeGroup.Adult => 1.2f,
                    Citizen.AgeGroup.Senior => 0.6f,
                    _ => 1f
                },

                ParkBuildingType.Garden => age switch
                {
                    Citizen.AgeGroup.Child => 0.8f,
                    Citizen.AgeGroup.Teen => 0.75f,
                    Citizen.AgeGroup.Young => 0.95f,
                    Citizen.AgeGroup.Adult => 1.1f,
                    Citizen.AgeGroup.Senior => 1.8f,
                    _ => 1f
                },

                ParkBuildingType.Sports => age switch
                {
                    Citizen.AgeGroup.Child => 0.4f,
                    Citizen.AgeGroup.Teen => 1.8f,
                    Citizen.AgeGroup.Young => 1.8f,
                    Citizen.AgeGroup.Adult => 1.15f,
                    Citizen.AgeGroup.Senior => 0.3f,
                    _ => 1f
                },

                _ => 1f
            };

            return Math.Min(weight, 3f);
        }
    }
}
