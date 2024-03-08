// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents view model of <see cref="QuickAccessWidget"/>.
	/// </summary>
	public class QuickAccessWidgetViewModel
	{
		public ObservableCollection<WidgetFolderCardItem> Items { get; } = [];
	}
}
