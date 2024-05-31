// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Storage.WindowsStorage;
using Files.Core.Storage.LocatableStorage;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

namespace Files.App.Services
{
	/// <inheritdoc cref="IFileExplorerService"/>
	internal sealed class FileExplorerService : IFileExplorerService
	{
		/// <inheritdoc/>
		public Task OpenAppFolderAsync(CancellationToken cancellationToken = default)
			=> Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder).AsTask(cancellationToken);

		/// <inheritdoc/>
		public Task OpenInFileExplorerAsync(ILocatableFolder folder, CancellationToken cancellationToken = default)
			=> Launcher.LaunchFolderPathAsync(folder.Path).AsTask(cancellationToken);
	}
}
