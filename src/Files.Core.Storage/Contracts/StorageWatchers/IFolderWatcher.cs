// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Specialized;

namespace Files.Core.Storage.Contracts
{
	public interface IFolderWatcher : INotifyCollectionChanged, IDisposable, IAsyncDisposable
	{
		/// <summary>
		/// Gets the folder being watched for changes.
		/// </summary>
		IMutableFolder TargetFolder { get; }
	}
}
