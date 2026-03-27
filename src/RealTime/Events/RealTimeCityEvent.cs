// RealTimeCityEvent.cs

namespace RealTime.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ColossalFramework;
    using RealTime.Events.Containers;
    using RealTime.Events.Storage;
    using RealTime.Simulation;
    using UnityEngine;

    /// <summary>A custom city event.</summary>
    /// <seealso cref="CityEventBase"/>
    /// <remarks>
    /// Initializes a new instance of the <see cref="RealTimeCityEvent"/> class using the specified <paramref name="eventTemplate"/>.
    /// </remarks>
    /// <param name="eventTemplate">The event template this city event is created from.</param>
    /// <exception cref="ArgumentNullException">Thrown when the argument is null.</exception>
    internal sealed class RealTimeCityEvent(CityEventTemplate eventTemplate) : CityEventBase
    {
        private readonly CityEventTemplate eventTemplate = eventTemplate ?? throw new ArgumentNullException(nameof(eventTemplate));

        private int attendeesCount;

        public int UserTicketCount { get; private set; }

        public float UserEntryCost { get; private set; }

        public List<IncentiveOptionItem> UserIncentives { get; } = [];

        public string EventName => eventTemplate.EventName;

        public string UserEventName => eventTemplate.UserEventName;

        public CityEventCosts Costs => eventTemplate.Costs;

        public CityEventIncentive[] Incentives => eventTemplate.Incentives;

        /// <summary>
        /// Initializes a new instance of the <see cref="RealTimeCityEvent"/> class using the specified
        /// <paramref name="eventTemplate"/> and the already known current <paramref name="attendeesCount"/>.
        /// </summary>
        /// <param name="eventTemplate">The event template this city event is created from.</param>
        /// <param name="attendeesCount">The current attendees count of this city event.</param>
        public RealTimeCityEvent(CityEventTemplate eventTemplate, int attendeesCount)
            : this(eventTemplate)
        {
            this.attendeesCount = attendeesCount;
        }

        /// <summary>
        /// Gets the event color.
        /// </summary>
        public override EventColor Color { get; } = new EventColor(180, 0, 90);

        /// <summary>Accepts an event attendee with specified properties.</summary>
        /// <param name="age">The attendee age.</param>
        /// <param name="gender">The attendee gender.</param>
        /// <param name="education">The attendee education.</param>
        /// <param name="wealth">The attendee wealth.</param>
        /// <param name="wellbeing">The attendee wellbeing.</param>
        /// <param name="happiness">The attendee happiness.</param>
        /// <param name="randomizer">A reference to the game's randomizer.</param>
        /// <param name="buildingClass">the class of the building the event is taking place in.</param>
        /// <returns>
        /// <c>true</c> if the event attendee with specified properties is accepted and can attend this city event;
        /// otherwise, <c>false</c>.
        /// </returns>
        public override bool TryAcceptAttendee(
            Citizen.AgeGroup age,
            Citizen.Gender gender,
            Citizen.Education education,
            Citizen.Wealth wealth,
            Citizen.Wellbeing wellbeing,
            Citizen.Happiness happiness,
            IRandomizer randomizer,
            ItemClass buildingClass)
        {
            if (attendeesCount > eventTemplate.Capacity)
            {
                return false;
            }

            if (eventTemplate.Costs != null && eventTemplate.Costs.Entry > GetCitizenBudgetForEvent(wealth, randomizer))
            {
                return false;
            }

            var attendees = eventTemplate.Attendees;
            float randomPercentage = randomizer.GetRandomValue(100u);

            if (!CheckAge(age, attendees, randomPercentage))
            {
                return false;
            }

            randomPercentage = randomizer.GetRandomValue(100u);
            if (!CheckGender(gender, attendees, randomPercentage))
            {
                return false;
            }

            randomPercentage = randomizer.GetRandomValue(100u);
            if (!CheckEducation(education, attendees, randomPercentage))
            {
                return false;
            }

            randomPercentage = randomizer.GetRandomValue(100u);
            if (!CheckWealth(wealth, attendees, randomPercentage))
            {
                return false;
            }

            randomPercentage = randomizer.GetRandomValue(100u);
            if (!CheckWellbeing(wellbeing, attendees, randomPercentage))
            {
                return false;
            }

            randomPercentage = randomizer.GetRandomValue(100u);
            if (!CheckHappiness(happiness, attendees, randomPercentage))
            {
                return false;
            }

            if (eventTemplate.Costs != null && eventTemplate.Costs.Entry > 0)
            {
                int entryFee = Mathf.RoundToInt(eventTemplate.Costs.Entry * 100f);
                Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.PublicIncome, entryFee, buildingClass);
            }
            attendeesCount++;

            return true;
        }

        /// <summary>
        /// Creates an instance of the <see cref="RealTimeEventStorage"/> class that contains the current city event data.
        /// </summary>
        /// <returns>A new instance of the <see cref="RealTimeEventStorage"/> class.</returns>
        public RealTimeEventStorage GetStorageData() => new()
        {
            EventName = eventTemplate.EventName,
            BuildingClassName = eventTemplate.BuildingClassName,
            StartTime = StartTime.Ticks,
            BuildingId = BuildingId,
            BuildingName = BuildingName,
            AttendeesCount = attendeesCount,
            UserTicketCount = UserTicketCount,
            UserEntryCost = UserEntryCost,
            UserIncentives = [.. UserIncentives.Select(i => new RealTimeEventStorage.StoredIncentive
            {
                Name = i.title,
                SliderValue = i.sliderValue
            })]
        };

        public void AddIncentive(string name, float count, float cost) => UserIncentives.Add(new IncentiveOptionItem
        {
            title = name,
            sliderValue = count,
            cost = cost
        });

        public void OnEventFinished(ItemClass buildingClass)
        {
            foreach (var incentive in UserIncentives)
            {
                int returnAmount = Mathf.RoundToInt(incentive.returnCost * incentive.sliderValue * 100f);
                Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.PublicIncome, returnAmount, buildingClass);
            }
        }


        /// <summary>Calculates the city event duration.</summary>
        /// <returns>This city event duration in hours.</returns>
        protected override float GetDuration() => (float)eventTemplate.Duration;

        private static bool CheckAge(Citizen.AgeGroup age, CityEventAttendees attendees, float randomPercentage)
        {
            switch (age)
            {
                case Citizen.AgeGroup.Child:
                    return randomPercentage < attendees.Children;

                case Citizen.AgeGroup.Teen:
                    return randomPercentage < attendees.Teens;

                case Citizen.AgeGroup.Young:
                    return randomPercentage < attendees.YoungAdults;

                case Citizen.AgeGroup.Adult:
                    return randomPercentage < attendees.Adults;

                case Citizen.AgeGroup.Senior:
                    return randomPercentage < attendees.Seniors;
                default:
                    break;
            }

            return false;
        }

        private static bool CheckWellbeing(Citizen.Wellbeing wellbeing, CityEventAttendees attendees, float randomPercentage)
        {
            switch (wellbeing)
            {
                case Citizen.Wellbeing.VeryUnhappy:
                    return randomPercentage < attendees.VeryUnhappyWellbeing;

                case Citizen.Wellbeing.Unhappy:
                    return randomPercentage < attendees.UnhappyWellbeing;

                case Citizen.Wellbeing.Satisfied:
                    return randomPercentage < attendees.SatisfiedWellbeing;

                case Citizen.Wellbeing.Happy:
                    return randomPercentage < attendees.HappyWellbeing;

                case Citizen.Wellbeing.VeryHappy:
                    return randomPercentage < attendees.VeryHappyWellbeing;
                default:
                    break;
            }

            return false;
        }

        private static bool CheckHappiness(Citizen.Happiness happiness, CityEventAttendees attendees, float randomPercentage)
        {
            switch (happiness)
            {
                case Citizen.Happiness.Bad:
                    return randomPercentage < attendees.BadHappiness;

                case Citizen.Happiness.Poor:
                    return randomPercentage < attendees.PoorHappiness;

                case Citizen.Happiness.Good:
                    return randomPercentage < attendees.GoodHappiness;

                case Citizen.Happiness.Excellent:
                    return randomPercentage < attendees.ExcellentHappiness;

                case Citizen.Happiness.Suberb:
                    return randomPercentage < attendees.SuperbHappiness;
                default:
                    break;
            }

            return false;
        }

        private static bool CheckGender(Citizen.Gender gender, CityEventAttendees attendees, float randomPercentage)
        {
            switch (gender)
            {
                case Citizen.Gender.Female:
                    return randomPercentage < attendees.Females;

                case Citizen.Gender.Male:
                    return randomPercentage < attendees.Males;
                default:
                    break;
            }

            return false;
        }

        private static bool CheckEducation(Citizen.Education education, CityEventAttendees attendees, float randomPercentage)
        {
            switch (education)
            {
                case Citizen.Education.Uneducated:
                    return randomPercentage < attendees.Uneducated;

                case Citizen.Education.OneSchool:
                    return randomPercentage < attendees.OneSchool;

                case Citizen.Education.TwoSchools:
                    return randomPercentage < attendees.TwoSchools;

                case Citizen.Education.ThreeSchools:
                    return randomPercentage < attendees.ThreeSchools;
                default:
                    break;
            }

            return false;
        }

        private static bool CheckWealth(Citizen.Wealth wealth, CityEventAttendees attendees, float randomPercentage)
        {
            switch (wealth)
            {
                case Citizen.Wealth.Low:
                    return randomPercentage < attendees.LowWealth;

                case Citizen.Wealth.Medium:
                    return randomPercentage < attendees.MediumWealth;

                case Citizen.Wealth.High:
                    return randomPercentage < attendees.HighWealth;
                default:
                    break;
            }

            return false;
        }
    }
}
