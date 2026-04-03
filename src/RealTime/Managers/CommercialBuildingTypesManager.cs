namespace RealTime.Managers
{
    using System.Collections.Generic;
    using ColossalFramework.UI;
    using RealTime.CustomAI;
    using RealTime.GameConnection;
    using RealTime.Utils.UIUtils;
    using UnityEngine;

    internal static class CommercialBuildingTypesManager
    {
        internal static Dictionary<ushort, CommercialBuildingType> CommercialBuildingTypes;

        internal static void Init() => CommercialBuildingTypes ??= [];

        internal static void Deinit() => CommercialBuildingTypes = [];

        internal static bool CommercialBuildingTypeExist(ushort buildingID) => CommercialBuildingTypes.ContainsKey(buildingID);

        internal static CommercialBuildingType GetCommercialBuildingType(ushort buildingID) => !CommercialBuildingTypes.TryGetValue(buildingID, out var commercialBuildingType) ? default : commercialBuildingType;

        internal static void CreateCommercialBuildingType(ushort buildingID, CommercialBuildingType type)
        {
            if (!CommercialBuildingTypes.TryGetValue(buildingID, out _))
            {
                CommercialBuildingTypes.Add(buildingID, type);
            }
        }

        internal static void SetCommercialBuildingType(ushort buildingID, CommercialBuildingType type) => CommercialBuildingTypes[buildingID] = type;

        internal static void RemoveCommercialBuildingType(ushort buildingID) => CommercialBuildingTypes.Remove(buildingID);

        internal static void CommercialBuildingTypeDropdownVisibility(ushort buildingID, ref UIDropDown panel,  ref bool isUpdating)
        {
            if (BuildingManagerConnection.IsAllowedCommercialBuildingType(buildingID) && CommercialBuildingTypeExist(buildingID))
            {
                int commercialBuildingTypeIndex = ConvertFlagsToIndex(GetCommercialBuildingType(buildingID));
                if (commercialBuildingTypeIndex != panel.selectedIndex)
                {
                    isUpdating = true;
                    panel.selectedIndex = commercialBuildingTypeIndex;
                    isUpdating = false;
                }
                panel.Show();
            }
            else
            {
                panel.Hide();
            }
        }

        internal static void CreateUI(UIComponent parent, ref UIDropDown panel, float xPos, float yPos)
        {
            var m_zonedBuildingWorldInfoPanel = UIView.library.Get<ZonedBuildingWorldInfoPanel>("ZonedBuildingWorldInfoPanel");

            if (m_zonedBuildingWorldInfoPanel == null)
            {
                return;
            }

            if (panel == null)
            {
                panel = UIDropDowns.AddLabelledDropDown(parent, xPos, yPos, "CommercialBuildingTypeDropdown", "Store Type", "select commercial building store type", 220f, 24f);
                panel.textColor = new Color32(255, 255, 255, 255);
                panel.disabledTextColor = new Color32(142, 142, 142, 255);
                panel.items = [
                    "Shopping",                       // Index 0
                        "Entertainment",                  // Index 1
                        "Food",                           // Index 2
                        "Shopping & Entertainment",       // Index 3
                        "Shopping & Food",                // Index 4
                        "Entertainment & Food",           // Index 5
                        "All"                             // Index 6
                ];
                panel.Hide();
            }
        }

        internal static void OnCommercialBuildingTypeDropdownIndexChanged(int value, ushort buildingID, bool isUpdating)
        {
            if (isUpdating)
            {
                return;
            }

            if (buildingID == 0)
            {
                return;
            }

            SetCommercialBuildingType(buildingID, ConvertIndexToFlags(value));
        }

        internal static CommercialBuildingType ConvertIndexToFlags(int index) => index switch
        {
            0 => CommercialBuildingType.Shopping,
            1 => CommercialBuildingType.Entertainment,
            2 => CommercialBuildingType.Food,

            // On the fly combination using bitwise OR!
            3 => CommercialBuildingType.Shopping | CommercialBuildingType.Entertainment,
            4 => CommercialBuildingType.Shopping | CommercialBuildingType.Food,
            5 => CommercialBuildingType.Entertainment | CommercialBuildingType.Food,
            6 => CommercialBuildingType.Shopping | CommercialBuildingType.Entertainment | CommercialBuildingType.Food,

            // Fallback safety
            _ => CommercialBuildingType.Shopping
        };

        internal static int ConvertFlagsToIndex(CommercialBuildingType type) => type switch
        {
            CommercialBuildingType.Shopping => 0,
            CommercialBuildingType.Entertainment => 1,
            CommercialBuildingType.Food => 2,
            CommercialBuildingType.Shopping | CommercialBuildingType.Entertainment => 3,
            CommercialBuildingType.Shopping | CommercialBuildingType.Food => 4,
            CommercialBuildingType.Entertainment | CommercialBuildingType.Food => 5,
            CommercialBuildingType.Shopping | CommercialBuildingType.Entertainment | CommercialBuildingType.Food => 6,
            _ => 0,
        };
    }
}
