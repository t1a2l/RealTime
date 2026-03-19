// EventColor.cs

namespace RealTime.Events
{
    using System;

    /// <summary>
    /// A struct representing the event color.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="EventColor"/> struct.
    /// </remarks>
    /// <param name="red">The red component of the color.</param>
    /// <param name="green">The green component of the color.</param>
    /// <param name="blue">The blue component of the color.</param>
    internal readonly struct EventColor(byte red, byte green, byte blue) : IEquatable<EventColor>
    {

        /// <summary>
        /// Gets the red component of the color.
        /// </summary>
        public byte Red { get; } = red;

        /// <summary>
        /// Gets the green component of the color.
        /// </summary>
        public byte Green { get; } = green;

        /// <summary>
        /// Gets the blue component of the color.
        /// </summary>
        public byte Blue { get; } = blue;

        public static bool operator ==(EventColor left, EventColor right) => left.Equals(right);

        public static bool operator !=(EventColor left, EventColor right) => !(left == right);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is EventColor color && Equals(color);

        /// <inheritdoc/>
        public bool Equals(EventColor other) => Red == other.Red && Green == other.Green && Blue == other.Blue;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = -1058441243;
            hashCode = hashCode * -1521134295 + Red.GetHashCode();
            hashCode = hashCode * -1521134295 + Green.GetHashCode();
            hashCode = hashCode * -1521134295 + Blue.GetHashCode();
            return hashCode;
        }
    }
}
