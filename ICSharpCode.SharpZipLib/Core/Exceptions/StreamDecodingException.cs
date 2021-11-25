using System;
using System.Runtime.Serialization;

namespace ICSharpCode.SharpZipLib
{
	/// <summary>
	/// Indicates that an error occurred during decoding of a input stream due to corrupt
	/// data or (unintentional) library incompatibility.
	/// </summary>
	[Serializable]
	public class StreamDecodingException : SharpZipBaseException
	{
		private const string GenericMessage = "Input stream could not be decoded";

		/// <summary>
		/// Initializes a new instance of the StreamDecodingException with a generic message
		/// </summary>
		public StreamDecodingException() : base(GenericMessage) { }

		/// <summary>
		/// Initializes a new instance of the StreamDecodingException class with a specified error message.
		/// </summary>
		/// <param name="message">A message describing the exception.</param>
		public StreamDecodingException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance of the StreamDecodingException class with a specified
		/// error message and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">A message describing the exception.</param>
		/// <param name="innerException">The inner exception</param>
		public StreamDecodingException(string message, Exception innerException) : base(message, innerException) { }

		/// <summary>
		/// Initializes a new instance of the StreamDecodingException class with serialized data.
		/// </summary>
		/// <param name="info">
		/// The System.Runtime.Serialization.SerializationInfo that holds the serialized
		/// object data about the exception being thrown.
		/// </param>
		/// <param name="context">
		/// The System.Runtime.Serialization.StreamingContext that contains contextual information
		/// about the source or destination.
		/// </param>
		protected StreamDecodingException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
