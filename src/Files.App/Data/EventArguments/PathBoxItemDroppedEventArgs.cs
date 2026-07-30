// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Data.EventArguments
{
	public sealed class PathBoxItemDroppedEventArgs
	{
		public required DataPackageView Package { get; set; }

		public required string Path { get; set; }

		public DataPackageOperation AcceptedOperation { get; set; }

		public required AsyncManualResetEvent SignalEvent { get; set; }
	}
}
