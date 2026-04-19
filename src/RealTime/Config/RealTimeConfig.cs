// RealTimeConfig.cs

namespace RealTime.Config
{
    using SkyTools.Configuration;
    using SkyTools.Tools;
    using SkyTools.UI;

    /// <summary>
    /// The mod's configuration.
    /// </summary>
    public sealed class RealTimeConfig : IConfiguration
    {
        /// <summary>The storage ID for the configuration objects.</summary>
        public const string StorageId = "RealTimeConfiguration";

        private const int LatestVersion = 3;

        /// <summary>Initializes a new instance of the <see cref="RealTimeConfig"/> class.</summary>
        public RealTimeConfig()
        {
            ResetToDefaults();
        }

        /// <summary>Initializes a new instance of the <see cref="RealTimeConfig"/> class.</summary>
        /// <param name="latestVersion">if set to <c>true</c>, the latest version of the configuration will be created.</param>
        public RealTimeConfig(bool latestVersion)
            : this()
        {
            if (latestVersion)
            {
                Version = LatestVersion;
            }
        }

        /// <summary>Gets or sets the version number of this configuration.</summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the speed of the time flow on daytime. Valid values are 1..7.
        /// </summary>
        [ConfigItem("1General", "0Time", 2)]
        [ConfigItemSlider(1, 6, ValueType = SliderValueType.Default)]
        public uint DayTimeSpeed { get; set; }

        /// <summary>
        /// Gets or sets the speed of the time flow on night time. Valid values are 1..7.
        /// </summary>
        [ConfigItem("1General", "0Time", 3)]
        [ConfigItemSlider(1, 6, ValueType = SliderValueType.Default)]
        public uint NightTimeSpeed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the dynamic day length is enabled.
        /// The dynamic day length depends on map's location and day of the year.
        /// </summary>
        [ConfigItem("1General", "0Time", 4)]
        [ConfigItemCheckBox]
        public bool IsDynamicDayLengthEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the weekends are enabled. Cims don't go to work on weekends.
        /// </summary>
        [ConfigItem("1General", "0Time", 5)]
        [ConfigItemCheckBox]
        public bool IsWeekendEnabled { get; set; }

