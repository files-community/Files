#if UNMANAGED

namespace SevenZip
{
    using System;

    /// <summary>
    /// Stores an int number
    /// </summary>
    public sealed class IntEventArgs : ValueEventArgs<int>
    {
        public IntEventArgs(int value) : base(value)
        {
        }
    }

    public class ValueEventArgs<T> : EventArgs
    {
        private readonly T _value;

        /// <summary>
        /// Initializes a new instance of the IntEventArgs class
        /// </summary>
        /// <param name="value">Useful data carried by the IntEventArgs class</param>
        public ValueEventArgs(T value)
        {
            _value = value;
        }

        /// <summary>
        /// Gets the value of the IntEventArgs class
        /// </summary>
        public T Value => _value;
    }
}

#endif
