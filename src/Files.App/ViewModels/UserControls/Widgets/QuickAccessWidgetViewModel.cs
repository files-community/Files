// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents view model of <see cref="QuickAccessWidget"/>.
	/// </summary>
	public sealed class QuickAccessWidgetViewModel
	{
		public ObservableCollection<WidgetFolderCardItem> Items { get; } = [];
	}
}
