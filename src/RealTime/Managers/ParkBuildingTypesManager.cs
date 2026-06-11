namespace RealTime.Managers
{
    using System.Collections.Generic;
    using ColossalFramework.UI;
    using RealTime.CustomAI;
    using RealTime.Localization;
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
    }
}
