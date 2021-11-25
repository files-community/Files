using System;
using System.Runtime.Serialization;

namespace ICSharpCode.SharpZipLib.GZip
{
	/// <summary>
	/// GZipException represents exceptions specific to GZip classes and code.
	/// </summary>
	[Serializable]
	public class GZipException : SharpZipBaseException
	{
		/// <summary>
		/// Initialise a new instance of <see cref="GZipException" />.
		/// </summary>
		public GZipException()
		{
		}

		/// <summary>
		/// Initialise a new instance of <see cref="GZipException" /> with its message string.
		/// </summary>
		/// <param name="message">A <see cref="string"/> that describes the error.</param>
		public GZipException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initialise a new instance of <see cref="GZipException" />.
		/// </summary>
		/// <param name="message">A <see cref="string"/> that describes the error.</param>
		/// <param name="innerException">The <see cref="Exception"/> that caused this exception.</param>
		public GZipException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the GZipException class with serialized data.
		/// </summary>
		/// <param name="info">
		/// The System.Runtime.Serialization.SerializationInfo that holds the serialized
		/// object data about the exception being thrown.
		/// </param>
		/// <param name="context">
		/// The System.Runtime.Serialization.StreamingContext that contains contextual information
		/// about the source or destination.
		/// </param>
		protected GZipException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
