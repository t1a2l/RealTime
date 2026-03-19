namespace RealTime.Events.Containers
{
    using RealTime.Events.Storage;

    /// <summary>
    /// Option storage
    /// </summary>
    internal class LabelOptionItem
    {
        /// <summary>
        /// A unique identifier for finding the option later
        /// </summary>
        public CityEventTemplate linkedTemplate = null;

        /// <summary>
        /// The readable string to be printed out
        /// </summary>
        public string readableLabel = "";
    }
}
