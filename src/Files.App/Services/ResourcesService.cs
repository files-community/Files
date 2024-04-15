﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using System.Drawing;

namespace Files.App.Services
{
	/// <inheritdoc cref="IResourcesService"/>
	public sealed class ResourcesService : IResourcesService
	{
		private IAppThemeModeService AppThemeModeService { get; } = Ioc.Default.GetRequiredService<IAppThemeModeService>();
		
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
		public void ApplyResources()
		{
			AppThemeModeService.ApplyResources();
		}
	}
}
