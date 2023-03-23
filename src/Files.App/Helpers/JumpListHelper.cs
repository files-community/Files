using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Shared.Services;
using System;
using System.Threading.Tasks;

namespace Files.App.Helpers;

public sealed class JumpListHelper
{
	public static async Task InitializeUpdatesAsync()
	{
		var jumpListService = Ioc.Default.GetRequiredService<IJumpListService>();

		try
		{
			App.QuickAccessManager.UpdateQuickAccessWidget += async (sender, args) => await jumpListService.RefreshPinnedFoldersAsync();

			await jumpListService.RefreshPinnedFoldersAsync();
		}
		catch (Exception ex)
		{
			App.Logger.Warn(ex, ex.Message);
		}
	}
}