// CityEventCosts.cs
namespace RealTime.Events.Storage
{
    using System.Xml.Serialization;

    /// <summary>
    /// A storage class for the city event costs settings.
    /// </summary>
    public class CityEventCosts
    {
        /// <summary>Gets or sets the creation price for this event.</summary>
        [XmlElement("Creation", IsNullable = false)]
        public float Creation = 100;

        /// <summary>Gets or sets the head price for this event.</summary>
        [XmlElement("PerHead", IsNullable = false)]
        public float PerHead = 5;

        /// <summary>Gets or sets the sign advertisment price for this event.</summary>
        [XmlElement("AdvertisingSigns", IsNullable = false)]
        public float AdvertisingSigns = 20000;

        /// <summary>Gets or sets the TV advertisment price for this event.</summary>
        [XmlElement("AdvertisingTV", IsNullable = false)]
        public float AdvertisingTV = 5000;

        /// <summary>Gets or sets the ticket price for this event.</summary>
        [XmlElement("EntryCost")]
        public float Entry { get; set; } = 10;
    }
}
