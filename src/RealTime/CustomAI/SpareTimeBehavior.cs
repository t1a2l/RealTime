// SpareTimeBehavior.cs

namespace RealTime.CustomAI
{
    using System;
    using RealTime.Config;
    using RealTime.Simulation;
    using SkyTools.Tools;
    using static Constants;

    /// <summary>
    /// A class that provides custom logic for the spare time simulation.
    /// </summary>
    internal sealed class SpareTimeBehavior : ISpareTimeBehavior
    {
        private readonly RealTimeConfig config;
        private readonly ITimeInfo timeInfo;
        private readonly uint[] defaultChances;
        private readonly uint[] secondShiftChances;
        private readonly uint[] nightShiftChances;
        private readonly uint[] continuousNightShiftChances;
        private readonly uint[] shoppingChances;
        private readonly uint[] vacationChances;
        private readonly uint[] businessAppointmentChances;
        private readonly uint[] eatingOutChances;

        private float simulationCycle;

        private int lastUpdatedMinute;
        private int lastUpdatedDay;

        /// <summary>Initializes a new instance of the <see cref="SpareTimeBehavior"/> class.</summary>
        /// <param name="config">The configuration to run with.</param>
        /// <param name="timeInfo">The object providing the game time information.</param>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        public SpareTimeBehavior(RealTimeConfig config, ITimeInfo timeInfo)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.timeInfo = timeInfo ?? throw new ArgumentNullException(nameof(timeInfo));

            int agesCount = Enum.GetValues(typeof(Citizen.AgeGroup)).Length;
            defaultChances = new uint[agesCount];
            secondShiftChances = new uint[agesCount];
            nightShiftChances = new uint[agesCount];
            continuousNightShiftChances = new uint[agesCount];
            shoppingChances = new uint[agesCount];
            businessAppointmentChances = new uint[agesCount];
            eatingOutChances = new uint[agesCount];

            vacationChances = new uint[Enum.GetValues(typeof(Citizen.Wealth)).Length];
        }

        /// <summary>
        /// Gets a value indicating whether the fireworks in the parks are allowed at current time.
        /// </summary>
        public bool AreFireworksAllowed => timeInfo.CurrentHour >= timeInfo.SunsetHour && timeInfo.CurrentHour < config.GoToSleepHour;

        /// <summary>Sets the duration (in hours) of a full simulation cycle for all citizens.
        /// The game calls the simulation methods for a particular citizen with this period.</summary>
        /// <param name="cyclePeriod">The citizens simulation cycle period, in game hours.</param>
        public void SetSimulationCyclePeriod(float cyclePeriod) => simulationCycle = cyclePeriod;

        /// <summary>Calculates the chances for the citizens to go out based on the current game time.</summary>
        public void RefreshChances()
        {
            if (lastUpdatedMinute != timeInfo.Now.Minute)
            {
                uint weekdayModifier;
                if (config.IsWeekendEnabled)
                {
                    weekdayModifier = timeInfo.Now.IsWeekendTime(12f, config.GoToSleepHour)
                        ? 11u
                        : 1u;
                }
                else
                {
                    weekdayModifier = 1u;
                }

                bool isWeekend = weekdayModifier > 1u;
                float currentHour = timeInfo.CurrentHour;

                CalculateDefaultChances(currentHour, weekdayModifier);
                CalculateSecondShiftChances(currentHour, isWeekend);
                CalculateNightShiftChances(currentHour, isWeekend);
                CalculateContinuousNightShiftChances(currentHour, isWeekend);
                CalculateShoppingChance(currentHour);
                CalculateBusinessAppointmentChance(currentHour);
                CalculateEatingOutChance(currentHour);

                lastUpdatedMinute = timeInfo.Now.Minute;
            }

            if (lastUpdatedDay != timeInfo.Now.Day)
            {
                CalculateVacationChances();
                lastUpdatedDay = timeInfo.Now.Day;
            }
        }

