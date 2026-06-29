namespace RealTime.Integration
{
    using RealTime.Config;

    public static  class RealTimeBridge
    {
        /// <summary>Gets or sets the mod configuration.</summary>
        public static RealTimeConfig RealTimeConfig { get; set; }

        /// <summary>The integration contract version; also confirms the API is present.</summary>
        /// v1: + get school operation hours
        public const int ApiVersion = 1;

        /// <summary>Returns <see cref="ApiVersion"/>.</summary>
        /// <remarks>Reflection contract: "GetApiVersion() : int32".</remarks>
        public static int GetApiVersion() => ApiVersion;

        /// <summary>
        /// Gets school operation hours.
        /// </summary>
        /// <param name="schoolStartHour">The school start hour.</param>
        /// <param name="schoolEndHour">The school end hour.</param>
        public static void GetSchoolOperationHours(out float schoolStartHour, out float schoolEndHour)
        {
            schoolStartHour = RealTimeConfig.SchoolBegin;
            schoolEndHour = RealTimeConfig.SchoolEnd;
        }
    }
}
