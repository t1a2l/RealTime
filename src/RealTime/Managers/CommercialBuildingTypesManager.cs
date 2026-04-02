namespace RealTime.Managers
{
    using System.Collections.Generic;
    using RealTime.CustomAI;

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
    }
}