        /// <summary>
        /// Gets the probability whether a citizen with specified age would go shopping on current time.
        /// </summary>
        ///
        /// <param name="citizenAge">The age of the citizen to check.</param>
        ///
        /// <returns>A percentage value in range of 0..100 that describes the probability whether
        /// a citizen with specified age would go shopping on current time.</returns>
        public uint GetShoppingChance(Citizen.AgeGroup citizenAge) => shoppingChances[(int)citizenAge];

        /// <summary>
        /// Gets the probability whether a citizen with specified age would go to a business appointment on current time.
        /// </summary>
        ///
        /// <param name="citizenAge">The age of the citizen to check.</param>
        ///
        /// <returns>A percentage value in range of 0..100 that describes the probability whether
        /// a citizen with specified age would go to a business appointment on current time.</returns>
        public uint GetBusinessAppointmentChance(Citizen.AgeGroup citizenAge) => businessAppointmentChances[(int)citizenAge];

        /// <summary>
        /// Gets the probability whether a citizen with specified age would go relaxing on current time.
        /// </summary>
        ///
        /// <param name="citizenAge">The age of the citizen to check.</param>
        /// <param name="workShift">The citizen's assigned work shift (default is <see cref="WorkShift.Unemployed"/>).</param>
        /// <param name="isOnVacation"><c>true</c> if the citizen is on vacation.</param>
        ///
        /// <returns>A percentage value in range of 0..100 that describes the probability whether
        /// a citizen with specified age would go relaxing on current time.</returns>
        public uint GetRelaxingChance(Citizen.AgeGroup citizenAge, WorkShift workShift = WorkShift.Unemployed, bool isOnVacation = false)
        {
            if (isOnVacation)
            {
                return defaultChances[(int)citizenAge] * 2u;
            }

            int age = (int)citizenAge;
            switch (citizenAge)
            {
                case Citizen.AgeGroup.Young:
                case Citizen.AgeGroup.Adult:
                    switch (workShift)
                    {
                        case WorkShift.Second:
                            return secondShiftChances[age];

                        case WorkShift.Night:
                            return nightShiftChances[age];

                        case WorkShift.ContinuousNight:
                            return continuousNightShiftChances[age];

                        default:
                            return defaultChances[age];
                    }

                default:
                    return defaultChances[age];
            }
        }


        /// <summary>Sets the dummy traffic ai probability based on relaxing chance of adults.</summary>
        /// <param name="probability">The dummy traffic probability.</param>
        /// <returns>The altered probability if the options is true otherwise the default value.</returns>
        public int SetDummyTrafficProbability(int probability)
        {
            if(config.DummyTrafficBehavior)
            {
                // Using the relaxing chance of an adult as base value - seems to be reasonable.
                int chance = (int)GetRelaxingChance(Citizen.AgeGroup.Adult);
                probability = probability * chance * chance / 10_000;
                return probability;
            }
            else
            {
                return probability;
            }
        }

        /// <summary>Gets a precise probability (in percent multiplied by 100) for a citizen with specified
        /// wealth to go on vacation on current day.</summary>
        /// <param name="wealth">The citizen's wealth.</param>
        /// <returns>The precise probability (in percent multiplied by 100) for the citizen to go on vacation
        /// on current day.</returns>
        public uint GetPreciseVacationChance(Citizen.Wealth wealth) => vacationChances[(int)wealth];

        /// <summary>
        /// Gets the probability whether a citizen with specified age would go to eat out on current time.
        /// </summary>
        ///
        /// <param name="citizenAge">The age of the citizen to check.</param>
        ///
        /// <returns>A percentage value in range of 0..100 that describes the probability whether
        /// a citizen with specified age would go to eat out on current time.</returns>
        public uint GetEatingOutChance(Citizen.AgeGroup citizenAge) => eatingOutChances[(int)citizenAge];

