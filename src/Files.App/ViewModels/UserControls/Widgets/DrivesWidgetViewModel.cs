// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents view model of <see cref="DrivesWidget"/>.
	/// </summary>
	public sealed class DrivesWidgetViewModel
	{
		public ObservableCollection<WidgetDriveCardItem> Items { get; } = [];
	}
}
