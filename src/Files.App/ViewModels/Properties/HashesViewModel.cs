// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Input;

namespace Files.App.ViewModels.Properties
{
	public sealed partial class HashesViewModel : ObservableObject, IDisposable
	{
		private ICommonDialogService CommonDialogService { get; } = Ioc.Default.GetRequiredService<ICommonDialogService>();
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>()!;

		private readonly AppWindow _appWindow;

		private HashInfoItem _selectedItem;
		public HashInfoItem SelectedItem
		{
			get => _selectedItem;
			set => SetProperty(ref _selectedItem, value);
		}

		public ObservableCollection<HashInfoItem> Hashes { get; set; }

		public Dictionary<string, bool> ShowHashes { get; private set; }

		public ICommand ToggleIsEnabledCommand { get; private set; }
		public ICommand CompareFileCommand { get; private set; }

		private ListedItem _item;

		private CancellationTokenSource _cancellationTokenSource;

		private string _hashInput;
		public string HashInput
		{
			get => _hashInput;
			set
			{
				SetProperty(ref _hashInput, value);

				OnHashInputTextChanged();
				OnPropertyChanged(nameof(IsInfoBarOpen));
			}
		}

		private InfoBarSeverity _infoBarSeverity;
		public InfoBarSeverity InfoBarSeverity
		{
			get => _infoBarSeverity;
			set => SetProperty(ref _infoBarSeverity, value);
		}

		private string _infoBarTitle;
		public string InfoBarTitle
		{
			get => _infoBarTitle;
			set => SetProperty(ref _infoBarTitle, value);
		}

		public bool IsInfoBarOpen
			=> !string.IsNullOrEmpty(HashInput);

		public HashesViewModel(ListedItem item, AppWindow appWindow)
		{
			ToggleIsEnabledCommand = new RelayCommand<string>(ToggleIsEnabled);

			_item = item;
			_appWindow = appWindow;
			_cancellationTokenSource = new();

			Hashes =
			[
				new() { Algorithm = "CRC32" },
				new() { Algorithm = "MD5" },
				new() { Algorithm = "SHA1" },
				new() { Algorithm = "SHA256" },
				new() { Algorithm = "SHA384" },
				new() { Algorithm = "SHA512" },
			];

			ShowHashes = UserSettingsService.GeneralSettingsService.ShowHashesDictionary ?? [];
			// Default settings
			ShowHashes.TryAdd("CRC32", true);
			ShowHashes.TryAdd("MD5", true);
			ShowHashes.TryAdd("SHA1", true);
			ShowHashes.TryAdd("SHA256", true);
			ShowHashes.TryAdd("SHA384", false);
			ShowHashes.TryAdd("SHA512", false);

			Hashes.Where(x => ShowHashes[x.Algorithm]).ForEach(x => ToggleIsEnabledCommand.Execute(x.Algorithm));

			CompareFileCommand = new RelayCommand(async () => await OnCompareFileAsync());
		}

		private void ToggleIsEnabled(string? algorithm)
		{
			var hashInfoItem = Hashes.First(x => x.Algorithm == algorithm);
			hashInfoItem.IsEnabled = !hashInfoItem.IsEnabled;

			if (ShowHashes[hashInfoItem.Algorithm] != hashInfoItem.IsEnabled)
			{
				ShowHashes[hashInfoItem.Algorithm] = hashInfoItem.IsEnabled;
				UserSettingsService.GeneralSettingsService.ShowHashesDictionary = ShowHashes;
			}

			// Don't calculate hashes for online files
			if (_item.SyncStatusUI.SyncStatus is CloudDriveSyncStatus.FileOnline or CloudDriveSyncStatus.FolderOnline)
			{
				hashInfoItem.HashValue = Strings.CalculationOnlineFileHashError.GetLocalizedResource();
				return;
			}

			if (hashInfoItem.HashValue is null && hashInfoItem.IsEnabled)
			{
				hashInfoItem.IsCalculating = true;

				MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					try
					{
						await using (var stream = File.OpenRead(_item.ItemPath))
						{
							hashInfoItem.HashValue = hashInfoItem.Algorithm switch
							{
								"CRC32" => await ChecksumHelpers.CreateCRC32(stream, _cancellationTokenSource.Token),
								"MD5" => await ChecksumHelpers.CreateMD5(stream, _cancellationTokenSource.Token),
								"SHA1" => await ChecksumHelpers.CreateSHA1(stream, _cancellationTokenSource.Token),
								"SHA256" => await ChecksumHelpers.CreateSHA256(stream, _cancellationTokenSource.Token),
								"SHA384" => await ChecksumHelpers.CreateSHA384(stream, _cancellationTokenSource.Token),
								"SHA512" => await ChecksumHelpers.CreateSHA512(stream, _cancellationTokenSource.Token),
								_ => throw new InvalidOperationException()
							};
						}

						hashInfoItem.IsCalculated = true;
					}
					catch (OperationCanceledException)
					{
						// not an error
					}
					catch (IOException)
					{
						// File is currently open
						hashInfoItem.HashValue = Strings.CalculationErrorFileIsOpen.GetLocalizedResource();
					}
					catch (Exception)
					{
						hashInfoItem.HashValue = Strings.CalculationError.GetLocalizedResource();
					}
					finally
					{
						hashInfoItem.IsCalculating = false;
					}
				});
			}
		}

		public string FindMatchingAlgorithm(string hash)
		{
			if (string.IsNullOrEmpty(hash))
				return string.Empty;

			return Hashes.FirstOrDefault(h => h.HashValue?.Equals(hash, StringComparison.OrdinalIgnoreCase) == true)?.Algorithm ?? string.Empty;
		}

		public async Task<string> CalculateFileHashAsync(string filePath)
		{
			using var stream = File.OpenRead(filePath);
			using var md5 = MD5.Create();
			var hash = await Task.Run(() => md5.ComputeHash(stream));
			return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
		}

		private void OnHashInputTextChanged()
		{
			string matchingAlgorithm = FindMatchingAlgorithm(HashInput);

			InfoBarSeverity = string.IsNullOrEmpty(matchingAlgorithm)
				? InfoBarSeverity.Error
				: InfoBarSeverity.Success;

			InfoBarTitle = string.IsNullOrEmpty(matchingAlgorithm)
				? Strings.HashesDoNotMatch.GetLocalizedResource()
				: string.Format(Strings.HashesMatch.GetLocalizedResource(), matchingAlgorithm);
		}

		private async Task OnCompareFileAsync()
		{
			var hWnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(_appWindow.Id);

			var result = CommonDialogService.Open_FileOpenDialog(
				hWnd,
				false,
				[],
				Environment.SpecialFolder.Desktop,
				out var filePath);

			HashInput = result && filePath != null
				? await CalculateFileHashAsync(filePath)
				: string.Empty;
		}

		public void Dispose()
		{
			_cancellationTokenSource.Cancel();
		}
	}
}
