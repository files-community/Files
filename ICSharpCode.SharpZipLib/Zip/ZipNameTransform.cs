using ICSharpCode.SharpZipLib.Core;
using System;
using System.IO;
using System.Text;

namespace ICSharpCode.SharpZipLib.Zip
{
	/// <summary>
	/// ZipNameTransform transforms names as per the Zip file naming convention.
	/// </summary>
	/// <remarks>The use of absolute names is supported although its use is not valid
	/// according to Zip naming conventions, and should not be used if maximum compatability is desired.</remarks>
	public class ZipNameTransform : INameTransform
	{
		#region Constructors

		/// <summary>
		/// Initialize a new instance of <see cref="ZipNameTransform"></see>
		/// </summary>
		public ZipNameTransform()
		{
		}

		/// <summary>
		/// Initialize a new instance of <see cref="ZipNameTransform"></see>
		/// </summary>
		/// <param name="trimPrefix">The string to trim from the front of paths if found.</param>
		public ZipNameTransform(string trimPrefix)
		{
			TrimPrefix = trimPrefix;
		}

		#endregion Constructors

		/// <summary>
		/// Static constructor.
		/// </summary>
		static ZipNameTransform()
		{
			char[] invalidPathChars;
			invalidPathChars = Path.GetInvalidPathChars();
			int howMany = invalidPathChars.Length + 2;

			InvalidEntryCharsRelaxed = new char[howMany];
			Array.Copy(invalidPathChars, 0, InvalidEntryCharsRelaxed, 0, invalidPathChars.Length);
			InvalidEntryCharsRelaxed[howMany - 1] = '*';
			InvalidEntryCharsRelaxed[howMany - 2] = '?';

			howMany = invalidPathChars.Length + 4;
			InvalidEntryChars = new char[howMany];
			Array.Copy(invalidPathChars, 0, InvalidEntryChars, 0, invalidPathChars.Length);
			InvalidEntryChars[howMany - 1] = ':';
			InvalidEntryChars[howMany - 2] = '\\';
			InvalidEntryChars[howMany - 3] = '*';
			InvalidEntryChars[howMany - 4] = '?';
		}

		/// <summary>
		/// Transform a windows directory name according to the Zip file naming conventions.
		/// </summary>
		/// <param name="name">The directory name to transform.</param>
		/// <returns>The transformed name.</returns>
		public string TransformDirectory(string name)
		{
			name = TransformFile(name);
			if (name.Length > 0)
			{
				if (!name.EndsWith("/", StringComparison.Ordinal))
				{
					name += "/";
				}
			}
			else
			{
				throw new ZipException("Cannot have an empty directory name");
			}
			return name;
		}

