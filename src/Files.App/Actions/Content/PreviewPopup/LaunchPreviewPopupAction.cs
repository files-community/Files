// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Dispatching;
using Windows.Win32;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class LaunchPreviewPopupAction : ObservableObject, IAction
	{
		private const int KeyDownMask = 0x8000;
		private static readonly TimeSpan HoldToPreviewDuration = TimeSpan.FromMilliseconds(750);
		private static readonly TimeSpan HoldPollInterval = TimeSpan.FromMilliseconds(30);

		private readonly IContentPageContext context;
		private readonly IPreviewPopupService previewPopupService;
		private DispatcherQueueTimer? holdTimer;

		public string Label
			=> Strings.LaunchPreviewPopup.GetLocalizedResource();

		public string Description
			=> Strings.LaunchPreviewPopupDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Show;

		public HotKey HotKey
			=> new(Keys.Space);

		public bool IsExecutable =>
			context.SelectedItems.Count == 1 &&
			(!context.ShellPage?.SlimContentPage?.IsRenamingItem ?? false);

		public LaunchPreviewPopupAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			previewPopupService = Ioc.Default.GetRequiredService<IPreviewPopupService>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var provider = await previewPopupService.GetProviderAsync();
			if (provider is null)
				return;

			var itemPath = context.SelectedItem?.ItemPath;
			if (itemPath is null)
				return;

			await provider.TogglePreviewPopupAsync(itemPath);
			if (provider.SupportsHoldToPreview)
				WatchHotKeyHold(provider);
		}

		/// <summary>
		/// Closes the preview when the hotkey is released after being held.
		/// </summary>
		private void WatchHotKeyHold(IPreviewPopupProvider provider)
		{
			holdTimer?.Stop();

			var keys = Ioc.Default.GetRequiredService<ICommandManager>().LaunchPreviewPopup.HotKeys.Select(hotKey => (int)hotKey.Key).ToArray();
			if (!keys.Any(IsKeyDown) || DispatcherQueue.GetForCurrentThread() is not { } dispatcherQueue)
				return;

			var pressedAt = Environment.TickCount64;
			holdTimer = dispatcherQueue.CreateTimer();
			holdTimer.Interval = HoldPollInterval;
			holdTimer.Tick += async (timer, _) =>
			{
				if (keys.Any(IsKeyDown))
					return;

				timer.Stop();
				if (Environment.TickCount64 - pressedAt >= HoldToPreviewDuration.TotalMilliseconds)
					await provider.ClosePreviewAsync();
			};
			holdTimer.Start();
		}

		private static bool IsKeyDown(int key)
			=> (PInvoke.GetAsyncKeyState(key) & KeyDownMask) != 0;

		private async Task SwitchPopupPreviewAsync()
		{
			if (IsExecutable)
			{
				var provider = await previewPopupService.GetProviderAsync();
				if (provider is null)
					return;

				var itemPath = context.SelectedItem?.ItemPath;
				if (itemPath is not null)
					await provider.SwitchPreviewAsync(itemPath);
			}
		}

		public async void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					await SwitchPopupPreviewAsync();
					break;
			}
		}
	}
}
