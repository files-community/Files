// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Server.Data
{
	public sealed class ColumnPreferences
	{
		public ColumnPreferencesItem GitStatusColumn { get; set; } = new();
		public ColumnPreferencesItem GitLastCommitDateColumn { get; set; } = new();
		public ColumnPreferencesItem GitLastCommitMessageColumn { get; set; } = new();
		public ColumnPreferencesItem GitCommitAuthorColumn { get; set; } = new();
		public ColumnPreferencesItem GitLastCommitShaColumn { get; set; } = new();
		public ColumnPreferencesItem TagColumn { get; set; } = new();
		public ColumnPreferencesItem NameColumn { get; set; } = new();
		public ColumnPreferencesItem StatusColumn { get; set; } = new();
		public ColumnPreferencesItem DateModifiedColumn { get; set; } = new();
		public ColumnPreferencesItem PathColumn { get; set; } = new();
		public ColumnPreferencesItem OriginalPathColumn { get; set; } = new();
		public ColumnPreferencesItem ItemTypeColumn { get; set; } = new();
		public ColumnPreferencesItem DateDeletedColumn { get; set; } = new();
		public ColumnPreferencesItem DateCreatedColumn { get; set; } = new();
		public ColumnPreferencesItem SizeColumn { get; set; } = new();
	}
}
