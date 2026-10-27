// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents a default column of <see cref="DetailsLayoutPage"/> shown in settings.
	/// </summary>
	public sealed partial class DetailsLayoutColumnSettingItem(string id, string displayName) : ObservableObject
	{
		public string Id { get; } = id;

		public bool CanRemove => Id != DetailsLayoutColumnIds.Name;

		private string _DisplayName = displayName;
		public string DisplayName
		{
			get => _DisplayName;
			set => SetProperty(ref _DisplayName, value);
		}
	}
}
