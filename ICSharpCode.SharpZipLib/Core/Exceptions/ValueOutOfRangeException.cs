using System;
using System.Runtime.Serialization;

namespace ICSharpCode.SharpZipLib
{
	/// <summary>
	/// Indicates that a value was outside of the expected range when decoding an input stream
	/// </summary>
	[Serializable]
	public class ValueOutOfRangeException : StreamDecodingException
	{
		/// <summary>
		/// Initializes a new instance of the ValueOutOfRangeException class naming the causing variable
		/// </summary>
		/// <param name="nameOfValue">Name of the variable, use: nameof()</param>
		public ValueOutOfRangeException(string nameOfValue)
			: base($"{nameOfValue} out of range") { }

		/// <summary>
		/// Initializes a new instance of the ValueOutOfRangeException class naming the causing variable,
		/// it's current value and expected range.
		/// </summary>
		/// <param name="nameOfValue">Name of the variable, use: nameof()</param>
		/// <param name="value">The invalid value</param>
		/// <param name="maxValue">Expected maximum value</param>
		/// <param name="minValue">Expected minimum value</param>
		public ValueOutOfRangeException(string nameOfValue, long value, long maxValue, long minValue = 0)
			: this(nameOfValue, value.ToString(), maxValue.ToString(), minValue.ToString()) { }

		/// <summary>
		/// Initializes a new instance of the ValueOutOfRangeException class naming the causing variable,
		/// it's current value and expected range.
		/// </summary>
		/// <param name="nameOfValue">Name of the variable, use: nameof()</param>
		/// <param name="value">The invalid value</param>
		/// <param name="maxValue">Expected maximum value</param>
		/// <param name="minValue">Expected minimum value</param>
		public ValueOutOfRangeException(string nameOfValue, string value, string maxValue, string minValue = "0") :
			base($"{nameOfValue} out of range: {value}, should be {minValue}..{maxValue}")
		{ }

		private ValueOutOfRangeException()
		{
		}

		private ValueOutOfRangeException(string message, Exception innerException) : base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the ValueOutOfRangeException class with serialized data.
		/// </summary>
		/// <param name="info">
		/// The System.Runtime.Serialization.SerializationInfo that holds the serialized
		/// object data about the exception being thrown.
		/// </param>
		/// <param name="context">
		/// The System.Runtime.Serialization.StreamingContext that contains contextual information
		/// about the source or destination.
		/// </param>
		protected ValueOutOfRangeException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
