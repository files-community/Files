// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using System.Drawing;

namespace Files.App.Services
{
	/// <inheritdoc cref="IResourcesService"/>
	public class ResourcesService : IResourcesService
	{
		/// <inheritdoc/>
		public void SetAppThemeBackgroundColor(Color appThemeBackgroundColor)
		{
			Application.Current.Resources["App.Theme.BackgroundBrush"] = appThemeBackgroundColor.ToWindowsColor();
		}

		/// <inheritdoc/>
		public void SetAppThemeAddressBarBackgroundColor(Color appThemeAddressBarBackgroundColor)
		{
			Application.Current.Resources["App.Theme.AddressBar.BackgroundBrush"] = appThemeAddressBarBackgroundColor.ToWindowsColor();

			// Overrides the selected tab background to match the address bar
			Application.Current.Resources["TabViewItemHeaderBackgroundSelected"] = appThemeAddressBarBackgroundColor.ToWindowsColor();
		}

		/// <inheritdoc/>
		public void SetAppThemeSidebarBackgroundColor(Color appThemeSidebarBackgroundColor)
		{
			Application.Current.Resources["App.Theme.Sidebar.BackgroundBrush"] = appThemeSidebarBackgroundColor.ToWindowsColor();
		}

		/// <inheritdoc/>
		public void SetAppThemeFileAreaBackgroundColor(Color appThemeFileAreaBackgroundColor)
		{
			Application.Current.Resources["App.Theme.FileArea.BackgroundBrush"] = appThemeFileAreaBackgroundColor.ToWindowsColor();
		}

		/// <inheritdoc/>
		public void SetAppThemeFontFamily(string contentControlThemeFontFamily)
		{
			Application.Current.Resources["ContentControlThemeFontFamily"] = contentControlThemeFontFamily;
		}

		/// <inheritdoc/>
		public void SetCompactSpacing(bool useCompactSpacing)
		{
			var listItemHeight = useCompactSpacing ? 28 : 36;
			var listItemMargin = useCompactSpacing ? "-2" : "0";

			Application.Current.Resources["ListItemHeight"] = listItemHeight;
			Application.Current.Resources["ListItemMargin"] = listItemMargin;
		}

		/// <inheritdoc/>
		public void ApplyResources()
		{
			ThemeHelper.ApplyResources();
		}
	}
}