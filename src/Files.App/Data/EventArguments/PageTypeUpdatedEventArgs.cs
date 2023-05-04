// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments
{
	public class PageTypeUpdatedEventArgs
	{
		public bool IsTypeCloudDrive { get; set; }

		public bool IsTypeRecycleBin { get; set; }
	}
}
