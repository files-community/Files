// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Filesystem.FilesystemHistory
{
	public interface IStorageHistory
	{
		/// <summary>
		/// Type of operation that took place
		/// </summary>
		FileOperationType OperationType { get; }

		/// <summary>
		/// Source file/folder
		/// <br/>
		/// <br/>
		/// Attention!
		/// <br/>
		/// May contain more that one item
		/// </summary>
		IList<IStorageItemWithPath> Source { get; }

		/// <summary>
		/// Destination file/folder
		/// <br/>
		/// <br/>
		/// Attention!
		/// <br/>
		/// May contain more that one item
		/// </summary>
		IList<IStorageItemWithPath> Destination { get; }

		void Modify(IStorageHistory newHistory);
		void Modify(FileOperationType operationType, IStorageItemWithPath source, IStorageItemWithPath destination);
		void Modify(FileOperationType operationType, IList<IStorageItemWithPath> source, IList<IStorageItemWithPath> destination);
	}
}