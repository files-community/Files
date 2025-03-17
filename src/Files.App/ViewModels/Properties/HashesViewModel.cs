// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
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

		private HashInfoItem _selectedItem;
		public HashInfoItem SelectedItem
		{
			get => _selectedItem;
			set => SetProperty(ref _selectedItem, value);
		}

		public ObservableCollection<HashInfoItem> Hashes { get; set; }

		public Dictionary<string, bool> ShowHashes { get; private set; }

		public ICommand ToggleIsEnabledCommand { get; private set; }
		public ICommand HashInputTextChangedCommand { get; private set; }
		public ICommand CompareFileCommand { get; private set; }

		private ListedItem _item;

		private CancellationTokenSource _cancellationTokenSource;

		private string _hashInput;
		public string HashInput
		{
			get => _hashInput;
			set
			{
				OnHashInputTextChanged();
				SetProperty(ref _hashInput, value);
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

		private bool _isInfoBarOpen;
		public bool IsInfoBarOpen
		{
			get => _isInfoBarOpen;
			set => SetProperty(ref _isInfoBarOpen, value);
		}

		public HashesViewModel(ListedItem item)
		{
			ToggleIsEnabledCommand = new RelayCommand<string>(ToggleIsEnabled);

			_item = item;
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

			HashInputTextChangedCommand = new RelayCommand(OnHashInputTextChanged);
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
				hashInfoItem.HashValue = "CalculationOnlineFileHashError".GetLocalizedResource();
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
						hashInfoItem.HashValue = "CalculationErrorFileIsOpen".GetLocalizedResource();
					}
					catch (Exception)
					{
						hashInfoItem.HashValue = "CalculationError".GetLocalizedResource();
					}
					finally
					{
						hashInfoItem.IsCalculating = false;
					}
				});
			}
		}

		public bool CompareHash(string algorithm, string hashToCompare)
		{
			var hashInfoItem = Hashes.FirstOrDefault(x => x.Algorithm == algorithm);
			if (hashInfoItem == null || hashInfoItem.HashValue == null)
			{
				return false;
			}

			return hashInfoItem.HashValue.Equals(hashToCompare, StringComparison.OrdinalIgnoreCase);
		}

		public async Task<bool> CompareFileAsync()
		{
			var result = CommonDialogService.Open_FileOpenDialog(MainWindow.Instance.WindowHandle, false, [], Environment.SpecialFolder.Desktop, out var toCompare);

			if (!result)
			{
				return false;
			}

			var file = await StorageHelpers.ToStorageItem<BaseStorageFolder>(toCompare);

			if (file is not null)
			{
				var selectedFileHash = await CalculateFileHashAsync(file.Path);
				var compare = CompareHash("SHA384", selectedFileHash);

				return compare;
			}

			return false;
		}

		public string FindMatchingAlgorithm(string hash)
		{
			if (string.IsNullOrEmpty(hash))
				throw new ArgumentNullException($"Invalid hash '{hash}' provided.");

			foreach (var hashInfo in Hashes)
			{
				if (hashInfo.HashValue != null && hashInfo.HashValue.Equals(hash, StringComparison.OrdinalIgnoreCase))
				{
					return hashInfo.Algorithm;
				}
			}

			return string.Empty;
		}

		public async Task<string> CalculateFileHashAsync(string filePath)
		{
			using (var stream = File.OpenRead(filePath))
			{
				using (var md5 = MD5.Create())
				{
					var hash = await Task.Run(() => md5.ComputeHash(stream));
					return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
				}
			}
		}

		private void OnHashInputTextChanged()
		{
			string? matchingAlgorithm = null;

			try
			{
				matchingAlgorithm = FindMatchingAlgorithm(HashInput);
			}
			catch (ArgumentNullException)
			{
				return;
			}

			if (string.IsNullOrEmpty(matchingAlgorithm))
			{
				InfoBarSeverity = InfoBarSeverity.Error;
				InfoBarTitle = Strings.HashesDoNotMatch.GetLocalizedResource();
				IsInfoBarOpen = true;
			}
			else
			{
				InfoBarSeverity = InfoBarSeverity.Success;
				InfoBarTitle = string.Format(Strings.HashesMatch.GetLocalizedResource(), matchingAlgorithm);
				IsInfoBarOpen = true;
			}
		}

		private async Task OnCompareFileAsync()
		{
			var result = await CompareFileAsync();

			if (result)
			{
				InfoBarSeverity = InfoBarSeverity.Success; // Check mark
				InfoBarTitle = Strings.HashesMatch.GetLocalizedResource();
			}
			else
			{
				InfoBarSeverity = InfoBarSeverity.Error; // Cross mark
				InfoBarTitle = Strings.HashesDoNotMatch.GetLocalizedResource();
			}

			IsInfoBarOpen = true;
		}

		public void Dispose()
		{
			_cancellationTokenSource.Cancel();
		}
	}
}
