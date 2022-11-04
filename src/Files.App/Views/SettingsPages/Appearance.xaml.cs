using CommunityToolkit.WinUI.UI;
using Files.App.Dialogs;
using Files.App.Helpers.XamlHelpers;
using Files.App.UserControls.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.SettingsPages
{
	public sealed partial class Appearance : Page
	{
		public Appearance()
		{
			InitializeComponent();
			Loaded += Appearance_Loaded;
		}

		private void Appearance_Loaded(object sender, RoutedEventArgs e)
		{
			for (int i = 0; i < ViewModel.CustomThemes.Count; i++)
			{
				if (ViewModel.CustomThemes[i].Path == ViewModel.SelectedTheme.Path)
				{
					AppThemeSelectionGridView.SelectedIndex = i;
				}
			}
		}

		private void ThemesLearnMoreButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			ThemesTeachingTip.IsOpen = true;
		}

		private async void OpenThemesFolderButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			ThemesTeachingTip.IsOpen = false;
			this.FindAscendant<SettingsDialog>()?.Hide();
			await ViewModel.OpenThemesFolder();
		}

		private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ViewModel.SelectedElementTheme))
			{
				foreach (var theme in ViewModel.CustomThemes)
				{
					var container = AppThemeSelectionGridView.ContainerFromItem(theme);
					if (container is not null)
					{
						var item = DependencyObjectHelpers.FindChild<ThemeSampleDisplayControl>(container);
						if (item is not null)
						{
							await item.ReevaluateThemeResourceBinding();
						}
					}
				}
			}
		}

		private void AppThemeSelectionGridView_Loaded(object sender, RoutedEventArgs e)
		{
			ViewModel.IsLoadingThemes = true;
			DispatcherQueue.TryEnqueue(async () =>
			{
				await App.ExternalResourcesHelper.LoadOtherThemesAsync();
				ViewModel.IsLoadingThemes = false;
			});
		}
	}
}