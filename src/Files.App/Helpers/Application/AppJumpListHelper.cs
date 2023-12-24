// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;
using Microsoft.Extensions.Logging;

namespace Files.App.Helpers
{
	public sealed class AppJumpListHelper
	{
		private static IJumpListService JumpListService { get; } = Ioc.Default.GetRequiredService<IJumpListService>();

		public static async Task InitializeUpdatesAsync()
		{
			try
			{
				App.QuickAccessManager.UpdateQuickAccessWidget -= UpdateQuickAccessWidgetAsync;
				App.QuickAccessManager.UpdateQuickAccessWidget += UpdateQuickAccessWidgetAsync;

				await JumpListService.RefreshPinnedFoldersAsync();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
		}

		private static async void UpdateQuickAccessWidgetAsync(object? sender, ModifyQuickAccessEventArgs e)
		{
			await JumpListService.RefreshPinnedFoldersAsync();
		}
	}
}
