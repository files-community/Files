// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Data.EventArguments
{
	public class PathBoxItemDroppedEventArgs
	{
		public DataPackageView Package { get; set; }

		public string Path { get; set; }

		public DataPackageOperation AcceptedOperation { get; set; }

		public AsyncManualResetEvent SignalEvent { get; set; }
	}
}