        private void CalculateDefaultChances(float currentHour, uint weekdayModifier)
        {
            float latestGoingOutHour = config.GoToSleepHour - simulationCycle;
            bool isDayTime = currentHour >= config.WakeUpHour && currentHour < latestGoingOutHour;
            float timeModifier;
            if (isDayTime)
            {
                timeModifier = FastMath.Clamp(currentHour - config.WakeUpHour, 0, 4f);
            }
            else
            {
                float nightDuration = 24f - (latestGoingOutHour - config.WakeUpHour);
                float relativeHour = currentHour - latestGoingOutHour;
                if (relativeHour < 0)
                {
                    relativeHour += 24f;
                }

                timeModifier = 3f / nightDuration * (nightDuration - relativeHour);
            }

            float chance = (timeModifier + weekdayModifier) * timeModifier;
            uint roundedChance = (uint)Math.Round(chance);

#if DEBUG
            bool dump = defaultChances[(int)Citizen.AgeGroup.Adult] != roundedChance;
#endif

            defaultChances[(int)Citizen.AgeGroup.Child] = isDayTime ? roundedChance : 0;
            defaultChances[(int)Citizen.AgeGroup.Teen] = isDayTime ? (uint)Math.Round(chance * 0.9f) : 0;
            defaultChances[(int)Citizen.AgeGroup.Young] = (uint)Math.Round(chance * 1.3f);
            defaultChances[(int)Citizen.AgeGroup.Adult] = roundedChance;
            defaultChances[(int)Citizen.AgeGroup.Senior] = isDayTime ? (uint)Math.Round(chance * 0.8f) : 0;

#if DEBUG
            if (dump)
            {
                Log.Debug(LogCategory.Simulation, $"DEFAULT GOING OUT CHANCES for {timeInfo.Now}: child = {defaultChances[0]}, teen = {defaultChances[1]}, young = {defaultChances[2]}, adult = {defaultChances[3]}, senior = {defaultChances[4]}");
            }
#endif
        }

        private void CalculateSecondShiftChances(float currentHour, bool isWeekend)
        {
#if DEBUG
            uint oldChance = secondShiftChances[(int)Citizen.AgeGroup.Adult];
#endif

            float wakeUpHour = config.WakeUpHour - config.GoToSleepHour + 24f;
            if (isWeekend || currentHour < config.WakeUpHour || currentHour >= wakeUpHour)
            {
                secondShiftChances[(int)Citizen.AgeGroup.Young] = defaultChances[(int)Citizen.AgeGroup.Young];
                secondShiftChances[(int)Citizen.AgeGroup.Adult] = defaultChances[(int)Citizen.AgeGroup.Adult];
            }
            else
            {
                secondShiftChances[(int)Citizen.AgeGroup.Young] = 0;
                secondShiftChances[(int)Citizen.AgeGroup.Adult] = 0;
            }

#if DEBUG
            if (oldChance != secondShiftChances[(int)Citizen.AgeGroup.Adult])
            {
                Log.Debug(LogCategory.Simulation, $"SECOND SHIFT GOING OUT CHANCE for {timeInfo.Now}: young = {secondShiftChances[2]}, adult = {secondShiftChances[3]}");
            }
#endif
        }

        private void CalculateContinuousNightShiftChances(float currentHour, bool isWeekend)
        {
#if DEBUG
            uint oldChance = continuousNightShiftChances[(int)Citizen.AgeGroup.Adult];
#endif

            float wakeUpHour = config.WorkBegin + (config.WakeUpHour - config.GoToSleepHour + 24f);
            if (isWeekend || currentHour < config.WakeUpHour || currentHour >= wakeUpHour)
            {
                continuousNightShiftChances[(int)Citizen.AgeGroup.Young] = defaultChances[(int)Citizen.AgeGroup.Young];
                continuousNightShiftChances[(int)Citizen.AgeGroup.Adult] = defaultChances[(int)Citizen.AgeGroup.Adult];
            }
            else
            {
                continuousNightShiftChances[(int)Citizen.AgeGroup.Young] = 0;
                continuousNightShiftChances[(int)Citizen.AgeGroup.Adult] = 0;
            }

#if DEBUG
            if (oldChance != continuousNightShiftChances[(int)Citizen.AgeGroup.Adult])
            {
                Log.Debug(LogCategory.Simulation, $"Continuous NIGHT SHIFT GOING OUT CHANCE for {timeInfo.Now}: young = {continuousNightShiftChances[2]}, adult = {continuousNightShiftChances[3]}");
            }
#endif
        }

