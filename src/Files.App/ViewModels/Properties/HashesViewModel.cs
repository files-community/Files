using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.Backend.Models;
using Files.Backend.Services.Settings;
using Files.Shared.Extensions;
using Files.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace Files.App.ViewModels.Properties
{
	public class HashesViewModel : ObservableObject, IDisposable
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>()!;

		private HashInfoItem _selectedItem;
		public HashInfoItem SelectedItem
		{
			get => _selectedItem;
			set => SetProperty(ref _selectedItem, value);
		}

		public ObservableCollection<HashInfoItem> Hashes { get; set; }

		private ListedItem _item;

		private CancellationTokenSource _cancellationTokenSource;

		private Dictionary<string, bool> _showHashesDictionary;

		public void Initialize(ListedItem item)
		{
			_item = item;
			_cancellationTokenSource = new();
			_showHashesDictionary = UserSettingsService.PreferencesSettingsService.ShowHashesDictionary;

			Hashes = new();
			Hashes.Add(new()
			{
				Algorithm = "MD5",
			});
			Hashes.Add(new()
			{
				Algorithm = "SHA1",
			});
			Hashes.Add(new()
			{
				Algorithm = "SHA256",
			});
			Hashes.Add(new()
			{
				Algorithm = "SHA384",
			});
			Hashes.Add(new()
			{
				Algorithm = "SHA512",
			});
			Hashes.ForEach(x =>
			{
				x.PropertyChanged += HashInfoItem_PropertyChanged;
				x.IsEnabled = _showHashesDictionary[x.Algorithm];
			});
		}

		private async void HashInfoItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is HashInfoItem hashInfoItem && e.PropertyName == nameof(HashInfoItem.IsEnabled))
			{
				_showHashesDictionary[hashInfoItem.Algorithm] = hashInfoItem.IsEnabled;
				UserSettingsService.PreferencesSettingsService.ShowHashesDictionary = _showHashesDictionary;

				if (hashInfoItem.HashValue is null && hashInfoItem.IsEnabled)
				{
					hashInfoItem.HashValue = "Calculating".GetLocalizedResource();

					try
					{
						var stream = File.OpenRead(_item.ItemPath);
						switch (hashInfoItem.Algorithm)
						{
							case "MD5":
								hashInfoItem.HashValue = await ChecksumHelpers.CreateMD5(stream, _cancellationTokenSource.Token);
								break;

							case "SHA1":
								hashInfoItem.HashValue = await ChecksumHelpers.CreateSHA1(stream, _cancellationTokenSource.Token);
								break;

							case "SHA256":
								hashInfoItem.HashValue = await ChecksumHelpers.CreateSHA256(stream, _cancellationTokenSource.Token);
								break;

							case "SHA384":
								hashInfoItem.HashValue = await ChecksumHelpers.CreateSHA384(stream, _cancellationTokenSource.Token);
								break;

							case "SHA512":
								hashInfoItem.HashValue = await ChecksumHelpers.CreateSHA512(stream, _cancellationTokenSource.Token);
								break;
						}
						hashInfoItem.IsCalculated = true;
					}
					catch (Exception)
					{
						hashInfoItem.HashValue = "UnableToCalculateTheHashValue".GetLocalizedResource();
					}
				}
			}
		}

		public void Dispose()
		{
			_cancellationTokenSource.Cancel();
			Hashes.ForEach(x => x.PropertyChanged -= HashInfoItem_PropertyChanged);
		}
	}
}
