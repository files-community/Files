using System;
using System.Runtime.Serialization;

namespace ICSharpCode.SharpZipLib.Core
{
	/// <summary>
	/// InvalidNameException is thrown for invalid names such as directory traversal paths and names with invalid characters
	/// </summary>
	[Serializable]
	public class InvalidNameException : SharpZipBaseException
	{
		/// <summary>
		/// Initializes a new instance of the InvalidNameException class with a default error message.
		/// </summary>
		public InvalidNameException() : base("An invalid name was specified")
		{
		}

		/// <summary>
		/// Initializes a new instance of the InvalidNameException class with a specified error message.
		/// </summary>
		/// <param name="message">A message describing the exception.</param>
		public InvalidNameException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the InvalidNameException class with a specified
		/// error message and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">A message describing the exception.</param>
		/// <param name="innerException">The inner exception</param>
		public InvalidNameException(string message, Exception innerException) : base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the InvalidNameException class with serialized data.
		/// </summary>
		/// <param name="info">
		/// The System.Runtime.Serialization.SerializationInfo that holds the serialized
		/// object data about the exception being thrown.
		/// </param>
		/// <param name="context">
		/// The System.Runtime.Serialization.StreamingContext that contains contextual information
		/// about the source or destination.
		/// </param>
		protected InvalidNameException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
