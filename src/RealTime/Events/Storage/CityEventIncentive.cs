// CityEventCosts.cs
namespace RealTime.Events.Storage
{
    using System.Xml.Serialization;

    /// <summary>
    /// A storage class for the city event incentive settings.
    /// </summary>
    public class CityEventIncentive
    {
        /// <summary>Gets or sets the name for this incentive.</summary>
        [XmlAttribute("Name")]
        public string Name = "";

        /// <summary>Gets or sets the cost for this incentive.</summary>
        [XmlAttribute("Cost")]
        public float Cost = 3;

        /// <summary>Gets or sets the return cost for this incentive.</summary>
        [XmlAttribute("ReturnCost")]
        public float ReturnCost = 10;

        /// <summary>Gets or sets if this incentive will be avaliable for random events.</summary>
        [XmlAttribute("ActiveWhenRandomEvent")]
        public bool ActiveWhenRandomEvent = false;

        /// <summary>Gets or sets the description of this incentive.</summary>
        [XmlElement("Description", IsNullable = false)]
        public string Description = "";

        /// <summary>Gets or sets the result of the positive effect for this incentive.</summary>
        [XmlElement("PositiveEffect", IsNullable = false)]
        public int PositiveEffect = 10;

        /// <summary>Gets or sets the result of the negative effect for this incentive.</summary>
        [XmlElement("NegativeEffect", IsNullable = false)]
        public int NegativeEffect = 10;
    }
}
