using System.IO;

namespace Files.App.Data.Exceptions
{
	public sealed class FileAlreadyExistsException : IOException
	{
		public string FileName { get; private set; }

		public FileAlreadyExistsException(string message, string fileName) : base(message)
		{
			FileName = fileName;
		}
	}
}
