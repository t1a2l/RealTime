namespace RealTime.CustomAI
{
    using System;

    [Flags]
    internal enum CommercialBuildingType
    {
        None = 0,            // 0000
        Shopping = 1 << 0,     // 0001 (1)
        Entertainment = 1 << 1,// 0010 (2)
        Food = 1 << 2,          // 0100 (4)
    }
}