        private void CalculateNightShiftChances(float currentHour, bool isWeekend)
        {
#if DEBUG
            uint oldChance = nightShiftChances[(int)Citizen.AgeGroup.Adult];
#endif

            float wakeUpHour = config.WorkBegin + (config.WakeUpHour - config.GoToSleepHour + 24f);
            if (isWeekend || currentHour < config.WakeUpHour || currentHour >= wakeUpHour)
            {
                nightShiftChances[(int)Citizen.AgeGroup.Young] = defaultChances[(int)Citizen.AgeGroup.Young];
                nightShiftChances[(int)Citizen.AgeGroup.Adult] = defaultChances[(int)Citizen.AgeGroup.Adult];
            }
            else
            {
                nightShiftChances[(int)Citizen.AgeGroup.Young] = 0;
                nightShiftChances[(int)Citizen.AgeGroup.Adult] = 0;
            }

#if DEBUG
            if (oldChance != nightShiftChances[(int)Citizen.AgeGroup.Adult])
            {
                Log.Debug(LogCategory.Simulation, $"NIGHT SHIFT GOING OUT CHANCE for {timeInfo.Now}: young = {nightShiftChances[2]}, adult = {nightShiftChances[3]}");
            }
#endif
        }

        private void CalculateShoppingChance(float currentHour)
        {
            float minShoppingChanceEndHour = Math.Min(config.WakeUpHour, EarliestWakeUp);
            float maxShoppingChanceStartHour = Math.Max(config.WorkBegin, config.WakeUpHour);
            if (minShoppingChanceEndHour == maxShoppingChanceStartHour)
            {
                minShoppingChanceEndHour = FastMath.Clamp(maxShoppingChanceStartHour - 1f, 2f, maxShoppingChanceStartHour - 1f);
            }

#if DEBUG
            uint oldChance = shoppingChances[(int)Citizen.AgeGroup.Adult];
#endif

            float chance;
            bool isNight;
            float maxShoppingChanceEndHour = Math.Max(config.GoToSleepHour, config.WorkEnd);
            if (currentHour < minShoppingChanceEndHour)
            {
                isNight = true;
                chance = NightShoppingChance;
            }
            else if (currentHour < maxShoppingChanceStartHour)
            {
                isNight = true;
                chance = NightShoppingChance
                    + (100u - NightShoppingChance) * (currentHour - minShoppingChanceEndHour) / (maxShoppingChanceStartHour - minShoppingChanceEndHour);
            }
            else if (currentHour < maxShoppingChanceEndHour)
            {
                isNight = false;
                chance = 100;
            }
            else
            {
                isNight = true;
                chance = NightShoppingChance
                    + (100u - NightShoppingChance) * (24f - currentHour) / (24f - maxShoppingChanceEndHour);
            }

            uint roundedChance = (uint)Math.Round(chance);

            shoppingChances[(int)Citizen.AgeGroup.Child] = isNight ? 0u : roundedChance;
            shoppingChances[(int)Citizen.AgeGroup.Teen] = isNight ? 0u : roundedChance;
            shoppingChances[(int)Citizen.AgeGroup.Young] = roundedChance;
            shoppingChances[(int)Citizen.AgeGroup.Adult] = roundedChance;
            shoppingChances[(int)Citizen.AgeGroup.Senior] = isNight ? (uint)Math.Round(chance * 0.1f) : roundedChance;

#if DEBUG
            if (oldChance != roundedChance)
            {
                Log.Debug(LogCategory.Simulation, $"SHOPPING CHANCES for {timeInfo.Now}: child = {shoppingChances[0]}, teen = {shoppingChances[1]}, young = {shoppingChances[2]}, adult = {shoppingChances[3]}, senior = {shoppingChances[4]}");
            }
#endif
        }