        /// <summary>
        /// Gets or sets the virtual citizens mode.
        /// </summary>
        [ConfigItem("1General", "1Other", 0)]
        [ConfigItemComboBox]
        public VirtualCitizensLevel VirtualCitizens { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the citizens aging and birth rates must be slowed down.
        /// </summary>
        [ConfigItem("1General", "1Other", 1)]
        [ConfigItemCheckBox]
        public bool UseSlowAging { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the construction sites should pause at night time.
        /// </summary>
        [ConfigItem("1General", "1Other", 2)]
        [ConfigItemCheckBox]
        public bool StopConstructionAtNight { get; set; }

        /// <summary>
        /// Gets or sets the percentage value of the building construction speed. Valid values are 1..100.
        /// </summary>
        [ConfigItem("1General", "1Other", 3)]
        [ConfigItemSlider(1, 100)]
        public uint ConstructionSpeed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the inactive buildings should switch off the lights at night time.
        /// </summary>
        [ConfigItem("1General", "1Other", 4)]
        [ConfigItemCheckBox]
        public bool SwitchOffLightsAtNight { get; set; }

        /// <summary>
        /// Gets or sets the maximum height of a residential, commercial, or office building that will switch the lights off
        /// at night. All buildings higher than this value will not switch the lights off.
        /// </summary>
        [ConfigItem("1General", "1Other", 5)]
        [ConfigItemSlider(0, 100f, 5f, ValueType = SliderValueType.Default)]
        public float SwitchOffLightsMaxHeight { get; set; }

        /// <summary>Gets or sets a value indicating whether a citizen can abandon a journey when being too long in
        /// a traffic congestion or waiting too long for public transport.</summary>
        [ConfigItem("1General", "1Other", 6)]
        [ConfigItemCheckBox]
        public bool CanAbandonJourney { get; set; }

        /// <summary>Gets or sets a value indicating whether buildings will have RealisticFires</summary>
        [ConfigItem("1General", "1Other", 7)]
        [ConfigItemCheckBox]
        public bool RealisticFires { get; set; }

        /// <summary>Gets or sets a value indicating whether buildings will work without people inside</summary>
        [ConfigItem("1General", "1Other", 8)]
        [ConfigItemCheckBox]
        public bool WorkForceMatters { get; set; }

        /// <summary>garbage accumulation rate default per week</summary>
        [ConfigItem("1General", "1Other", 9)]
        [ConfigItemSlider(0.05f, 1.0f, 0.05f, ValueType = SliderValueType.Default)]
        public float GarbageSlowDown { get; set; }

        /// <summary>mail accumulation rate default per week</summary>
        [ConfigItem("1General", "1Other", 10)]
        [ConfigItemSlider(0.1f, 1.0f, 0.05f, ValueType = SliderValueType.Default)]
        public float MailSlowDown { get; set; }

        /// <summary>Gets or sets a value indicating whether a commerical building will receive goods delivery once a week</summary>
        [ConfigItem("1General", "1Other", 11)]
        [ConfigItemCheckBox]
        public bool WeeklyCommericalDeliveries { get; set; }

        /// <summary>Gets or sets a value indicating whether the spare time behavior has affect on the dummy traffic ai </summary>
        [ConfigItem("1General", "1Other", 12)]
        [ConfigItemCheckBox]
        public bool DummyTrafficBehavior { get; set; }

        /// <summary>
        /// Gets or sets a value that determines the percentage of the Cims that will work a second shift.
        /// Valid values are 1..8.
        /// </summary>
        [ConfigItem("2Quotas", 0)]
        [ConfigItemSlider(1, 25)]
        public uint SecondShiftQuota { get; set; }

        /// <summary>
        /// Gets or sets a value that determines the percentage of the Cims that will work a night shift.
        /// Valid values are 1..8.
        /// </summary>
        [ConfigItem("2Quotas", 1)]
        [ConfigItemSlider(1, 25)]
        public uint NightShiftQuota { get; set; }

        /// <summary>
        /// Gets or sets a value that determines the percentage of the Cims that will work a continuous night shift.
        /// Valid values are 1..8.
        /// </summary>
        [ConfigItem("2Quotas", 2)]
        [ConfigItemSlider(1, 25)]
        public uint ContinuousNightShiftQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of the Cims that will go out for breakfast before work or school.
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", 3)]
        [ConfigItemSlider(0, 100)]
        public uint BreakfastBeforeWorkOrSchoolQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of the Cims that will go out for lunch during work or school.
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", 4)]
        [ConfigItemSlider(0, 100)]
        public uint LunchDuringWorkOrSchoolQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of the Cims that will go out for supper after work or school.
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", 5)]
        [ConfigItemSlider(0, 100)]
        public uint SupperAfterWorkOrSchoolQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of the population that will search locally for buildings.
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", 6)]
        [ConfigItemSlider(0, 100)]
        public uint LocalBuildingSearchQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of the Cims that will go shopping just for fun without needing to buy something.
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", 7)]
        [ConfigItemSlider(0, 50)]
        public uint ShoppingForFunQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of the Cims that will go to and leave their work or school
        /// on time (no overtime!).
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", 8)]
        [ConfigItemSlider(0, 100)]
        public uint OnTimeQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of low commercial buildings that stay open at night
        /// on time (no overtime!).
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", 9)]
        [ConfigItemSlider(0, 100)]
        public uint OpenLowCommercialAtNightQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of commercial buildings that stay open at weekends
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", 10)]
        [ConfigItemSlider(0, 100)]
        public uint OpenCommercialSecondShiftQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of commercial buildings that stay open at weekends
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", 11)]
        [ConfigItemSlider(0, 100)]
        public uint OpenCommercialAtWeekendsQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of the Cims that will go to night class.
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", 12)]
        [ConfigItemSlider(0, 100)]
        public uint NightClassQuota { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the custom events are enabled.
        /// </summary>
        [ConfigItem("3Events", 0)]
        [ConfigItemCheckBox]
        public bool AreEventsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the earliest event on a week day can start.
        /// </summary>
        [ConfigItem("3Events", 1)]
        [ConfigItemSlider(0, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float EarliestHourEventStartWeekday { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the latest event on a week day can start.
        /// </summary>
        [ConfigItem("3Events", 2)]
        [ConfigItemSlider(0, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float LatestHourEventStartWeekday { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the earliest event on a Weekend day can start.
        /// </summary>
        [ConfigItem("3Events", 3)]
        [ConfigItemSlider(0, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float EarliestHourEventStartWeekend { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the latest event on a Weekend day can start.
        /// </summary>
        [ConfigItem("3Events", 4)]
        [ConfigItemSlider(0, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float LatestHourEventStartWeekend { get; set; }

        /// <summary>
        /// Gets or sets the duration of event preparation.
        /// </summary>
        [ConfigItem("3Events", 5)]
        [ConfigItemSlider(2f, 8f, 3f, ValueType = SliderValueType.Duration)]
        public float EventPreparationDuration { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the city wakes up.
        /// </summary>
        [ConfigItem("4Time", 0)]
        [ConfigItemSlider(4f, 8f, 0.25f, ValueType = SliderValueType.Time)]
        public float WakeUpHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the city goes to sleep.
        /// </summary>
        [ConfigItem("4Time", 1)]
        [ConfigItemSlider(20f, 23.75f, 0.25f, ValueType = SliderValueType.Time)]
        public float GoToSleepHour { get; set; }

        /// <summary>
        /// Gets or sets the work start daytime hour. The adult Cims must be at work.
        /// </summary>
        [ConfigItem("4Time", 2)]
        [ConfigItemSlider(4, 11, 0.25f, ValueType = SliderValueType.Time)]
        public float WorkBegin { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the adult Cims return from work.
        /// </summary>
        [ConfigItem("4Time", 3)]
        [ConfigItemSlider(12, 20, 0.25f, ValueType = SliderValueType.Time)]
        public float WorkEnd { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Cims should go out for breakfast before work or schoo.
        /// </summary>
        [ConfigItem("4Time", 4)]
        [ConfigItemCheckBox]
        public bool IsBreakfastTimeEnabledBeforeWorkOrSchool { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Cims should go out for lunch during work or school
        /// </summary>
        [ConfigItem("4Time", 5)]
        [ConfigItemCheckBox]
        public bool IsLunchTimeEnabledDuringWorkOrSchool { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Cims should go out for supper after work or school.
        /// </summary>
        [ConfigItem("4Time", 6)]
        [ConfigItemCheckBox]
        public bool IsSupperTimeEnabledAfterWorkOrSchool { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the Cims go out for lunch.
        /// </summary>
        [ConfigItem("4Time", 7)]
        [ConfigItemSlider(11, 13, 0.25f, ValueType = SliderValueType.Time)]
        public float LunchBegin { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the Cims return from lunch back to work.
        /// </summary>
        [ConfigItem("4Time", 8)]
        [ConfigItemSlider(13, 15, 0.25f, ValueType = SliderValueType.Time)]
        public float LunchEnd { get; set; }

        /// <summary>
        /// Gets or sets the maximum overtime for the Cims. They come to work earlier or stay at work longer for at most this
        /// amount of hours. This applies only for those Cims that are not on time, see <see cref="OnTimeQuota"/>.
        /// The young Cims (school and university) don't do overtime.
        /// </summary>
        [ConfigItem("4Time", 9)]
        [ConfigItemSlider(0, 4, 0.25f, ValueType = SliderValueType.Duration)]
        public float MaxOvertime { get; set; }

        /// <summary>
        /// Gets or sets the school start daytime hour. The young Cims must be at school or university.
        /// </summary>
        [ConfigItem("4Time", 10)]
        [ConfigItemSlider(4, 10, 0.25f, ValueType = SliderValueType.Time)]
        public float SchoolBegin { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the young Cims return from school or university.
        /// </summary>
        [ConfigItem("4Time", 11)]
        [ConfigItemSlider(11, 16, 0.25f, ValueType = SliderValueType.Time)]
        public float SchoolEnd { get; set; }

        /// <summary>
        /// Gets or sets the maximum vacation length in days.
        /// </summary>
        [ConfigItem("4Time", 12)]
        [ConfigItemSlider(0, 7, ValueType = SliderValueType.Default)]
        public uint MaxVacationLength { get; set; }

        /// <summary>
        /// Gets or sets the length of the academic year in hours.
        /// </summary>
        [ConfigItem("4Time", 13)]
        [ConfigItemSlider(1f, 30f, 1f, ValueType = SliderValueType.Default)]
        public float AcademicYearLength { get; set; }

        /// <summary>
        /// Gets or sets the length of a Toga party in hours.
        /// </summary>
        [ConfigItem("4Time", 14)]
        [ConfigItemSlider(4f, 24f, 1f, ValueType = SliderValueType.Default)]
        public float TogaPartyLength { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage service starts for residential buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 2)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float GarbageResidentialStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage service ends for residential buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 3)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float GarbageResidentialEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage service starts for commercial buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 4)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float GarbageCommercialStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage service ends for commercial buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 5)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float GarbageCommercialEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage service starts for industrial buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 6)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float GarbageIndustrialStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage service ends for industrial buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 7)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float GarbageIndustrialEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage service starts for office buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 8)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float GarbageOfficeStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage service ends for office buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 9)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float GarbageOfficeEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage service starts for other buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 10)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float GarbageOtherStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage service ends for other buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 11)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float GarbageOtherEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service starts for residential buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 0)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float MailResidentialStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service ends for residential buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 1)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float MailResidentialEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service starts for commercial buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 2)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float MailCommercialStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service ends for commercial buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 3)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float MailCommercialEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service starts for industrial buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 4)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float MailIndustrialStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service ends for industrial buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 5)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float MailIndustrialEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service starts for office buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 6)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float MailOfficeStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service ends for office buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 7)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float MailOfficeEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service starts for other buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 8)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float MailOtherStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service ends for other buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 9)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float MailOtherEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the park maintenance service starts.
        /// </summary>
        [ConfigItem("5Services", "2ParkMaintenance", 0)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float ParkMaintenanceStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the park maintenance service ends.
        /// </summary>
        [ConfigItem("5Services", "2ParkMaintenance", 1)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float ParkMaintenanceEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the maintenance and snow service starts for small roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 0)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float RoadMaintenanceRoadsSmallStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service ends for small roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 1)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float RoadMaintenanceRoadsSmallEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service starts for medium roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 2)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float RoadMaintenanceRoadsMediumStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service ends for medium roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 3)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float RoadMaintenanceRoadsMediumEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service starts for large roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 4)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float RoadMaintenanceRoadsLargeStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service ends for large roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 5)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float RoadMaintenanceRoadsLargeEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service starts for highway roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 6)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float RoadMaintenanceRoadsHighwayStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service ends for highway roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 7)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float RoadMaintenanceRoadsHighwayEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service starts for other roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 8)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float RoadMaintenanceRoadsOtherStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service ends for other roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 9)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float RoadMaintenanceRoadsOtherEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow service starts for small roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 0)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float SnowRoadsSmallStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow service ends for small roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 1)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float SnowRoadsSmallEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow service starts for medium roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 2)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float SnowRoadsMediumStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow service ends for medium roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 3)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float SnowRoadsMediumEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow service starts for large roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 4)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float SnowRoadsLargeStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow service ends for large roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 5)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float SnowRoadsLargeEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow service starts for highway roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 6)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float SnowRoadsHighwayStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow service ends for highway roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 7)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float SnowRoadsHighwayEndHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow service starts for other roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 8)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float SnowRoadsOtherStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow service ends for other roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 9)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float SnowRoadsOtherEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the mod should show the incompatibility notifications.
        /// </summary>
        [ConfigItem("Tools", 0)]
        [ConfigItemCheckBox]
        public bool ShowIncompatibilityNotifications { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the mod should use the English-US time and date formats, if the English language is selected.
        /// </summary>
        [ConfigItem("Tools", 1)]
        [ConfigItemCheckBox]
        public bool UseEnglishUSFormats { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether debug mod should be on or not.
        /// </summary>
        [ConfigItem("Tools", 2)]
        [ConfigItemCheckBox]
        public bool DebugMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether logging should be on or not.
        /// </summary>
        [ConfigItem("Tools", 3)]
        [ConfigItemCheckBox]
        public bool LoggingMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether advanced logging should be on or not.
        /// </summary>
        [ConfigItem("Tools", 4)]
        [ConfigItemCheckBox]
        public bool AdvancedLoggingMode { get; set; }

        /// <summary>Checks the version of the deserialized object and migrates it to the latest version when necessary.</summary>
        public void MigrateWhenNecessary()
        {
            if (Version == 0)
            {
                SecondShiftQuota = (uint)(SecondShiftQuota * 3.125f);
                NightShiftQuota = (uint)(NightShiftQuota * 3.125f);
            }

            Version = LatestVersion;
        }

        /// <summary>Validates this instance and corrects possible invalid property values.</summary>
        public void Validate()
        {
            WakeUpHour = FastMath.Clamp(WakeUpHour, 4f, 8f);
            GoToSleepHour = FastMath.Clamp(GoToSleepHour, 20f, 23.75f);

            DayTimeSpeed = FastMath.Clamp(DayTimeSpeed, 1u, 6u);
            NightTimeSpeed = FastMath.Clamp(NightTimeSpeed, 1u, 6u);

            VirtualCitizens = (VirtualCitizensLevel)FastMath.Clamp((int)VirtualCitizens, (int)VirtualCitizensLevel.None, (int)VirtualCitizensLevel.Vanilla);
            ConstructionSpeed = FastMath.Clamp(ConstructionSpeed, 1u, 100u);

            SwitchOffLightsMaxHeight = FastMath.Clamp(SwitchOffLightsMaxHeight, 0f, 100f);

            SecondShiftQuota = FastMath.Clamp(SecondShiftQuota, 1u, 25u);
            NightShiftQuota = FastMath.Clamp(NightShiftQuota, 1u, 25u);
            ContinuousNightShiftQuota = FastMath.Clamp(ContinuousNightShiftQuota, 1u, 25u);

            BreakfastBeforeWorkOrSchoolQuota = FastMath.Clamp(BreakfastBeforeWorkOrSchoolQuota, 0u, 100u);
            LunchDuringWorkOrSchoolQuota = FastMath.Clamp(LunchDuringWorkOrSchoolQuota, 0u, 100u);
            SupperAfterWorkOrSchoolQuota = FastMath.Clamp(SupperAfterWorkOrSchoolQuota, 0u, 100u);
            LocalBuildingSearchQuota = FastMath.Clamp(LocalBuildingSearchQuota, 0u, 100u);
            ShoppingForFunQuota = FastMath.Clamp(ShoppingForFunQuota, 0u, 50u);
            OnTimeQuota = FastMath.Clamp(OnTimeQuota, 0u, 100u);

            OpenLowCommercialAtNightQuota = FastMath.Clamp(OpenLowCommercialAtNightQuota, 0u, 100u);
            OpenCommercialSecondShiftQuota = FastMath.Clamp(OpenCommercialSecondShiftQuota, 0u, 100u);
            OpenCommercialAtWeekendsQuota = FastMath.Clamp(OpenCommercialAtWeekendsQuota, 0u, 100u);

            NightClassQuota = FastMath.Clamp(NightClassQuota, 0u, 100u);

            EarliestHourEventStartWeekday = FastMath.Clamp(EarliestHourEventStartWeekday, 0f, 23.5f);
            LatestHourEventStartWeekday = FastMath.Clamp(LatestHourEventStartWeekday, 0f, 23.5f);
            if (LatestHourEventStartWeekday < EarliestHourEventStartWeekday)
            {
                LatestHourEventStartWeekday = EarliestHourEventStartWeekday;
            }

            EarliestHourEventStartWeekend = FastMath.Clamp(EarliestHourEventStartWeekend, 0f, 23.5f);
            LatestHourEventStartWeekend = FastMath.Clamp(LatestHourEventStartWeekend, 0f, 23.5f);
            if (LatestHourEventStartWeekend < EarliestHourEventStartWeekend)
            {
                LatestHourEventStartWeekend = EarliestHourEventStartWeekend;
            }

            EventPreparationDuration = FastMath.Clamp(EventPreparationDuration, 2f, 8f);

            WorkBegin = FastMath.Clamp(WorkBegin, 4f, 11f);
            WorkEnd = FastMath.Clamp(WorkEnd, 12f, 20f);
            LunchBegin = FastMath.Clamp(LunchBegin, 11f, 13f);
            LunchEnd = FastMath.Clamp(LunchEnd, 13f, 15f);
            SchoolBegin = FastMath.Clamp(SchoolBegin, 4f, 10f);
            SchoolEnd = FastMath.Clamp(SchoolEnd, 11f, 16f);
            MaxOvertime = FastMath.Clamp(MaxOvertime, 0f, 4f);
            MaxVacationLength = FastMath.Clamp(MaxVacationLength, 0u, 7u);
            AcademicYearLength = FastMath.Clamp(AcademicYearLength, 1f, 30f);
            TogaPartyLength = FastMath.Clamp(TogaPartyLength, 4f, 24f);

            GarbageResidentialStartHour = FastMath.Clamp(GarbageResidentialStartHour, 0f, 23.5f);
            GarbageResidentialEndHour = FastMath.Clamp(GarbageResidentialEndHour, 0f, 23.5f);

            GarbageCommercialStartHour = FastMath.Clamp(GarbageCommercialStartHour, 0f, 23.5f);
            GarbageCommercialEndHour = FastMath.Clamp(GarbageCommercialEndHour, 0f, 23.5f);

            GarbageIndustrialStartHour = FastMath.Clamp(GarbageIndustrialStartHour, 0f, 23.5f);
            GarbageIndustrialEndHour = FastMath.Clamp(GarbageIndustrialEndHour, 0f, 23.5f);

            GarbageOfficeStartHour = FastMath.Clamp(GarbageOfficeStartHour, 0f, 23.5f);
            GarbageOfficeEndHour = FastMath.Clamp(GarbageOfficeEndHour, 0f, 23.5f);

            GarbageOtherStartHour = FastMath.Clamp(GarbageOtherStartHour, 0f, 23.5f);
            GarbageOtherEndHour = FastMath.Clamp(GarbageOtherEndHour, 0f, 23.5f);

            MailResidentialStartHour = FastMath.Clamp(MailResidentialStartHour, 0f, 23.5f);
            MailResidentialEndHour = FastMath.Clamp(MailResidentialEndHour, 0f, 23.5f);

            MailCommercialStartHour = FastMath.Clamp(MailCommercialStartHour, 0f, 23.5f);
            MailCommercialEndHour = FastMath.Clamp(MailCommercialEndHour,0f, 23.5f);

            MailIndustrialStartHour = FastMath.Clamp(MailIndustrialStartHour, 0f, 23.5f);
            MailIndustrialEndHour = FastMath.Clamp(MailIndustrialEndHour, 0f, 23.5f);

            MailOfficeStartHour = FastMath.Clamp(MailOfficeStartHour, 0f, 23.5f);
            MailOfficeEndHour = FastMath.Clamp(MailOfficeEndHour, 0f, 23.5f);

            MailOtherStartHour = FastMath.Clamp(MailOtherStartHour, 0f, 23.5f);
            MailOtherEndHour = FastMath.Clamp(MailOtherEndHour, 0f, 23.5f);

            ParkMaintenanceStartHour = FastMath.Clamp(ParkMaintenanceStartHour, 0f, 23.5f);
            ParkMaintenanceEndHour = FastMath.Clamp(ParkMaintenanceEndHour, 0f, 23.5f);

            RoadMaintenanceRoadsSmallStartHour = FastMath.Clamp(RoadMaintenanceRoadsSmallStartHour, 0f, 23.5f);
            RoadMaintenanceRoadsSmallEndHour = FastMath.Clamp(RoadMaintenanceRoadsSmallEndHour, 0f, 23.5f);

            RoadMaintenanceRoadsMediumStartHour = FastMath.Clamp(RoadMaintenanceRoadsMediumStartHour, 0f, 23.5f);
            RoadMaintenanceRoadsMediumEndHour = FastMath.Clamp(RoadMaintenanceRoadsMediumEndHour, 0f, 23.5f);
            RoadMaintenanceRoadsLargeStartHour = FastMath.Clamp(RoadMaintenanceRoadsLargeStartHour, 0f, 23.5f);
            RoadMaintenanceRoadsLargeEndHour = FastMath.Clamp(RoadMaintenanceRoadsLargeEndHour, 0f, 23.5f);

            RoadMaintenanceRoadsHighwayStartHour = FastMath.Clamp(RoadMaintenanceRoadsHighwayStartHour, 0f, 23.5f);
            RoadMaintenanceRoadsHighwayEndHour = FastMath.Clamp(RoadMaintenanceRoadsHighwayEndHour, 0f, 23.5f);

            RoadMaintenanceRoadsOtherStartHour = FastMath.Clamp(RoadMaintenanceRoadsOtherStartHour, 0f, 23.5f);
            RoadMaintenanceRoadsOtherEndHour = FastMath.Clamp(RoadMaintenanceRoadsOtherEndHour, 0f, 23.5f);

            SnowRoadsSmallStartHour = FastMath.Clamp(SnowRoadsSmallStartHour, 0f, 23.5f);
            SnowRoadsSmallEndHour = FastMath.Clamp(SnowRoadsSmallEndHour, 0f, 23.5f);

            SnowRoadsMediumStartHour = FastMath.Clamp(SnowRoadsMediumStartHour, 0f, 23.5f);
            SnowRoadsMediumEndHour = FastMath.Clamp(SnowRoadsMediumEndHour, 0f, 23.5f);
            SnowRoadsLargeStartHour = FastMath.Clamp(SnowRoadsLargeStartHour, 0f, 23.5f);
            SnowRoadsLargeEndHour = FastMath.Clamp(SnowRoadsLargeEndHour, 0f, 23.5f);

            SnowRoadsHighwayStartHour = FastMath.Clamp(SnowRoadsHighwayStartHour, 0f, 23.5f);
            SnowRoadsHighwayEndHour = FastMath.Clamp(SnowRoadsHighwayEndHour, 0f, 23.5f);

            SnowRoadsOtherStartHour = FastMath.Clamp(SnowRoadsOtherStartHour, 0f, 23.5f);
            SnowRoadsOtherEndHour = FastMath.Clamp(SnowRoadsOtherEndHour, 0f, 23.5f);
        }

        /// <summary>Resets all values to their defaults.</summary>
        public void ResetToDefaults()
        {
            WakeUpHour = 6f;
            GoToSleepHour = 22f;

            IsDynamicDayLengthEnabled = true;
            DayTimeSpeed = 4;
            NightTimeSpeed = 5;

            VirtualCitizens = VirtualCitizensLevel.Vanilla;
            UseSlowAging = true;
            IsWeekendEnabled = true;
            IsBreakfastTimeEnabledBeforeWorkOrSchool = true;
            IsLunchTimeEnabledDuringWorkOrSchool = true;
            IsSupperTimeEnabledAfterWorkOrSchool = false;

            StopConstructionAtNight = true;
            ConstructionSpeed = 50;
            SwitchOffLightsAtNight = true;
            SwitchOffLightsMaxHeight = 40f;
            CanAbandonJourney = true;
            RealisticFires = false;
            WorkForceMatters = false;
            GarbageSlowDown = 0.15f;
            MailSlowDown = 0.3f;
            WeeklyCommericalDeliveries = true;
            DummyTrafficBehavior = true;

            SecondShiftQuota = 13;
            NightShiftQuota = 6;
            ContinuousNightShiftQuota = 6;

            BreakfastBeforeWorkOrSchoolQuota = 20;
            LunchDuringWorkOrSchoolQuota = 80;
            SupperAfterWorkOrSchoolQuota = 20;
            LocalBuildingSearchQuota = 60;
            ShoppingForFunQuota = 30;
            OnTimeQuota = 80;
            OpenLowCommercialAtNightQuota = 10;
            OpenCommercialSecondShiftQuota = 50;
            OpenCommercialAtWeekendsQuota = 40;
            NightClassQuota = 10;

            AreEventsEnabled = true;
            EarliestHourEventStartWeekday = 16f;
            LatestHourEventStartWeekday = 20f;
            EarliestHourEventStartWeekend = 8f;
            LatestHourEventStartWeekend = 22f;
            EventPreparationDuration = 3f;

            WorkBegin = 9f;
            WorkEnd = 18f;
            LunchBegin = 12f;
            LunchEnd = 13f;
            MaxOvertime = 2f;
            SchoolBegin = 8f;
            SchoolEnd = 14f;
            MaxVacationLength = 3u;
            AcademicYearLength = 7f;
            TogaPartyLength = 8f;

            GarbageResidentialStartHour = 0f;
            GarbageResidentialEndHour = 0f;
            GarbageCommercialStartHour = 0f;
            GarbageCommercialEndHour = 0f;
            GarbageIndustrialStartHour = 0f;
            GarbageIndustrialEndHour = 0f;
            GarbageOfficeStartHour = 0f;
            GarbageOfficeEndHour = 0f;
            GarbageOtherStartHour = 0f;
            GarbageOtherEndHour = 0f;

            MailResidentialStartHour = 0f;
            MailResidentialEndHour = 0f;
            MailCommercialStartHour = 0f;
            MailCommercialEndHour = 0f;
            MailIndustrialStartHour = 0f;
            MailIndustrialEndHour = 0f;
            MailOfficeStartHour = 0f;
            MailOfficeEndHour = 0f;
            MailOtherStartHour = 0f;
            MailOtherEndHour = 0f;

            ParkMaintenanceStartHour = 0f;
            ParkMaintenanceEndHour = 0f;

            RoadMaintenanceRoadsSmallStartHour = 0f;
            RoadMaintenanceRoadsSmallEndHour = 0f;
            RoadMaintenanceRoadsMediumStartHour = 0f;
            RoadMaintenanceRoadsMediumEndHour = 0f;
            RoadMaintenanceRoadsLargeStartHour = 0f;
            RoadMaintenanceRoadsLargeEndHour = 0f;
            RoadMaintenanceRoadsHighwayStartHour = 0f;
            RoadMaintenanceRoadsHighwayEndHour = 0f;
            RoadMaintenanceRoadsOtherStartHour = 0f;
            RoadMaintenanceRoadsOtherEndHour = 0f;

            SnowRoadsSmallStartHour = 0f;
            SnowRoadsSmallEndHour = 0f;
            SnowRoadsMediumStartHour = 0f;
            SnowRoadsMediumEndHour = 0f;
            SnowRoadsLargeStartHour = 0f;
            SnowRoadsLargeEndHour = 0f;
            SnowRoadsHighwayStartHour = 0f;
            SnowRoadsHighwayEndHour = 0f;
            SnowRoadsOtherStartHour = 0f;
            SnowRoadsOtherEndHour = 0f;

            ShowIncompatibilityNotifications = true;
            DebugMode = false;
            LoggingMode = false;
            AdvancedLoggingMode = false;
        }
    }
}
