// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.UserControls.Widgets;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Files.App.Helpers
{
	public sealed class JumpListHelper
	{
		private static IJumpListService jumpListService = Ioc.Default.GetRequiredService<IJumpListService>();

		public static async Task InitializeUpdatesAsync()
		{
			try
			{
				App.QuickAccessManager.UpdateQuickAccessWidget -= UpdateQuickAccessWidgetAsync;
				App.QuickAccessManager.UpdateQuickAccessWidget += UpdateQuickAccessWidgetAsync;

				await jumpListService.RefreshPinnedFoldersAsync();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
		}

		private static async void UpdateQuickAccessWidgetAsync(object? sender, ModifyQuickAccessEventArgs e)
		{
			await jumpListService.RefreshPinnedFoldersAsync();
		}
	}
}