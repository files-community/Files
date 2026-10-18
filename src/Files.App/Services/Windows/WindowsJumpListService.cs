// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services;

public sealed class WindowsJumpListService : IWindowsJumpListService
{
	public async Task InitializeAsync()
	{
		App.QuickAccessManager.UpdateQuickAccessWidget -= OnQuickAccessChanged;
		App.QuickAccessManager.UpdateQuickAccessWidget += OnQuickAccessChanged;
		await RefreshPinnedFoldersAsync();
	}

	public async Task AddFolderAsync(string path)
		=> await SafetyExtensions.IgnoreExceptions(async () => await Server.AppService.AddJumpListFolderAsync(path), App.Logger);

	public async Task RemoveFolderAsync(string path)
		=> await SafetyExtensions.IgnoreExceptions(async () => await Server.AppService.RemoveJumpListFolderAsync(path), App.Logger);

	public async Task<IEnumerable<string>> GetFoldersAsync()
		=> (await Server.AppService.GetJumpListFoldersAsync()).ToArray();

	public async Task RefreshPinnedFoldersAsync()
		=> await SafetyExtensions.IgnoreExceptions(async () => await Server.AppService.RefreshJumpListAsync(), App.Logger);

	private async void OnQuickAccessChanged(object? sender, ModifyQuickAccessEventArgs e)
		=> await RefreshPinnedFoldersAsync();
}
