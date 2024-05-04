// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Data.Contexts
{
	internal sealed class WindowContext : ObservableObject, IWindowContext
	{
		private bool _IsCompactOverlay;
		public bool IsCompactOverlay
		{
			get => _IsCompactOverlay;
			set => SetProperty(ref _IsCompactOverlay, value);
		}

		private int _TabStripSelectedIndex = 0;
		public int TabStripSelectedIndex
		{
			get => _TabStripSelectedIndex;
			set
			{
				SetProperty(ref _TabStripSelectedIndex, value);

				if (value >= 0 && value < MainPageViewModel.AppInstances.Count)
				{
					Frame rootFrame = (Frame)MainWindow.Instance.Content;
					var mainView = (MainPage)rootFrame.Content;
					mainView.ViewModel.SelectedTabItem = MainPageViewModel.AppInstances[value];
				}
			}
		}

		private bool _IsAppElevated = false;
		public bool IsAppElevated
		{
			get => _IsAppElevated;
			set => SetProperty(ref _IsAppElevated, value);
		}

		private bool _IsPasteEnabled = false;
		public bool IsPasteEnabled
		{
			get => _IsPasteEnabled;
			set => SetProperty(ref _IsPasteEnabled, value);
		}

		private volatile int _IsMainWindowClosed = 0;
		public bool IsMainWindowClosed
		{
			get => _IsMainWindowClosed == 1;
			set
			{
				int original = Interlocked.Exchange(ref _IsMainWindowClosed, value ? 1 : 0);
				if (_IsMainWindowClosed != original)
					OnPropertyChanged();
			}
		}

		private int _PropertiesWindowCount = 0;
		public int PropertiesWindowCount
			=> _PropertiesWindowCount;

		private bool _ForceProcessTermination = false;
		public bool ForceProcessTermination
		{
			get => _ForceProcessTermination;
			set => SetProperty(ref _ForceProcessTermination, value);
		}

		private string _GoogleDrivePath = string.Empty;
		/// <inheritdoc/>
		public string GoogleDrivePath
		{
			get => _GoogleDrivePath;
			set => SetProperty(ref _GoogleDrivePath, value);
		}

		private string _PCloudDrivePath = string.Empty;
		/// <inheritdoc/>
		public string PCloudDrivePath
		{
			get => _PCloudDrivePath;
			set => SetProperty(ref _PCloudDrivePath, value);
		}

		private float _AppWindowDPI = Win32PInvoke.GetDpiForWindow(MainWindow.Instance.WindowHandle) / 96f;
		/// <inheritdoc/>
		public float AppWindowDPI
		{
			get => _AppWindowDPI;
			set => SetProperty(ref _AppWindowDPI, value);
		}

		// Constructor

		public WindowContext()
		{
			MainWindow.Instance.PresenterChanged += Window_PresenterChanged;
			Clipboard.ContentChanged += Clipboard_ContentChanged;
		}

		// Methods

		public int IncrementPropertiesWindowCount()
		{
			var result = Interlocked.Increment(ref _PropertiesWindowCount);
			OnPropertyChanged(nameof(PropertiesWindowCount));
			return result;
		}

		public int DecrementPropertiesWindowCount()
		{
			var result = Interlocked.Decrement(ref _PropertiesWindowCount);
			OnPropertyChanged(nameof(PropertiesWindowCount));
			return result;
		}

		// Event methods

		private void Window_PresenterChanged(object? sender, AppWindowPresenter e)
		{
			IsCompactOverlay = e.Kind is AppWindowPresenterKind.CompactOverlay;
		}

		private void Clipboard_ContentChanged(object? sender, object e)
		{
			// TODO: Refactor here

			try
			{
				DataPackageView packageView = Clipboard.GetContent();
				IsPasteEnabled = packageView.Contains(StandardDataFormats.StorageItems) || packageView.Contains(StandardDataFormats.Bitmap);
			}
			catch
			{
				IsPasteEnabled = false;
			}
		}
	}
}
