using ICSharpCode.SharpZipLib.Core;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ICSharpCode.SharpZipLib.Zip
{
	/// <summary>
	/// WindowsNameTransform transforms <see cref="ZipFile"/> names to windows compatible ones.
	/// </summary>
	public class WindowsNameTransform : INameTransform
	{
		/// <summary>
		///  The maximum windows path name permitted.
		/// </summary>
		/// <remarks>This may not valid for all windows systems - CE?, etc but I cant find the equivalent in the CLR.</remarks>
		private const int MaxPath = 260;

		private string _baseDirectory;
		private bool _trimIncomingPaths;
		private char _replacementChar = '_';
		private bool _allowParentTraversal;

		/// <summary>
		/// In this case we need Windows' invalid path characters.
		/// Path.GetInvalidPathChars() only returns a subset invalid on all platforms.
		/// </summary>
		private static readonly char[] InvalidEntryChars = new char[] {
			'"', '<', '>', '|', '\0', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005',
			'\u0006', '\a', '\b', '\t', '\n', '\v', '\f', '\r', '\u000e', '\u000f',
			'\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015', '\u0016',
			'\u0017', '\u0018', '\u0019', '\u001a', '\u001b', '\u001c', '\u001d',
			'\u001e', '\u001f',
			// extra characters for masks, etc.
			'*', '?', ':'
		};

		/// <summary>
		/// Initialises a new instance of <see cref="WindowsNameTransform"/>
		/// </summary>
		/// <param name="baseDirectory"></param>
		/// <param name="allowParentTraversal">Allow parent directory traversal in file paths (e.g. ../file)</param>
		public WindowsNameTransform(string baseDirectory, bool allowParentTraversal = false)
		{
			BaseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory), "Directory name is invalid");
			AllowParentTraversal = allowParentTraversal;
		}

		/// <summary>
		/// Initialise a default instance of <see cref="WindowsNameTransform"/>
		/// </summary>
		public WindowsNameTransform()
		{
			// Do nothing.
		}

		/// <summary>
		/// Gets or sets a value containing the target directory to prefix values with.
		/// </summary>
		public string BaseDirectory
		{
			get { return _baseDirectory; }
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException(nameof(value));
				}

				_baseDirectory = Path.GetFullPath(value);
			}
		}

		/// <summary>
		/// Allow parent directory traversal in file paths (e.g. ../file)
		/// </summary>
		public bool AllowParentTraversal
		{
			get => _allowParentTraversal;
			set => _allowParentTraversal = value;
		}

		/// <summary>
		/// Gets or sets a value indicating whether paths on incoming values should be removed.
		/// </summary>
		public bool TrimIncomingPaths
		{
			get { return _trimIncomingPaths; }
			set { _trimIncomingPaths = value; }
		}

		/// <summary>
		/// Transform a Zip directory name to a windows directory name.
		/// </summary>
		/// <param name="name">The directory name to transform.</param>
		/// <returns>The transformed name.</returns>
		public string TransformDirectory(string name)
		{
			name = TransformFile(name);
			if (name.Length > 0)
			{
				while (name.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
				{
					name = name.Remove(name.Length - 1, 1);
				}
			}
			else
			{
				throw new InvalidNameException("Cannot have an empty directory name");
			}
			return name;
		}

		/// <summary>
		/// Transform a Zip format file name to a windows style one.
		/// </summary>
		/// <param name="name">The file name to transform.</param>
		/// <returns>The transformed name.</returns>
		public string TransformFile(string name)
		{
			if (name != null)
			{
				name = MakeValidName(name, _replacementChar);

				if (_trimIncomingPaths)
				{
					name = Path.GetFileName(name);
				}

				// This may exceed windows length restrictions.
				// Combine will throw a PathTooLongException in that case.
				if (_baseDirectory != null)
				{
					name = Path.Combine(_baseDirectory, name);

					// Ensure base directory ends with directory separator ('/' or '\' depending on OS)
					var pathBase = Path.GetFullPath(_baseDirectory);
					if (pathBase[pathBase.Length - 1] != Path.DirectorySeparatorChar)
					{
						pathBase += Path.DirectorySeparatorChar;
					}

					if (!_allowParentTraversal && !Path.GetFullPath(name).StartsWith(pathBase, StringComparison.InvariantCultureIgnoreCase))
					{
						throw new InvalidNameException("Parent traversal in paths is not allowed");
					}
				}
			}
			else
			{
				name = string.Empty;
			}
			return name;
		}

		/// <summary>
		/// Test a name to see if it is a valid name for a windows filename as extracted from a Zip archive.
		/// </summary>
		/// <param name="name">The name to test.</param>
		/// <returns>Returns true if the name is a valid zip name; false otherwise.</returns>
		/// <remarks>The filename isnt a true windows path in some fundamental ways like no absolute paths, no rooted paths etc.</remarks>
		public static bool IsValidName(string name)
		{
			bool result =
				(name != null) &&
				(name.Length <= MaxPath) &&
				(string.Compare(name, MakeValidName(name, '_'), StringComparison.Ordinal) == 0)
				;

			return result;
		}

		/// <summary>
		/// Force a name to be valid by replacing invalid characters with a fixed value
		/// </summary>
		/// <param name="name">The name to make valid</param>
		/// <param name="replacement">The replacement character to use for any invalid characters.</param>
		/// <returns>Returns a valid name</returns>
		public static string MakeValidName(string name, char replacement)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}

			name = PathUtils.DropPathRoot(name.Replace("/", Path.DirectorySeparatorChar.ToString()));

			// Drop any leading slashes.
			while ((name.Length > 0) && (name[0] == Path.DirectorySeparatorChar))
			{
				name = name.Remove(0, 1);
			}

			// Drop any trailing slashes.
			while ((name.Length > 0) && (name[name.Length - 1] == Path.DirectorySeparatorChar))
			{
				name = name.Remove(name.Length - 1, 1);
			}

			// Convert consecutive \\ characters to \
			int index = name.IndexOf(string.Format("{0}{0}", Path.DirectorySeparatorChar), StringComparison.Ordinal);
			while (index >= 0)
			{
				name = name.Remove(index, 1);
				index = name.IndexOf(string.Format("{0}{0}", Path.DirectorySeparatorChar), StringComparison.Ordinal);
			}

			// Convert any invalid characters using the replacement one.
			index = name.IndexOfAny(InvalidEntryChars);
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

			// Check for names greater than MaxPath characters.
			// TODO: Were is CLR version of MaxPath defined?  Can't find it in Environment.
			if (name.Length > MaxPath)
			{
				throw new PathTooLongException();
			}

			return name;
		}

		/// <summary>
		/// Gets or set the character to replace invalid characters during transformations.
		/// </summary>
		public char Replacement
		{
			get { return _replacementChar; }
			set
			{
				for (int i = 0; i < InvalidEntryChars.Length; ++i)
				{
					if (InvalidEntryChars[i] == value)
					{
						throw new ArgumentException("invalid path character");
					}
				}

				if ((value == Path.DirectorySeparatorChar) || (value == Path.AltDirectorySeparatorChar))
				{
					throw new ArgumentException("invalid replacement character");
				}

				_replacementChar = value;
			}
		}
	}
}
