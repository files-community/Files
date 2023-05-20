// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Filesystem.FilesystemHistory
{
	/// <summary>
	/// Represents an interface for storage history
	/// </summary>
	public interface IStorageHistory
	{
		/// <summary>
		/// Type of operation that took place.
		/// </summary>
		FileOperationType OperationType { get; }

		/// <summary>
		/// The sources file or folder.
		/// </summary>
		/// <remarks>
		/// This property may contain more than one item.
		/// </remarks>
		IList<IStorageItemWithPath> Source { get; }

		/// <summary>
		/// The destination file or folder.
		/// </summary>
		/// <remarks>
		/// This property may contain more than one item.
		/// </remarks>
		IList<IStorageItemWithPath> Destination { get; }

		void Modify(IStorageHistory newHistory);

		void Modify(FileOperationType operationType, IStorageItemWithPath source, IStorageItemWithPath destination);

		void Modify(FileOperationType operationType, IList<IStorageItemWithPath> source, IList<IStorageItemWithPath> destination);
	}
}
