// CityEventContainer.cs

namespace RealTime.Events.Storage
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// A storage container class for the city event templates.
    /// </summary>
    [XmlRoot("EventContainer")]
    public sealed class CityEventContainer
    {
        /// <summary>Gets the event templates stored in this container.</summary>
        [XmlArray("Events", IsNullable = false)]
        [XmlArrayItem("Event")]
        public List<CityEventTemplate> Templates { get; } = [];
    }
}
