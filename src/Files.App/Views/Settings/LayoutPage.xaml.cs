// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using CommunityToolkit.WinUI;
using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views.Settings
{
	public sealed partial class LayoutPage : Page
	{
		public LayoutPage()
		{
			InitializeComponent();
			Loaded += (_, _) =>
			{
				ViewModel.EditTemplateRequested += ViewModel_EditTemplateRequested;
				ViewModel.StartListening();
			};
			Unloaded += (_, _) =>
			{
				ViewModel.StopListening();
				ViewModel.EditTemplateRequested -= ViewModel_EditTemplateRequested;
			};
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			ViewModel.TemplateIdToEdit = e.NavigationMode is NavigationMode.New ? e.Parameter as string : null;
		}

		private void ViewModel_EditTemplateRequested(object? sender, LayoutTemplateEditorViewModel editor)
		{
			this.FindAscendant<ScrollViewer>()?.ChangeView(null, 0, null, true);
			Frame.Navigate(typeof(LayoutTemplatePage), editor, new SuppressNavigationTransitionInfo());
		}
	}
}
