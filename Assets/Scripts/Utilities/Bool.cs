using System;

namespace BovineLabs.Entities.Helpers {
    /// <summary>
    /// Burst currently does not support <see cref="bool"/> so this is a simple wrapper that acts like bool.
    /// </summary>
    [Serializable]
    public struct Bool : IEquatable<Bool>
    {
        /// <summary>
        /// The value of the Bool. Should only be used by serializer.
        /// </summary>
        public byte Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Bool"/> struct.
        /// </summary>
        /// <param name="value">The default value.</param>
        public Bool(bool value)
        {
            this.Value = value ? (byte)1 : (byte)0;
        }

        public static implicit operator Bool(bool b)
        {
            return new Bool(b);
        }

        public static implicit operator bool(Bool b)
        {
            return b.Value != 0;
        }

        /// <inheritdoc/>
        public bool Equals(Bool other)
        {
            return this.Value == other.Value;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj != null && obj is Bool b && this.Equals(b);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return (this.Value != 0).ToString();
        }
    }
}