// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.ViewModels.FileTags;
using System.Collections.Specialized;

namespace Files.Core.Services
{
	/// <summary>
	/// Represents a service used to manage file tags.
	/// </summary>
	public interface IFileTagsService
	{
		event EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		/// <summary>
		/// Gets a list of file tags
		/// </summary>
		IList<TagViewModel> FileTagList { get; }

		/// <summary>
		/// Gets the path indicates file tags database file.
		/// </summary>
		string? FileTagsDatabasePath { get; }

		/// <summary>
		/// Get single instance of file tags database.
		/// </summary>
		/// <returns></returns>
		FileTagsDb GetFileTagsDatabaseInstance();

		/// <summary>
		/// Gets tag set for a path.
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		string[] GetFileTagForPath(string filePath);

		/// <summary>
		/// Sets or updates tag values for a given <paramref name="storable"/>.
		/// </summary>
		/// <param name="path">The path to update tags for.</param>
		/// <param name="tags">The tag UIDs to set.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If successful, returns true, otherwise false.</returns>
		Task<bool> SetFileTagForPathAsync(string path, string[] tags, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all tags which are used to tag files and folders from the database.
		/// </summary>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="string"/> that represents all tags.</returns>
		IAsyncEnumerable<TagViewModel> GetAllTagsAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all items that are tagged with a specific <paramref name="tag"/>.
		/// </summary>
		/// <param name="tagUid">The UID of a tag.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="TaggedItemModel"/>.</returns>
		IAsyncEnumerable<TaggedItemModel> GetStorableItemsForFileTagAsync(string tagUid, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all file/folder tags from the database.
		/// </summary>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="TaggedItemModel"/> of tags saved in the database.</returns>
		IAsyncEnumerable<TaggedItemModel> GetStorableItemsForAllFileTagsAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Updates list of file tags
		/// </summary>
		Task UpdateFileTagsListAsync();

		/// <summary>
		/// Updates the file tags database instance.
		/// </summary>
		void UpdateFileTagsDatabase();
	}
}
