// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Threading;
using System.Threading.Tasks;

namespace Files.Sdk.Storage.MutableStorage
{
	/// <summary>
	/// Represents a folder whose content can change.
	/// </summary>
	public interface IMutableFolder
	{
		/// <summary>
		/// Asynchronously retrieves a disposable object which can notify of changes to the folder.
		/// </summary>
		/// <returns>A Task representing the asynchronous operation. The result is a disposable object which can notify of changes to the folder.</returns>
		public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default);
	}
}
