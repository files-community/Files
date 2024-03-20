// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents view model of <see cref="RecentFilesWidget"/>.
	/// </summary>
	public sealed class RecentFilesWidgetViewModel
	{
		public ObservableCollection<RecentItem> Items { get; } = [];
	}
}
