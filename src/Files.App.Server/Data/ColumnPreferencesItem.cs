// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Server.Data
{
	public sealed class ColumnPreferencesItem
	{
		public double UserLengthPixels { get; set; }
		public double NormalMaxLength { get; set; } = 800;
		public bool UserCollapsed { get; set; }
	}
}
