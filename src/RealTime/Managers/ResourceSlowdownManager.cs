namespace RealTime.Managers
{
    using System.Collections.Generic;
    using RealTime.Core;

    public static class ResourceSlowdownManager
    {
        public static readonly float[] GarbageAccumulator = new float[BuildingManager.MAX_BUILDING_COUNT];

        public static readonly float[] MailAccumulator = new float[BuildingManager.MAX_BUILDING_COUNT];

        public static readonly float[] CrimeAccumulator = new float[BuildingManager.MAX_BUILDING_COUNT];

        public static readonly HashSet<ushort> PendingCrimeDispatch = [];

        public static void ApplyGarbageSlowdown(ushort buildingID, ref Building buildingData, ushort garbageBefore, float multiplier = 1f)
        {
            ushort garbageProduced = (ushort)(buildingData.m_garbageBuffer - garbageBefore);
            if (garbageProduced == 0)
            {
                return;
            }

            float accumulated = GarbageAccumulator[buildingID];
            accumulated += garbageProduced * RealTimeMod.configProvider.Configuration.GarbageSlowDown * multiplier;

            ushort adjustedGarbage = (ushort)accumulated;
            GarbageAccumulator[buildingID] = accumulated - adjustedGarbage;

            buildingData.m_garbageBuffer = (ushort)(garbageBefore + adjustedGarbage);
        }

        public static void ApplyMailSlowdown(ushort buildingID, ref Building buildingData, ushort mailBefore, float multiplier = 1f)
        {
            ushort mailProduced = (ushort)(buildingData.m_mailBuffer - mailBefore);
            if (mailProduced == 0)
            {
                return;
            }

            float accumulated = MailAccumulator[buildingID];
            accumulated += mailProduced * RealTimeMod.configProvider.Configuration.MailSlowDown * multiplier;

            ushort adjustedMail = (ushort)accumulated;
            MailAccumulator[buildingID] = accumulated - adjustedMail;

            buildingData.m_mailBuffer = (ushort)(mailBefore + adjustedMail);
        }

        public static void ApplyCrimeSlowdown(ushort buildingID, ref Building buildingData, ushort crimeBefore)
        {
            ushort crimeProduced = (ushort)(buildingData.m_crimeBuffer - crimeBefore);
            if (crimeProduced == 0)
            {
                return;
            }

            float accumulated = CrimeAccumulator[buildingID];
            accumulated += crimeProduced * RealTimeMod.configProvider.Configuration.CrimeSlowDown;

            ushort adjustedCrime = (ushort)accumulated;
            CrimeAccumulator[buildingID] = accumulated - adjustedCrime;
            buildingData.m_crimeBuffer = (ushort)(crimeBefore + adjustedCrime);
        }
    }
}