		/// <summary>
		/// Transform a windows file name according to the Zip file naming conventions.
		/// </summary>
		/// <param name="name">The file name to transform.</param>
		/// <returns>The transformed name.</returns>
		public string TransformFile(string name)
		{
			if (name != null)
			{
				string lowerName = name.ToLower();
				if ((trimPrefix_ != null) && (lowerName.IndexOf(trimPrefix_, StringComparison.Ordinal) == 0))
				{
					name = name.Substring(trimPrefix_.Length);
				}

				name = name.Replace(@"\", "/");
				name = PathUtils.DropPathRoot(name);

				// Drop any leading and trailing slashes.
				name = name.Trim('/');

				// Convert consecutive // characters to /
				int index = name.IndexOf("//", StringComparison.Ordinal);
				while (index >= 0)
				{
					name = name.Remove(index, 1);
					index = name.IndexOf("//", StringComparison.Ordinal);
				}

				name = MakeValidName(name, '_');
			}
			else
			{
				name = string.Empty;
			}
			return name;
		}

		/// <summary>
		/// Get/set the path prefix to be trimmed from paths if present.
		/// </summary>
		/// <remarks>The prefix is trimmed before any conversion from
		/// a windows path is done.</remarks>
		public string TrimPrefix
		{
			get { return trimPrefix_; }
			set
			{
				trimPrefix_ = value;
				if (trimPrefix_ != null)
				{
					trimPrefix_ = trimPrefix_.ToLower();
				}
			}
		}

		/// <summary>
		/// Force a name to be valid by replacing invalid characters with a fixed value
		/// </summary>
		/// <param name="name">The name to force valid</param>
		/// <param name="replacement">The replacement character to use.</param>
		/// <returns>Returns a valid name</returns>
		private static string MakeValidName(string name, char replacement)
		{
			int index = name.IndexOfAny(InvalidEntryChars);
			if (index >= 0)
			{
				var builder = new StringBuilder(name);

				while (index >= 0)
				{
					builder[index] = replacement;

					if (index >= name.Length)
					{
						index = -1;
					}
					else
					{
						index = name.IndexOfAny(InvalidEntryChars, index + 1);
					}
				}
				name = builder.ToString();
			}

			if (name.Length > 0xffff)
			{
				throw new PathTooLongException();
			}

			return name;
		}

		/// <summary>
		/// Test a name to see if it is a valid name for a zip entry.
		/// </summary>
		/// <param name="name">The name to test.</param>
		/// <param name="relaxed">If true checking is relaxed about windows file names and absolute paths.</param>
		/// <returns>Returns true if the name is a valid zip name; false otherwise.</returns>
		/// <remarks>Zip path names are actually in Unix format, and should only contain relative paths.
		/// This means that any path stored should not contain a drive or
		/// device letter, or a leading slash.  All slashes should forward slashes '/'.
		/// An empty name is valid for a file where the input comes from standard input.
		/// A null name is not considered valid.
		/// </remarks>
		public static bool IsValidName(string name, bool relaxed)
		{
			bool result = (name != null);

			if (result)
			{
				if (relaxed)
				{
					result = name.IndexOfAny(InvalidEntryCharsRelaxed) < 0;
				}
				else
				{
					result =
						(name.IndexOfAny(InvalidEntryChars) < 0) &&
						(name.IndexOf('/') != 0);
				}
			}

			return result;
		}

		/// <summary>
		/// Test a name to see if it is a valid name for a zip entry.
		/// </summary>
		/// <param name="name">The name to test.</param>
		/// <returns>Returns true if the name is a valid zip name; false otherwise.</returns>
		/// <remarks>Zip path names are actually in unix format,
		/// and should only contain relative paths if a path is present.
		/// This means that the path stored should not contain a drive or
		/// device letter, or a leading slash.  All slashes should forward slashes '/'.
		/// An empty name is valid where the input comes from standard input.
		/// A null name is not considered valid.
		/// </remarks>
		public static bool IsValidName(string name)
		{
			bool result =
				(name != null) &&
				(name.IndexOfAny(InvalidEntryChars) < 0) &&
				(name.IndexOf('/') != 0)
				;
			return result;
		}

		#region Instance Fields

		private string trimPrefix_;

		#endregion Instance Fields

		#region Class Fields

		private static readonly char[] InvalidEntryChars;
		private static readonly char[] InvalidEntryCharsRelaxed;

		#endregion Class Fields
	}

	/// <summary>
	/// An implementation of INameTransform that transforms entry paths as per the Zip file naming convention.
	/// Strips path roots and puts directory separators in the correct format ('/')
	/// </summary>
	public class PathTransformer : INameTransform
	{
		/// <summary>
		/// Initialize a new instance of <see cref="PathTransformer"></see>
		/// </summary>
		public PathTransformer()
		{
		}

		/// <summary>
		/// Transform a windows directory name according to the Zip file naming conventions.
		/// </summary>
		/// <param name="name">The directory name to transform.</param>
		/// <returns>The transformed name.</returns>
		public string TransformDirectory(string name)
		{
			name = TransformFile(name);
			
			if (name.Length > 0)
			{
				if (!name.EndsWith("/", StringComparison.Ordinal))
				{
					name += "/";
				}
			}
			else
			{
				throw new ZipException("Cannot have an empty directory name");
			}

			return name;
		}

		/// <summary>
		/// Transform a windows file name according to the Zip file naming conventions.
		/// </summary>
		/// <param name="name">The file name to transform.</param>
		/// <returns>The transformed name.</returns>
		public string TransformFile(string name)
		{
			if (name != null)
			{
				// Put separators in the expected format.
				name = name.Replace(@"\", "/");

				// Remove the path root.
				name = PathUtils.DropPathRoot(name);

				// Drop any leading and trailing slashes.
				name = name.Trim('/');

				// Convert consecutive // characters to /
				int index = name.IndexOf("//", StringComparison.Ordinal);
				while (index >= 0)
				{
					name = name.Remove(index, 1);
					index = name.IndexOf("//", StringComparison.Ordinal);
				}
			}
			else
			{
				name = string.Empty;
			}

			return name;
		}
	}
}
