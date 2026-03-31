namespace RealTime.Managers
{
    using System;
    using System.Collections.Generic;

    internal static class CommercialBuildingTypesManager
    {
        internal static Dictionary<ushort, CommercialBuildingType> CommercialBuildingTypes;

        [Flags]
        internal enum CommercialBuildingType
        {
            None = 0,            // 0000
            Shopping = 1 << 0,     // 0001 (1)
            Entertainment = 1 << 1,// 0010 (2)
            Food = 1 << 2,          // 0100 (4)
        }

        internal static void Init() => CommercialBuildingTypes ??= [];

        internal static void Deinit() => CommercialBuildingTypes = [];

        internal static bool CommercialBuildingTypeExist(ushort buildingID) => CommercialBuildingTypes.ContainsKey(buildingID);

        internal static CommercialBuildingType GetCommercialBuildingType(ushort buildingID) => !CommercialBuildingTypes.TryGetValue(buildingID, out var burnTime) ? default : burnTime;

        internal static void CreateCommercialBuildingType(ushort buildingID, CommercialBuildingType type)
        {
            if (!CommercialBuildingTypes.TryGetValue(buildingID, out _))
            {
                CommercialBuildingTypes.Add(buildingID, type);
            }
        }

        internal static void SetCommercialBuildingType(ushort buildingID, CommercialBuildingType type) => CommercialBuildingTypes[buildingID] = type;

        internal static void RemoveCommercialBuildingType(ushort buildingID) => CommercialBuildingTypes.Remove(buildingID);
    }
}
