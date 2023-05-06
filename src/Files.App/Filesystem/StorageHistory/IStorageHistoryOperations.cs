// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Filesystem.FilesystemHistory
{
	public interface IStorageHistoryOperations : IDisposable
	{
		/// <summary>
		/// Redo an action with given <paramref name="history"/>
		/// </summary>
		/// <param name="history"></param>
		/// <returns></returns>
		Task<ReturnResult> Undo(IStorageHistory history);

		/// <summary>
		/// Redo an action with given <paramref name="history"/>
		/// </summary>
		/// <param name="history"></param>
		/// <returns></returns>
		Task<ReturnResult> Redo(IStorageHistory history);
	}
}