// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Windows.UI;

namespace Files.App.Services
{
	/// <inheritdoc cref="IResourcesService"/>
	public sealed class ResourcesService : IResourcesService
	{
		private IAppThemeModeService AppThemeModeService { get; } = Ioc.Default.GetRequiredService<IAppThemeModeService>();
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public ResourcesService()
		{
			SetScrollInertiaEnabled(UserSettingsService.GeneralSettingsService.EnableSmoothScrolling);

			UserSettingsService.GeneralSettingsService.PropertyChanged += GeneralSettingsService_PropertyChanged;
		}

		private void GeneralSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IGeneralSettingsService.EnableSmoothScrolling))
			{
				SetScrollInertiaEnabled(UserSettingsService.GeneralSettingsService.EnableSmoothScrolling);
				ApplyResources();
			}
		}

		/// <inheritdoc/>
		public void SetAppThemeBackgroundColor(Color appThemeBackgroundColor)
		{
			Application.Current.Resources["App.Theme.BackgroundBrush"] = appThemeBackgroundColor;
		}

		/// <inheritdoc/>
		public void SetAppThemeAddressBarBackgroundColor(Color appThemeAddressBarBackgroundColor)
		{
			Application.Current.Resources["App.Theme.AddressBar.BackgroundBrush"] = appThemeAddressBarBackgroundColor;

			// Overrides the selected tab background to match the address bar
			Application.Current.Resources["TabViewItemHeaderBackgroundSelected"] = appThemeAddressBarBackgroundColor;
		}

		/// <inheritdoc/>
		public void SetAppThemeToolbarBackgroundColor(Color appThemeToolbarBackgroundColor)
		{
			Application.Current.Resources["App.Theme.Toolbar.BackgroundBrush"] = appThemeToolbarBackgroundColor;
		}

		/// <inheritdoc/>
		public void SetAppThemeSidebarBackgroundColor(Color appThemeSidebarBackgroundColor)
		{
			Application.Current.Resources["App.Theme.Sidebar.BackgroundBrush"] = appThemeSidebarBackgroundColor;
		}

		/// <inheritdoc/>
		public void SetAppThemeFileAreaBackgroundColor(Color appThemeFileAreaBackgroundColor)
		{
			Application.Current.Resources["App.Theme.FileArea.BackgroundBrush"] = appThemeFileAreaBackgroundColor;
		}

		/// <inheritdoc/>
		public void SetAppThemeFileAreaSecondaryBackgroundColor(Color appThemeFileAreaSecondaryBackgroundColor)
		{
			Application.Current.Resources["App.Theme.FileArea.SecondaryBackgroundBrush"] = appThemeFileAreaSecondaryBackgroundColor;
		}

		/// <inheritdoc/>
		public void SetAppThemeInfoPaneBackgroundColor(Color appThemeInfoPaneBackgroundColor)
		{
			Application.Current.Resources["App.Theme.InfoPane.BackgroundBrush"] = appThemeInfoPaneBackgroundColor;
		}

		/// <inheritdoc/>
		public void SetAppThemeFontFamily(string contentControlThemeFontFamily)
		{
			Application.Current.Resources["ContentControlThemeFontFamily"] = contentControlThemeFontFamily;
		}

		/// <inheritdoc/>
		public void SetScrollInertiaEnabled(bool enableScrollInertia)
		{
			Application.Current.Resources["App.ScrollInertiaEnabled"] = enableScrollInertia;
		}

		/// <inheritdoc/>
		public void ApplyResources()
		{
			AppThemeModeService.ApplyResources();
		}
	}
}
