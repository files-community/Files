﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Items
{
	public class ShellLinkItem : ShellFileItem
	{
		public string TargetPath { get; set; }

		public string Arguments { get; set; }

		public string WorkingDirectory { get; set; }

		public bool RunAsAdmin { get; set; }

		public bool InvalidTarget { get; set; }

		public ShellLinkItem()
		{
		}

		public ShellLinkItem(ShellFileItem baseItem)
		{
			RecyclePath = baseItem.RecyclePath;
			FileName = baseItem.FileName;
			FilePath = baseItem.FilePath;
			RecycleDate = baseItem.RecycleDate;
			ModifiedDate = baseItem.ModifiedDate;
			CreatedDate = baseItem.CreatedDate;
			FileSize = baseItem.FileSize;
			FileSizeBytes = baseItem.FileSizeBytes;
			FileType = baseItem.FileType;
			PIDL = baseItem.PIDL;
		}
	}
}
