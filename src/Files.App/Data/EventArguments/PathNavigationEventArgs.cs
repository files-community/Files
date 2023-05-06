// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments
{
	public class PathNavigationEventArgs
	{
		public string ItemPath { get; set; }

		public string ItemName { get; set; }

		public bool IsFile { get; set; }
	}
}
