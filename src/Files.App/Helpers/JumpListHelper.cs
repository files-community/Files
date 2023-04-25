// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.UserControls.Widgets;
using Files.Shared.Services;
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
				App.QuickAccessManager.UpdateQuickAccessWidget -= UpdateQuickAccessWidget;
				App.QuickAccessManager.UpdateQuickAccessWidget += UpdateQuickAccessWidget;

				await jumpListService.RefreshPinnedFoldersAsync();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
		}

		private static async void UpdateQuickAccessWidget(object? sender, ModifyQuickAccessEventArgs e)
		{
			await jumpListService.RefreshPinnedFoldersAsync();
		}
	}
}