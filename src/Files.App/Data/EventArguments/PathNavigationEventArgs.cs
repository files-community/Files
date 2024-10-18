// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments
{
	public sealed class PathNavigationEventArgs
	{
		public string ItemPath { get; set; }

		public string ItemName { get; set; }

		public bool IsFile { get; set; }
	}
}
