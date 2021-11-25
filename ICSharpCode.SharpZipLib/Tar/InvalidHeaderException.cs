using System;
using System.Runtime.Serialization;

namespace ICSharpCode.SharpZipLib.Tar
{
	/// <summary>
	/// This exception is used to indicate that there is a problem
	/// with a TAR archive header.
	/// </summary>
	[Serializable]
	public class InvalidHeaderException : TarException
	{
		/// <summary>
		/// Initialise a new instance of the InvalidHeaderException class.
		/// </summary>
		public InvalidHeaderException()
		{
		}

		/// <summary>
		/// Initialises a new instance of the InvalidHeaderException class with a specified message.
		/// </summary>
		/// <param name="message">Message describing the exception cause.</param>
		public InvalidHeaderException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initialise a new instance of InvalidHeaderException
		/// </summary>
		/// <param name="message">Message describing the problem.</param>
		/// <param name="exception">The exception that is the cause of the current exception.</param>
		public InvalidHeaderException(string message, Exception exception)
			: base(message, exception)
		{
		}

		/// <summary>
		/// Initializes a new instance of the InvalidHeaderException class with serialized data.
		/// </summary>
		/// <param name="info">
		/// The System.Runtime.Serialization.SerializationInfo that holds the serialized
		/// object data about the exception being thrown.
		/// </param>
		/// <param name="context">
		/// The System.Runtime.Serialization.StreamingContext that contains contextual information
		/// about the source or destination.
		/// </param>
		protected InvalidHeaderException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