        private void CalculateBusinessAppointmentChance(float currentHour)
        {
#if DEBUG
            uint oldChance = businessAppointmentChances[(int)Citizen.AgeGroup.Adult];
#endif

            float chance;
            float businessAppointmentChanceStartHour = config.WorkBegin;
            float businessAppointmentChanceEndHour = config.WorkEnd;
            chance = currentHour < businessAppointmentChanceStartHour ? 0u : currentHour < businessAppointmentChanceEndHour ? 100 : 0u;

            uint roundedChance = (uint)Math.Round(chance);

            businessAppointmentChances[(int)Citizen.AgeGroup.Child] = 0u;
            businessAppointmentChances[(int)Citizen.AgeGroup.Teen] = 0u;
            businessAppointmentChances[(int)Citizen.AgeGroup.Young] = roundedChance;
            businessAppointmentChances[(int)Citizen.AgeGroup.Adult] = roundedChance;
            businessAppointmentChances[(int)Citizen.AgeGroup.Senior] = roundedChance;

#if DEBUG
            if (oldChance != roundedChance)
            {
                Log.Debug(LogCategory.Simulation, $"BusinessAppointment CHANCES for {timeInfo.Now}: child = {businessAppointmentChances[0]}, teen = {businessAppointmentChances[1]}, young = {businessAppointmentChances[2]}, adult = {businessAppointmentChances[3]}, senior = {businessAppointmentChances[4]}");
            }
#endif
        }

        private void CalculateEatingOutChance(float currentHour)
        {
            float breakfastPeak = config.WakeUpHour + 1.5f;
            float lunchPeak = (config.WorkBegin + config.WorkEnd) * 0.5f;
            float supperPeak = Math.Min(config.GoToSleepHour - 2f, config.WorkEnd + 3f);

            float breakfastChance = GetMealChance(currentHour, breakfastPeak, 2.0f, 35f);
            float lunchChance = GetMealChance(currentHour, lunchPeak, 2.0f, 55f);
            float supperChance = GetMealChance(currentHour, supperPeak, 3.0f, 75f);

            float chance = Math.Max(breakfastChance, Math.Max(lunchChance, supperChance));
            uint roundedChance = (uint)Math.Round(FastMath.Clamp(chance, 0f, 100f));

            bool isLateEvening = currentHour >= config.GoToSleepHour - 2f;

            eatingOutChances[(int)Citizen.AgeGroup.Child] = 0u;
            eatingOutChances[(int)Citizen.AgeGroup.Teen] = 0u;
            eatingOutChances[(int)Citizen.AgeGroup.Young] = roundedChance;
            eatingOutChances[(int)Citizen.AgeGroup.Adult] = roundedChance;
            eatingOutChances[(int)Citizen.AgeGroup.Senior] = isLateEvening ? (uint)Math.Round(roundedChance * 0.5f) : (uint)Math.Round(roundedChance * 0.8f);
        }

        private static float GetMealChance(float currentHour, float peakHour, float halfWidth, float peakChance)
        {
            float distance = Math.Abs(currentHour - peakHour);
            return distance >= halfWidth ? 0f : peakChance * (1f - distance / halfWidth);
        }

        private void CalculateVacationChances()
        {
            uint baseChance;
            int dayOfYear = timeInfo.Now.DayOfYear;
            if (dayOfYear < 7)
            {
                baseChance = 100u * 8u;
            }
            else if (dayOfYear < 30 * 5)
            {
                baseChance = 100u * 1u;
            }
            else if (dayOfYear < 30 * 9)
            {
                baseChance = 100u * 3u;
            }
            else if (dayOfYear < 352)
            {
                baseChance = 100u * 1u;
            }
            else
            {
                baseChance = 100u * 20u;
            }

#if DEBUG
            bool dump = baseChance != vacationChances[(int)Citizen.Wealth.Medium];
#endif

            vacationChances[(int)Citizen.Wealth.Low] = baseChance / 2;
            vacationChances[(int)Citizen.Wealth.Medium] = baseChance;
            vacationChances[(int)Citizen.Wealth.High] = baseChance * 3 / 2;

#if DEBUG
            if (dump)
            {
                Log.Debug(LogCategory.Simulation, $"VACATION CHANCES for {timeInfo.Now}: low = {vacationChances[0]}, medium = {vacationChances[1]}, high = {vacationChances[2]}");
            }
#endif
        }
    }
}
