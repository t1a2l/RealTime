// RealTimeConfig.cs

namespace RealTime.Config
{
    using System;
    using System.Reflection;
    using ColossalFramework;
    using RealTime.Core;
    using RealTime.Managers;
    using SkyTools.Configuration;
    using SkyTools.Tools;
    using SkyTools.UI;

    /// <summary>
    /// The mod's configuration.
    /// </summary>
    public sealed class RealTimeConfig : IConfiguration
    {
        /// <summary>
        /// The storage ID for the configuration objects.
        /// </summary>
        public const string StorageId = "RealTimeConfiguration";

        private const int LatestVersion = 5;

        /// <summary>
        /// Initializes a new instance of the <see cref="RealTimeConfig"/> class.
        /// </summary>
        public RealTimeConfig()
        {
            ResetToDefaults();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RealTimeConfig"/> class.
        /// </summary>
        /// <param name="latestVersion">if set to <c>true</c>, the latest version of the configuration will be created.</param>
        public RealTimeConfig(bool latestVersion) : this()
        {
            if (latestVersion)
            {
                Version = LatestVersion;
            }
        }

        /// <summary>
        /// Gets or sets the version number of this configuration.
        /// </summary>
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
        public bool IsWeekendEnabled
        {
            get;
            set
            {
                if (field == value)
                {
                    return;
                }

                field = value;

                if (IsCityReady)
                {
                    OnWeekendEnabledChanged(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets this when the city is ready.
        /// </summary>
        internal static bool IsCityReady { get; set; }

        /// <summary>
        /// Apply when city is ready
        /// </summary>
        internal void ApplyWeekendSetting()
        {
            if (IsCityReady)
            {
                OnWeekendEnabledChanged(IsWeekendEnabled);
            }
        }

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

        /// <summary>
        /// Gets or sets a value indicating whether a citizen can abandon a journey when being too long in
        /// a traffic congestion or waiting too long for public transport.
        /// </summary>
        [ConfigItem("1General", "1Other", 6)]
        [ConfigItemCheckBox]
        public bool CanAbandonJourney { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether buildings will have RealisticFires.
        /// </summary>
        [ConfigItem("1General", "1Other", 7)]
        [ConfigItemCheckBox]
        public bool RealisticFires { get; set; }

        /// <summary>
        /// Gets or sets the value of slow down garbage accumulation.
        /// </summary>
        [ConfigItem("1General", "1Other", 8)]
        [ConfigItemSlider(0.05f, 1.0f, 0.05f, ValueType = SliderValueType.Default)]
        public float GarbageSlowDown { get; set; }

        /// <summary>
        /// Gets or sets the value of slow down mail accumulation.
        /// </summary>
        [ConfigItem("1General", "1Other", 9)]
        [ConfigItemSlider(0.1f, 1.0f, 0.05f, ValueType = SliderValueType.Default)]
        public float MailSlowDown { get; set; }

        /// <summary>
        /// Gets or sets the value of slow down crime accumulation.
        /// </summary>
        [ConfigItem("1General", "1Other", 10)]
        [ConfigItemSlider(0.1f, 1.0f, 0.05f, ValueType = SliderValueType.Default)]
        public float CrimeSlowDown { get; set; }

        /// /// <summary>
        /// Gets or sets a value indicating whether a commerical building will receive goods delivery once a week.
        /// </summary>
        [ConfigItem("1General", "1Other", 11)]
        [ConfigItemCheckBox]
        public bool WeeklyCommericalDeliveries { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the spare time behavior has affect on the dummy traffic AI.
        /// </summary>
        [ConfigItem("1General", "1Other", 12)]
        [ConfigItemCheckBox]
        public bool DummyTrafficBehavior { get; set; }

        /// <summary>
        /// Gets or sets the percentage of the Cims that will go to and leave their work or school.
        /// on time (no overtime!).
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", "0WorkAndSchoolQuotas", 0)]
        [ConfigItemSlider(0, 100)]
        public uint OnTimeQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of the Cims that will go to night class.
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", "0WorkAndSchoolQuotas", 1)]
        [ConfigItemSlider(0, 100)]
        public uint NightClassQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of the Cims that will go out for breakfast during work or school.
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", "0WorkAndSchoolQuotas", 2)]
        [ConfigItemSlider(0, 100)]
        public uint BreakfastDuringWorkOrSchoolQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of the Cims that will go out for lunch during work or school.
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", "0WorkAndSchoolQuotas", 3)]
        [ConfigItemSlider(0, 100)]
        public uint LunchDuringWorkOrSchoolQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of the Cims that will go out for supper during work or school.
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", "0WorkAndSchoolQuotas", 4)]
        [ConfigItemSlider(0, 100)]
        public uint SupperDuringWorkOrSchoolQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of the population that will search locally for buildings.
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", "1OtherQuotas", 0)]
        [ConfigItemSlider(0, 100)]
        public uint LocalBuildingSearchQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of the Cims that will go shopping just for fun without needing to buy something.
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", "1OtherQuotas", 1)]
        [ConfigItemSlider(0, 50)]
        public uint ShoppingForFunQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of low commercial buildings that stay open at night.
        /// on time (no overtime!).
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", "2BuildingQuotas", 0)]
        [ConfigItemSlider(0, 100)]
        public uint OpenLowCommercialAtNightQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of commercial buildings that will have a second shift.
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", "2BuildingQuotas", 1)]
        [ConfigItemSlider(0, 100)]
        public uint OpenCommercialSecondShiftQuota { get; set; }

        /// <summary>
        /// Gets or sets the percentage of commercial buildings that stay open at weekends.
        /// Valid values are 0..100.
        /// </summary>
        [ConfigItem("2Quotas", "2BuildingQuotas", 2)]
        [ConfigItemSlider(0, 100)]
        public uint OpenCommercialAtWeekendsQuota { get; set; }

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
        [ConfigItemSlider(0, 23.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(EarliestHourEventStartWeekday), MinOffset = 4f)]
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
        [ConfigItemSlider(0, 23.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(EarliestHourEventStartWeekend), MinOffset = 4f)]
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
        [ConfigItem("4Time", "0General", 0)]
        [ConfigItemSlider(4f, 8f, 0.25f, ValueType = SliderValueType.Time)]
        public float WakeUpHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the city goes to sleep.
        /// </summary>
        [ConfigItem("4Time", "0General", 1)]
        [ConfigItemSlider(20f, 23.75f, 0.25f, ValueType = SliderValueType.Time)]
        public float GoToSleepHour { get; set; }

        /// <summary>
        /// Gets or sets the maximum overtime for the Cims. They come to work earlier or stay at work longer for at most this
        /// amount of hours. This applies only for those Cims that are not on time, see <see cref="OnTimeQuota"/>.
        /// The young Cims (school and university) don't do overtime.
        /// </summary>
        [ConfigItem("4Time", "0General", 2)]
        [ConfigItemSlider(0, 4, 0.25f, ValueType = SliderValueType.Duration)]
        public float MaxOvertime { get; set; }

        /// <summary>
        /// Gets or sets the interval in days at which Cims visit the bank or post office.
        /// </summary>
        [ConfigItem("4Time", "0General", 3)]
        [ConfigItemSlider(1f, 7f, 1f, ValueType = SliderValueType.Default)]
        public float VisitBankOrPostOfficeInterval { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Cims should go out for breakfast during work or school.
        /// </summary>
        [ConfigItem("4Time", "1Meal", 0)]
        [ConfigItemCheckBox]
        public bool IsBreakfastTimeEnabledDuringWorkOrSchool { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether Cims should go out for lunch during work or school.
        /// </summary>
        [ConfigItem("4Time", "1Meal", 1)]
        [ConfigItemCheckBox]
        public bool IsLunchTimeEnabledDuringWorkOrSchool { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Cims should go out for supper during work or school.
        /// </summary>
        [ConfigItem("4Time", "1Meal", 2)]
        [ConfigItemCheckBox]
        public bool IsSupperTimeEnabledDuringWorkOrSchool { get; set; }

        /// <summary>
        /// Gets or sets the start daytime hour when the Cims go out for breakfast.
        /// </summary>
        [ConfigItem("4Time", "1Meal", 3)]
        [ConfigItemSlider(6f, 8f, 0.25f, ValueType = SliderValueType.Time)]
        public float BreakfastBegin { get; set; }

        /// <summary>
        /// Gets or sets the duration time of eating breakfast.
        /// </summary>
        [ConfigItem("4Time", "1Meal", 4)]
        [ConfigItemSlider(0.5f, 1.5f, 0.25f, ValueType = SliderValueType.Duration)]
        public float BreakfastDuration { get; set; }

        /// <summary>
        /// Gets or sets the end daytime hour when the Cims go out for breakfast.
        /// </summary>
        [ConfigItem("4Time", "1Meal", 5)]
        [ConfigItemSlider(8f, 10f, 0.25f, ValueType = SliderValueType.Time, MinFrom = nameof(BreakfastBegin), MinOffset = 2f)]
        public float BreakfastEnd { get; set; }

        /// <summary>
        /// Gets or sets the start daytime hour when the Cims go out for lunch.
        /// </summary>
        [ConfigItem("4Time", "1Meal", 6)]
        [ConfigItemSlider(11f, 13f, 0.25f, ValueType = SliderValueType.Time)]
        public float LunchBegin { get; set; }

        /// <summary>
        /// Gets or sets the duration time of eating lunch.
        /// </summary>
        [ConfigItem("4Time", "1Meal", 7)]
        [ConfigItemSlider(0.5f, 2f, 0.25f, ValueType = SliderValueType.Duration)]
        public float LunchDuration { get; set; }

        /// <summary>
        /// Gets or sets the end daytime hour when the Cims go out for lunch.
        /// </summary>
        [ConfigItem("4Time", "1Meal", 8)]
        [ConfigItemSlider(13f, 15f, 0.25f, ValueType = SliderValueType.Time, MinFrom = nameof(LunchBegin), MinOffset = 2f)]
        public float LunchEnd { get; set; }

        /// <summary>
        /// Gets or sets the start daytime hour when the Cims go out for supper.
        /// </summary>
        [ConfigItem("4Time", "1Meal", 9)]
        [ConfigItemSlider(17f, 19f, 0.25f, ValueType = SliderValueType.Time)]
        public float SupperBegin { get; set; }

        /// <summary>
        /// Gets or sets the duration time of eating supper.
        /// </summary>
        [ConfigItem("4Time", "1Meal", 10)]
        [ConfigItemSlider(0.5f, 2f, 0.25f, ValueType = SliderValueType.Duration)]
        public float SupperDuration { get; set; }

        /// <summary>
        /// Gets or sets the end daytime hour when the Cims go out for supper.
        /// </summary>
        [ConfigItem("4Time", "1Meal", 11)]
        [ConfigItemSlider(19f, 21f, 0.25f, ValueType = SliderValueType.Time, MinFrom = nameof(SupperBegin), MinOffset = 2f)]
        public float SupperEnd  { get; set; }

        /// <summary>
        /// Gets or sets the school start daytime hour. The young Cims must go at school or university.
        /// </summary>
        [ConfigItem("4Time", "2Education", 0)]
        [ConfigItemSlider(4, 10, 0.25f, ValueType = SliderValueType.Time)]
        public float SchoolBegin { get; set; }

        /// <summary>
        /// Gets or sets the school end daytime hour. The young Cims must return from school or university.
        /// </summary>
        [ConfigItem("4Time", "2Education", 1)]
        [ConfigItemSlider(11, 16, 0.25f, ValueType = SliderValueType.Time, MinFrom = nameof(SchoolBegin), MinOffset = 6f)]
        public float SchoolEnd { get; set; }

        /// <summary>
        /// Gets or sets the maximum vacation length in days.
        /// </summary>
        [ConfigItem("4Time", "2Education", 3)]
        [ConfigItemSlider(0, 7, ValueType = SliderValueType.Default)]
        public uint MaxVacationLength { get; set; }

        /// <summary>
        /// Gets or sets the length of the academic year in hours.
        /// </summary>
        [ConfigItem("4Time", "2Education", 4)]
        [ConfigItemSlider(1f, 30f, 1f, ValueType = SliderValueType.Default)]
        public float AcademicYearLength { get; set; }

        /// <summary>
        /// Gets or sets the length of a Toga party in hours.
        /// </summary>
        [ConfigItem("4Time", "2Education", 5)]
        [ConfigItemSlider(4f, 24f, 1f, ValueType = SliderValueType.Default)]
        public float TogaPartyLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for garbage collection at residential buildings.
        /// When set to <c>false</c>, garbage collection operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 0)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeGarbageResidential { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage collection starts for residential buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 1)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float GarbageResidentialStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage collection ends for residential buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 2)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(GarbageResidentialStartHour), MinOffset = 2f)]
        public float GarbageResidentialEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for garbage collection at commercial buildings.
        /// When set to <c>false</c>, garbage collection operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 3)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeGarbageCommercial { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage collection starts for commercial buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 4)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float GarbageCommercialStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage collection ends for commercial buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 5)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(GarbageCommercialStartHour), MinOffset = 2f)]
        public float GarbageCommercialEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for garbage collection at industrial buildings.
        /// When set to <c>false</c>, garbage collection operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 6)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeGarbageIndustrial { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage collection starts for industrial buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 7)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float GarbageIndustrialStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage collection ends for industrial buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 8)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(GarbageIndustrialStartHour), MinOffset = 2f)]
        public float GarbageIndustrialEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for garbage collection at office buildings.
        /// When set to <c>false</c>, garbage collection operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 9)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeGarbageOffice { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage collection starts for office buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 10)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float GarbageOfficeStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage collection ends for office buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 11)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(GarbageOfficeStartHour), MinOffset = 2f)]
        public float GarbageOfficeEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for garbage collection at other buildings.
        /// When set to <c>false</c>, garbage collection operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 12)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeGarbageOther { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage collection starts for other buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 13)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float GarbageOtherStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the garbage collection ends for other buildings.
        /// </summary>
        [ConfigItem("5Services", "0Garbage", 14)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(GarbageOtherStartHour), MinOffset = 2f)]
        public float GarbageOtherEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for mail service at residential buildings.
        /// When set to <c>false</c>, mail service operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 0)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeMailResidential { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service starts for residential buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 1)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float MailResidentialStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service ends for residential buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 2)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(MailResidentialStartHour), MinOffset = 2f)]
        public float MailResidentialEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for mail service at commercial buildings.
        /// When set to <c>false</c>, mail service operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 3)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeMailCommercial { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service starts for commercial buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 4)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float MailCommercialStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service ends for commercial buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 5)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(MailCommercialStartHour), MinOffset = 2f)]
        public float MailCommercialEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for mail service at industrial buildings.
        /// When set to <c>false</c>, mail service operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 6)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeMailIndustrial { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service starts for industrial buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 7)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float MailIndustrialStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service ends for industrial buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 8)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(MailIndustrialStartHour), MinOffset = 2f)]
        public float MailIndustrialEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for mail service at office buildings.
        /// When set to <c>false</c>, mail service operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 9)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeMailOffice { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service starts for office buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 10)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float MailOfficeStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service ends for office buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 11)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(MailOfficeStartHour), MinOffset = 2f)]
        public float MailOfficeEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for mail service at other buildings.
        /// When set to <c>false</c>, mail service operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 12)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeMailOther { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service starts for other buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 13)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float MailOtherStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the mail service ends for other buildings.
        /// </summary>
        [ConfigItem("5Services", "1Mail", 14)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(MailOtherStartHour), MinOffset = 2f)]
        public float MailOtherEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for park maintenance.
        /// When set to <c>false</c>, park maintenance operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "2ParkMaintenance", 0)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeParkMaintenance { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the park maintenance starts.
        /// </summary>
        [ConfigItem("5Services", "2ParkMaintenance", 1)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float ParkMaintenanceStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the park maintenance ends.
        /// </summary>
        [ConfigItem("5Services", "2ParkMaintenance", 2)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(ParkMaintenanceStartHour), MinOffset = 2f)]
        public float ParkMaintenanceEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for road maintenance on small roads.
        /// When set to <c>false</c>, road maintenance operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 0)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeRoadMaintenanceRoadsSmall { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service starts for small roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 1)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float RoadMaintenanceRoadsSmallStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service ends for small roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 2)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(RoadMaintenanceRoadsSmallStartHour), MinOffset = 2f)]
        public float RoadMaintenanceRoadsSmallEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for road maintenance on medium roads.
        /// When set to <c>false</c>, road maintenance operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 3)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeRoadMaintenanceRoadsMedium { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service starts for medium roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 4)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float RoadMaintenanceRoadsMediumStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service ends for medium roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 5)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(RoadMaintenanceRoadsMediumStartHour), MinOffset = 2f)]
        public float RoadMaintenanceRoadsMediumEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for road maintenance on large roads.
        /// When set to <c>false</c>, road maintenance operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 6)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeRoadMaintenanceRoadsLarge { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service starts for large roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 7)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float RoadMaintenanceRoadsLargeStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service ends for large roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 8)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(RoadMaintenanceRoadsLargeStartHour), MinOffset = 2f)]
        public float RoadMaintenanceRoadsLargeEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for road maintenance on highway roads.
        /// When set to <c>false</c>, road maintenance operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 9)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeRoadMaintenanceRoadsHighway { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service starts for highway roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 10)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float RoadMaintenanceRoadsHighwayStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service ends for highway roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 11)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(RoadMaintenanceRoadsHighwayStartHour), MinOffset = 2f)]
        public float RoadMaintenanceRoadsHighwayEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for road maintenance on other roads.
        /// When set to <c>false</c>, road maintenance operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 12)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeRoadMaintenanceRoadsOther { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service starts for other roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 13)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float RoadMaintenanceRoadsOtherStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the road maintenance service ends for other roads.
        /// </summary>
        [ConfigItem("5Services", "3RoadMaintenance", 14)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(RoadMaintenanceRoadsOtherStartHour), MinOffset = 2f)]
        public float RoadMaintenanceRoadsOtherEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for snow removal on small roads.
        /// When set to <c>false</c>, snow removal operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 0)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeSnowRoadsSmall { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow removal starts for small roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 1)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float SnowRoadsSmallStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow removal ends for small roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 2)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(SnowRoadsSmallStartHour), MinOffset = 2f)]
        public float SnowRoadsSmallEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for snow removal on medium roads.
        /// When set to <c>false</c>, snow removal operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 3)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeSnowRoadsMedium { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow removal starts for medium roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 4)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float SnowRoadsMediumStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow removal ends for medium roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 5)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(SnowRoadsMediumStartHour), MinOffset = 2f)]
        public float SnowRoadsMediumEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for snow removal on large roads.
        /// When set to <c>false</c>, snow removal operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 6)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeSnowRoadsLarge { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow removal starts for large roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 7)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float SnowRoadsLargeStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow removal ends for large roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 8)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(SnowRoadsLargeStartHour), MinOffset = 2f)]
        public float SnowRoadsLargeEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for snow removal on highway roads.
        /// When set to <c>false</c>, snow removal operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 9)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeSnowRoadsHighway { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow removal starts for highway roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 10)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float SnowRoadsHighwayStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow removal ends for highway roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 11)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(SnowRoadsHighwayStartHour), MinOffset = 2f)]
        public float SnowRoadsHighwayEndHour { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time range restrictions are enabled for snow removal on other roads.
        /// When set to <c>false</c>, snow removal operates 24/7.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 12)]
        [ConfigItemCheckBox]
        public bool EnableTimeRangeSnowRoadsOther { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow removal starts for other roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 13)]
        [ConfigItemSlider(0f, 23.5f, 0.5f, ValueType = SliderValueType.Time)]
        public float SnowRoadsOtherStartHour { get; set; }

        /// <summary>
        /// Gets or sets the daytime hour when the snow removal ends for other roads.
        /// </summary>
        [ConfigItem("5Services", "4Snow", 14)]
        [ConfigItemSlider(2f, 47.5f, 0.5f, ValueType = SliderValueType.Time, MinFrom = nameof(SnowRoadsOtherStartHour), MinOffset = 2f)]
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

        /// <summary>
        /// Checks the version of the deserialized object and migrates it to the latest version when necessary.
        /// </summary>
        public void MigrateWhenNecessary()
        {
            if(Version < 5)
            {
                MigrateServiceHoursToVersion5();
            }

            Version = LatestVersion;
        }

        /// <summary>
        /// Validates this instance and corrects possible invalid property values.
        /// </summary>
        public void Validate()
        {
            WakeUpHour = FastMath.Clamp(WakeUpHour, 4f, 8f);
            GoToSleepHour = FastMath.Clamp(GoToSleepHour, 20f, 23.75f);

            DayTimeSpeed = FastMath.Clamp(DayTimeSpeed, 1u, 6u);
            NightTimeSpeed = FastMath.Clamp(NightTimeSpeed, 1u, 6u);

            VirtualCitizens = (VirtualCitizensLevel)FastMath.Clamp((int)VirtualCitizens, (int)VirtualCitizensLevel.None, (int)VirtualCitizensLevel.Vanilla);
            ConstructionSpeed = FastMath.Clamp(ConstructionSpeed, 1u, 100u);

            SwitchOffLightsMaxHeight = FastMath.Clamp(SwitchOffLightsMaxHeight, 0f, 100f);

            BreakfastDuringWorkOrSchoolQuota = FastMath.Clamp(BreakfastDuringWorkOrSchoolQuota, 0u, 100u);
            LunchDuringWorkOrSchoolQuota = FastMath.Clamp(LunchDuringWorkOrSchoolQuota, 0u, 100u);
            SupperDuringWorkOrSchoolQuota = FastMath.Clamp(SupperDuringWorkOrSchoolQuota, 0u, 100u);
            LocalBuildingSearchQuota = FastMath.Clamp(LocalBuildingSearchQuota, 0u, 100u);
            ShoppingForFunQuota = FastMath.Clamp(ShoppingForFunQuota, 0u, 50u);
            OnTimeQuota = FastMath.Clamp(OnTimeQuota, 0u, 100u);

            OpenLowCommercialAtNightQuota = FastMath.Clamp(OpenLowCommercialAtNightQuota, 0u, 100u);
            OpenCommercialSecondShiftQuota = FastMath.Clamp(OpenCommercialSecondShiftQuota, 0u, 100u);
            OpenCommercialAtWeekendsQuota = FastMath.Clamp(OpenCommercialAtWeekendsQuota, 0u, 100u);

            NightClassQuota = FastMath.Clamp(NightClassQuota, 0u, 100u);

            EarliestHourEventStartWeekday = FastMath.Clamp(EarliestHourEventStartWeekday, 0f, 23.5f);
            LatestHourEventStartWeekday = FastMath.Clamp(LatestHourEventStartWeekday, EarliestHourEventStartWeekday + 4f, 23.5f);

            EarliestHourEventStartWeekend = FastMath.Clamp(EarliestHourEventStartWeekend, 0f, 23.5f);
            LatestHourEventStartWeekend = FastMath.Clamp(LatestHourEventStartWeekend, EarliestHourEventStartWeekend + 4f, 23.5f);

            EventPreparationDuration = FastMath.Clamp(EventPreparationDuration, 2f, 8f);

            BreakfastBegin = FastMath.Clamp(BreakfastBegin, 6f, 8f);
            BreakfastDuration = FastMath.Clamp(BreakfastDuration, 0.5f, 1.5f);
            BreakfastEnd = FastMath.Clamp(BreakfastEnd, BreakfastBegin + 2f, 10f);
            LunchBegin = FastMath.Clamp(LunchBegin, 11f, 13f);
            LunchDuration = FastMath.Clamp(LunchDuration, 0.5f, 2f);
            LunchEnd = FastMath.Clamp(LunchEnd, LunchBegin + 2f, 15f);
            SupperBegin = FastMath.Clamp(SupperBegin, 17f, 19f);
            SupperDuration = FastMath.Clamp(SupperDuration, 0.5f, 2f);
            SupperEnd = FastMath.Clamp(SupperEnd, SupperBegin + 2f, 21f);

            SchoolBegin = FastMath.Clamp(SchoolBegin, 4f, 10f);
            SchoolEnd = FastMath.Clamp(SchoolEnd, SchoolBegin + 6f, 16f);
            MaxOvertime = FastMath.Clamp(MaxOvertime, 0f, 4f);
            MaxVacationLength = FastMath.Clamp(MaxVacationLength, 0u, 7u);
            AcademicYearLength = FastMath.Clamp(AcademicYearLength, 1f, 30f);
            TogaPartyLength = FastMath.Clamp(TogaPartyLength, 4f, 24f);

            VisitBankOrPostOfficeInterval = FastMath.Clamp(VisitBankOrPostOfficeInterval, 1f, 7f);

            GarbageResidentialStartHour = FastMath.Clamp(GarbageResidentialStartHour, 0f, 23.5f);
            GarbageResidentialEndHour = FastMath.Clamp(GarbageResidentialEndHour, GarbageResidentialStartHour + 2f, 47.5f);

            GarbageCommercialStartHour = FastMath.Clamp(GarbageCommercialStartHour, 0f, 23.5f);
            GarbageCommercialEndHour = FastMath.Clamp(GarbageCommercialEndHour, GarbageCommercialStartHour + 2f, 47.5f);

            GarbageIndustrialStartHour = FastMath.Clamp(GarbageIndustrialStartHour, 0f, 23.5f);
            GarbageIndustrialEndHour = FastMath.Clamp(GarbageIndustrialEndHour, GarbageIndustrialStartHour + 2f, 47.5f);

            GarbageOfficeStartHour = FastMath.Clamp(GarbageOfficeStartHour, 0f, 23.5f);
            GarbageOfficeEndHour = FastMath.Clamp(GarbageOfficeEndHour, GarbageOfficeStartHour + 2f, 47.5f);

            GarbageOtherStartHour = FastMath.Clamp(GarbageOtherStartHour, 0f, 23.5f);
            GarbageOtherEndHour = FastMath.Clamp(GarbageOtherEndHour, GarbageOtherStartHour + 2f, 47.5f);

            MailResidentialStartHour = FastMath.Clamp(MailResidentialStartHour, 0f, 23.5f);
            MailResidentialEndHour = FastMath.Clamp(MailResidentialEndHour, MailResidentialStartHour + 2f, 47.5f);

            MailCommercialStartHour = FastMath.Clamp(MailCommercialStartHour, 0f, 23.5f);
            MailCommercialEndHour = FastMath.Clamp(MailCommercialEndHour, MailCommercialStartHour + 2f, 47.5f);

            MailIndustrialStartHour = FastMath.Clamp(MailIndustrialStartHour, 0f, 23.5f);
            MailIndustrialEndHour = FastMath.Clamp(MailIndustrialEndHour, MailIndustrialStartHour + 2f, 47.5f);

            MailOfficeStartHour = FastMath.Clamp(MailOfficeStartHour, 0f, 23.5f);
            MailOfficeEndHour = FastMath.Clamp(MailOfficeEndHour, MailOfficeStartHour + 2f, 47.5f);

            MailOtherStartHour = FastMath.Clamp(MailOtherStartHour, 0f, 23.5f);
            MailOtherEndHour = FastMath.Clamp(MailOtherEndHour, MailOtherStartHour + 2f, 47.5f);
           
            ParkMaintenanceStartHour = FastMath.Clamp(ParkMaintenanceStartHour, 0f, 23.5f);
            ParkMaintenanceEndHour = FastMath.Clamp(ParkMaintenanceEndHour, ParkMaintenanceStartHour + 2f, 47.5f);
           
            RoadMaintenanceRoadsSmallStartHour = FastMath.Clamp(RoadMaintenanceRoadsSmallStartHour, 0f, 23.5f);
            RoadMaintenanceRoadsSmallEndHour = FastMath.Clamp(RoadMaintenanceRoadsSmallEndHour, RoadMaintenanceRoadsSmallStartHour + 2f, 47.5f);

            RoadMaintenanceRoadsMediumStartHour = FastMath.Clamp(RoadMaintenanceRoadsMediumStartHour, 0f, 23.5f);
            RoadMaintenanceRoadsMediumEndHour = FastMath.Clamp(RoadMaintenanceRoadsMediumEndHour, RoadMaintenanceRoadsMediumStartHour + 2f, 47.5f);

            RoadMaintenanceRoadsLargeStartHour = FastMath.Clamp(RoadMaintenanceRoadsLargeStartHour, 0f, 23.5f);
            RoadMaintenanceRoadsLargeEndHour = FastMath.Clamp(RoadMaintenanceRoadsLargeEndHour, RoadMaintenanceRoadsLargeStartHour + 2f, 47.5f);

            RoadMaintenanceRoadsHighwayStartHour = FastMath.Clamp(RoadMaintenanceRoadsHighwayStartHour, 0f, 23.5f);
            RoadMaintenanceRoadsHighwayEndHour = FastMath.Clamp(RoadMaintenanceRoadsHighwayEndHour, RoadMaintenanceRoadsHighwayStartHour + 2f, 47.5f);

            RoadMaintenanceRoadsOtherStartHour = FastMath.Clamp(RoadMaintenanceRoadsOtherStartHour, 0f, 23.5f);
            RoadMaintenanceRoadsOtherEndHour = FastMath.Clamp(RoadMaintenanceRoadsOtherEndHour, RoadMaintenanceRoadsOtherStartHour + 2f, 47.5f);
            
            SnowRoadsSmallStartHour = FastMath.Clamp(SnowRoadsSmallStartHour, 0f, 23.5f);
            SnowRoadsSmallEndHour = FastMath.Clamp(SnowRoadsSmallEndHour, SnowRoadsSmallStartHour + 2f, 47.5f);

            SnowRoadsMediumStartHour = FastMath.Clamp(SnowRoadsMediumStartHour, 0f, 23.5f);
            SnowRoadsMediumEndHour = FastMath.Clamp(SnowRoadsMediumEndHour, SnowRoadsMediumStartHour + 2f, 47.5f);

            SnowRoadsLargeStartHour = FastMath.Clamp(SnowRoadsLargeStartHour, 0f, 23.5f);
            SnowRoadsLargeEndHour = FastMath.Clamp(SnowRoadsLargeEndHour, SnowRoadsLargeStartHour + 2f, 47.5f);

            SnowRoadsHighwayStartHour = FastMath.Clamp(SnowRoadsHighwayStartHour, 0f, 23.5f);
            SnowRoadsHighwayEndHour = FastMath.Clamp(SnowRoadsHighwayEndHour, SnowRoadsHighwayStartHour + 2f, 47.5f);

            SnowRoadsOtherStartHour = FastMath.Clamp(SnowRoadsOtherStartHour, 0f, 23.5f);
            SnowRoadsOtherEndHour = FastMath.Clamp(SnowRoadsOtherEndHour, SnowRoadsOtherStartHour + 2f, 47.5f);
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
            IsBreakfastTimeEnabledDuringWorkOrSchool = true;
            IsLunchTimeEnabledDuringWorkOrSchool = true;
            IsSupperTimeEnabledDuringWorkOrSchool = false;

            StopConstructionAtNight = true;
            ConstructionSpeed = 50;
            SwitchOffLightsAtNight = true;
            SwitchOffLightsMaxHeight = 40f;
            CanAbandonJourney = true;
            RealisticFires = false;
            GarbageSlowDown = 0.15f;
            MailSlowDown = 0.3f;
            CrimeSlowDown = 0.2f;
            WeeklyCommericalDeliveries = true;
            DummyTrafficBehavior = true;

            BreakfastDuringWorkOrSchoolQuota = 20;
            LunchDuringWorkOrSchoolQuota = 80;
            SupperDuringWorkOrSchoolQuota = 20;
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

            BreakfastBegin = 7f;
            BreakfastDuration = 0.5f;
            BreakfastEnd = 10f;
            LunchBegin = 12f;
            LunchDuration = 0.5f;
            LunchEnd = 13f;
            SupperBegin = 18f;
            SupperDuration = 0.5f;
            SupperEnd = 20f;

            MaxOvertime = 2f;
            SchoolBegin = 8f;
            SchoolEnd = 14f;
            MaxVacationLength = 3u;
            AcademicYearLength = 7f;
            TogaPartyLength = 8f;
            VisitBankOrPostOfficeInterval = 3f;

            EnableTimeRangeGarbageResidential = false;
            GarbageResidentialStartHour = 0f;
            GarbageResidentialEndHour = 2f;
            EnableTimeRangeGarbageCommercial = false;
            GarbageCommercialStartHour = 0f;
            GarbageCommercialEndHour = 2f;
            EnableTimeRangeGarbageIndustrial = false;
            GarbageIndustrialStartHour = 0f;
            GarbageIndustrialEndHour = 2f;
            EnableTimeRangeGarbageOffice = false;
            GarbageOfficeStartHour = 0f;
            GarbageOfficeEndHour = 2f;
            EnableTimeRangeGarbageOther = false;
            GarbageOtherStartHour = 0f;
            GarbageOtherEndHour = 2f;

            EnableTimeRangeMailResidential = false;
            MailResidentialStartHour = 0f;
            MailResidentialEndHour = 2f;
            EnableTimeRangeMailCommercial = false;
            MailCommercialStartHour = 0f;
            MailCommercialEndHour = 2f;
            EnableTimeRangeMailIndustrial = false;
            MailIndustrialStartHour = 0f;
            MailIndustrialEndHour = 2f;
            EnableTimeRangeMailOffice = false;
            MailOfficeStartHour = 0f;
            MailOfficeEndHour = 2f;
            EnableTimeRangeMailResidential = false;
            MailOtherStartHour = 0f;
            MailOtherEndHour = 2f;

            EnableTimeRangeParkMaintenance = false;
            ParkMaintenanceStartHour = 0f;
            ParkMaintenanceEndHour = 2f;

            EnableTimeRangeRoadMaintenanceRoadsSmall = false;
            RoadMaintenanceRoadsSmallStartHour = 0f;
            RoadMaintenanceRoadsSmallEndHour = 2f;
            EnableTimeRangeRoadMaintenanceRoadsMedium = false;
            RoadMaintenanceRoadsMediumStartHour = 0f;
            RoadMaintenanceRoadsMediumEndHour = 2f;
            EnableTimeRangeRoadMaintenanceRoadsLarge = false;
            RoadMaintenanceRoadsLargeStartHour = 0f;
            RoadMaintenanceRoadsLargeEndHour = 2f;
            EnableTimeRangeRoadMaintenanceRoadsHighway = false;
            RoadMaintenanceRoadsHighwayStartHour = 0f;
            RoadMaintenanceRoadsHighwayEndHour = 2f;
            EnableTimeRangeRoadMaintenanceRoadsOther = false;
            RoadMaintenanceRoadsOtherStartHour = 0f;
            RoadMaintenanceRoadsOtherEndHour = 2f;

            EnableTimeRangeSnowRoadsSmall = false;
            SnowRoadsSmallStartHour = 0f;
            SnowRoadsSmallEndHour = 2f;
            EnableTimeRangeSnowRoadsMedium = false;
            SnowRoadsMediumStartHour = 0f;
            SnowRoadsMediumEndHour = 2f;
            EnableTimeRangeSnowRoadsLarge = false;
            SnowRoadsLargeStartHour = 0f;
            SnowRoadsLargeEndHour = 2f;
            EnableTimeRangeSnowRoadsHighway = false;
            SnowRoadsHighwayStartHour = 0f;
            SnowRoadsHighwayEndHour = 2f;
            EnableTimeRangeSnowRoadsOther = false;
            SnowRoadsOtherStartHour = 0f;
            SnowRoadsOtherEndHour = 2f;

            ShowIncompatibilityNotifications = true;
            DebugMode = false;
            LoggingMode = false;
            AdvancedLoggingMode = false;
        }

        private void OnWeekendEnabledChanged(bool value)
        {
            var buildingManager = Singleton<BuildingManager>.instance;
            if (buildingManager == null)
            {
                return;
            }

            var buildings = buildingManager.m_buildings;

            DayOfWeek[] allWeek = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

            DayOfWeek[] noWeekend = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

            for (ushort buildingId = 0; buildingId < buildings.m_size; buildingId++)
            {
                ref var building = ref buildings.m_buffer[buildingId];
                if ((building.m_flags & Building.Flags.Created) != 0)
                {
                    if (BuildingWorkTimeManager.BuildingWorkTimeExist(buildingId))
                    {
                        var workTime = BuildingWorkTimeManager.GetBuildingWorkTime(buildingId);

                        if(workTime.IsDefault)
                        {
                            if (!value)
                            {
                                workTime.WorkDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];
                            }
                            else
                            {
                                var service = building.Info.m_class.m_service;
                                var subService = building.Info.m_class.m_subService;
                                bool openOnWeekends = BuildingWorkTimeManager.IsBuildingActiveOnWeekend(service, subService);
                                workTime.WorkDays = openOnWeekends ? allWeek : noWeekend;
                            }
                            BuildingWorkTimeManager.SetBuildingWorkTime(buildingId, workTime);
                        }
                    }
                }
            }
        }

        private void MigrateServiceHoursToVersion5()
        {
            Log.Info("MigrateServiceHoursToVersion5");

            var type = GetType();

            foreach (var enabledProperty in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                const string prefix = "EnableTimeRange";

                if (!enabledProperty.Name.StartsWith(prefix, StringComparison.Ordinal) || enabledProperty.PropertyType != typeof(bool))
                {
                    continue;
                }

                string serviceName = enabledProperty.Name.Substring(prefix.Length);

                var startProperty = type.GetProperty(serviceName + "StartHour");

                var endProperty = type.GetProperty(serviceName + "EndHour");

                if (startProperty?.PropertyType != typeof(float) || endProperty?.PropertyType != typeof(float))
                {
                    throw new InvalidOperationException($"Missing time-range properties for {serviceName}");
                }

                float oldStart = (float)startProperty.GetValue(this, null);
                float oldEnd = (float)endProperty.GetValue(this, null);

                // In v4, equal values meant unrestricted service.
                bool enabled = oldStart != oldEnd;

                float start = Math.Max(0f, Math.Min(23.5f, oldStart));
                float end;

                if (!enabled)
                {
                    // v5 needs valid slider values even when disabled.
                    end = start + 2f;
                }
                else
                {
                    end = oldEnd;

                    // Convert an overnight clock time into next-day hours.
                    if (end < start)
                    {
                        end += 24f;
                    }

                    // A v4 interval shorter than 2 hours cannot be
                    // represented under the new minimum-duration rule.
                    end = Math.Max(end, start + 2f);
                    end = Math.Min(end, start + 24f);
                }

                startProperty.SetValue(this, start, null);
                endProperty.SetValue(this, end, null);
                enabledProperty.SetValue(this, enabled, null);

                RealTimeMod.configProvider.SaveDefaultConfiguration();
            }
        }
    }
}
