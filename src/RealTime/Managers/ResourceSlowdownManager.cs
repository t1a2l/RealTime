namespace RealTime.Managers
{
    using RealTime.Core;

    public static class ResourceSlowdownManager
    {
        public static readonly float[] GarbageAccumulator = new float[BuildingManager.MAX_BUILDING_COUNT];

        public static readonly float[] MailAccumulator = new float[BuildingManager.MAX_BUILDING_COUNT];

        public static void ApplyGarbageSlowdown(ushort buildingID, ref Building buildingData, ushort garbageBefore)
        {
            ushort garbageProduced = (ushort)(buildingData.m_garbageBuffer - garbageBefore);
            if (garbageProduced == 0)
            {
                return;
            }

            float accumulated = GarbageAccumulator[buildingID];
            accumulated += garbageProduced * RealTimeMod.configProvider.Configuration.GarbageSlowDown;

            ushort adjustedGarbage = (ushort)accumulated;
            GarbageAccumulator[buildingID] = accumulated - adjustedGarbage;

            buildingData.m_garbageBuffer = (ushort)(garbageBefore + adjustedGarbage);
        }

        public static void ApplyMailSlowdown(ushort buildingID, ref Building buildingData, ushort mailBefore)
        {
            ushort mailProduced = (ushort)(buildingData.m_mailBuffer - mailBefore);
            if (mailProduced == 0)
            {
                return;
            }

            float accumulated = MailAccumulator[buildingID];
            accumulated += mailProduced * RealTimeMod.configProvider.Configuration.MailSlowDown;

            ushort adjustedMail = (ushort)accumulated;
            MailAccumulator[buildingID] = accumulated - adjustedMail;

            buildingData.m_mailBuffer = (ushort)(mailBefore + adjustedMail);
        }

        public static void ModifyGarbageMaterialBuffer(ushort buildingID, ref int delta)
        {
            // Apply slowdown directly to the delta
            float accumulated = GarbageAccumulator[buildingID];
            accumulated += delta * RealTimeMod.configProvider.Configuration.GarbageSlowDown;

            int adjustedDelta = (int)accumulated;
            GarbageAccumulator[buildingID] = accumulated - adjustedDelta;

            delta = adjustedDelta;
        }

        public static void ModifyMailMaterialBuffer(ushort buildingID, ref int delta)
        {
            // Apply slowdown directly to the delta
            float accumulated = MailAccumulator[buildingID];
            accumulated += delta * RealTimeMod.configProvider.Configuration.MailSlowDown;

            int adjustedDelta = (int)accumulated;
            MailAccumulator[buildingID] = accumulated - adjustedDelta;

            delta = adjustedDelta;
        }
    }
}
