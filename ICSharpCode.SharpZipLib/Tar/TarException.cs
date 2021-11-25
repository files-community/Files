using System;
using System.Runtime.Serialization;

namespace ICSharpCode.SharpZipLib.Tar
{
	/// <summary>
	/// TarException represents exceptions specific to Tar classes and code.
	/// </summary>
	[Serializable]
	public class TarException : SharpZipBaseException
	{
		/// <summary>
		/// Initialise a new instance of <see cref="TarException" />.
		/// </summary>
		public TarException()
		{
		}

		/// <summary>
		/// Initialise a new instance of <see cref="TarException" /> with its message string.
		/// </summary>
		/// <param name="message">A <see cref="string"/> that describes the error.</param>
		public TarException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initialise a new instance of <see cref="TarException" />.
		/// </summary>
		/// <param name="message">A <see cref="string"/> that describes the error.</param>
		/// <param name="innerException">The <see cref="Exception"/> that caused this exception.</param>
		public TarException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the TarException class with serialized data.
		/// </summary>
		/// <param name="info">
		/// The System.Runtime.Serialization.SerializationInfo that holds the serialized
		/// object data about the exception being thrown.
		/// </param>
		/// <param name="context">
		/// The System.Runtime.Serialization.StreamingContext that contains contextual information
		/// about the source or destination.
		/// </param>
		protected TarException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
