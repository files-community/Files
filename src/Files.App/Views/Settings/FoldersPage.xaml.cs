// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.Settings
{
	public sealed partial class FoldersPage : Page
	{
		private FoldersViewModel ViewModel => DataContext as FoldersViewModel;

		public FoldersPage()
		{
			DataContext = Ioc.Default.GetRequiredService<FoldersViewModel>();

			InitializeComponent();
		}
	}
}
