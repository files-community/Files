using System;
using ICSharpCode.SharpZipLib.Core;
using static ICSharpCode.SharpZipLib.Zip.ZipEntryFactory;

namespace ICSharpCode.SharpZipLib.Zip
{
	/// <summary>
	/// Defines factory methods for creating new <see cref="ZipEntry"></see> values.
	/// </summary>
	public interface IEntryFactory
	{
		/// <summary>
		/// Create a <see cref="ZipEntry"/> for a file given its name
		/// </summary>
		/// <param name="fileName">The name of the file to create an entry for.</param>
		/// <returns>Returns a <see cref="ZipEntry">file entry</see> based on the <paramref name="fileName"/> passed.</returns>
		ZipEntry MakeFileEntry(string fileName);

		/// <summary>
		/// Create a <see cref="ZipEntry"/> for a file given its name
		/// </summary>
		/// <param name="fileName">The name of the file to create an entry for.</param>
		/// <param name="useFileSystem">If true get details from the file system if the file exists.</param>
		/// <returns>Returns a <see cref="ZipEntry">file entry</see> based on the <paramref name="fileName"/> passed.</returns>
		ZipEntry MakeFileEntry(string fileName, bool useFileSystem);

		/// <summary>
		/// Create a <see cref="ZipEntry"/> for a file given its actual name and optional override name
		/// </summary>
		/// <param name="fileName">The name of the file to create an entry for.</param>
		/// <param name="entryName">An alternative name to be used for the new entry. Null if not applicable.</param>
		/// <param name="useFileSystem">If true get details from the file system if the file exists.</param>
		/// <returns>Returns a <see cref="ZipEntry">file entry</see> based on the <paramref name="fileName"/> passed.</returns>
		ZipEntry MakeFileEntry(string fileName, string entryName, bool useFileSystem);

		/// <summary>
		/// Create a <see cref="ZipEntry"/> for a directory given its name
		/// </summary>
		/// <param name="directoryName">The name of the directory to create an entry for.</param>
		/// <returns>Returns a <see cref="ZipEntry">directory entry</see> based on the <paramref name="directoryName"/> passed.</returns>
		ZipEntry MakeDirectoryEntry(string directoryName);

		/// <summary>
		/// Create a <see cref="ZipEntry"/> for a directory given its name
		/// </summary>
		/// <param name="directoryName">The name of the directory to create an entry for.</param>
		/// <param name="useFileSystem">If true get details from the file system for this directory if it exists.</param>
		/// <returns>Returns a <see cref="ZipEntry">directory entry</see> based on the <paramref name="directoryName"/> passed.</returns>
		ZipEntry MakeDirectoryEntry(string directoryName, bool useFileSystem);

		/// <summary>
		/// Get/set the <see cref="INameTransform"></see> applicable.
		/// </summary>
		INameTransform NameTransform { get; set; }

		/// <summary>
		/// Get the <see cref="TimeSetting"/> in use.
		/// </summary>
		TimeSetting Setting { get; }

		/// <summary>
		/// Get the <see cref="DateTime"/> value to use when <see cref="Setting"/> is set to <see cref="TimeSetting.Fixed"/>,
		/// or if not specified, the value of <see cref="DateTime.Now"/> when the class was the initialized
		/// </summary>
		DateTime FixedDateTime { get; }
	}
}
