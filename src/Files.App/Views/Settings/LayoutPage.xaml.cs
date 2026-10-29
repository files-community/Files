// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using CommunityToolkit.WinUI;
using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Files.App.Views.Settings
{
	public sealed partial class LayoutPage : Page
	{
		public LayoutPage()
		{
			InitializeComponent();
			Loaded += (_, _) =>
			{
				ViewModel.StartListening();
				ViewModel.EditTemplateRequested += ViewModel_EditTemplateRequested;
			};
			Unloaded += (_, _) =>
			{
				ViewModel.StopListening();
				ViewModel.EditTemplateRequested -= ViewModel_EditTemplateRequested;
			};
		}

		private void ViewModel_EditTemplateRequested(object? sender, LayoutTemplateEditorViewModel editor)
		{
			this.FindAscendant<ScrollViewer>()?.ChangeView(null, 0, null, true);
			Frame.Navigate(typeof(LayoutTemplatePage), editor, new SuppressNavigationTransitionInfo());
		}
	}
}
