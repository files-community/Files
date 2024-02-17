// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public sealed class FileTagsWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Dependency injections

		private IFileTagsService FileTagsService { get; } = Ioc.Default.GetRequiredService<IFileTagsService>();

		// Fields

		private readonly SemaphoreSlim _refreshItemsSemaphore;
		private CancellationTokenSource _refreshItemsCTS;

		// Properties

		public ObservableCollection<WidgetFileTagsContainerItem> Containers { get; } = [];

		public string WidgetName => nameof(FileTagsWidgetViewModel);
		public string WidgetHeader => "FileTags".GetLocalizedResource();
		public string AutomationProperties => "FileTags".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem { get; } = null;

		// Events

		public static event EventHandler<IEnumerable<WidgetFileTagCardItem>>? SelectedTaggedItemsChanged;

		// Constructor

		public FileTagsWidgetViewModel()
		{
			_refreshItemsSemaphore = new(1, 1);
			_refreshItemsCTS = new();

			_ = RefreshWidgetAsync();
		}

		// Methods

		public async Task RefreshWidgetAsync()
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				try
				{
					await _refreshItemsSemaphore.WaitAsync(_refreshItemsCTS.Token);
				}
				catch (OperationCanceledException)
				{
					return;
				}

				try
				{
					// Drop other waiting instances
					_refreshItemsCTS.Cancel();
					_refreshItemsCTS.TryReset();

					await foreach (var item in FileTagsService.GetTagsAsync())
					{
						var container = new WidgetFileTagsContainerItem()
						{
							TagId = item.Uid,
							Name = item.Name,
							Color = item.Color
						};

						Containers.Add(container);

						// Initialize inner tag items
						_ = container.InitializeAsync();
					}
				}
				catch (Exception ex)
				{
					App.Logger.LogInformation(ex, "Could not populate file tags containers.");
				}
				finally
				{
					_refreshItemsSemaphore.Release();
				}
			});
		}

		// Disposer

		public void Dispose()
		{
		}
	}
}
