﻿using CommunityToolkit.Mvvm.ComponentModel;
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

		public HashesViewModel(ListedItem item)
		{
			_item = item;
			_cancellationTokenSource = new();
			_showHashesDictionary = UserSettingsService.PreferencesSettingsService.ShowHashesDictionary;

			Hashes = new()
			{
				new() { Algorithm = "MD5" },
				new() { Algorithm = "SHA1" },
				new() { Algorithm = "SHA256" },
				new() { Algorithm = "SHA384" },
				new() { Algorithm = "SHA512" },
			};
			Hashes.ForEach(x =>
			{
				x.PropertyChanged += HashInfoItem_PropertyChanged;
				if (_showHashesDictionary.TryGetValue(x.Algorithm, out var value)) {
					x.IsEnabled = value;
				} 
				else
				{
					x.IsEnabled = x.Algorithm switch
					{
						"MD5" => true,
						"SHA1" => true,
						"SHA256" => true,
						"SHA384" => false,
						"SHA512" => false,
						_ => throw new InvalidOperationException()
					};
				}
			});
		}

		private async void HashInfoItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is HashInfoItem hashInfoItem && e.PropertyName == nameof(HashInfoItem.IsEnabled))
			{
				if (!_showHashesDictionary.ContainsKey(hashInfoItem.Algorithm)
					|| _showHashesDictionary[hashInfoItem.Algorithm] != hashInfoItem.IsEnabled)
				{
					_showHashesDictionary[hashInfoItem.Algorithm] = hashInfoItem.IsEnabled;
					UserSettingsService.PreferencesSettingsService.ShowHashesDictionary = _showHashesDictionary;
				}

				if (hashInfoItem.HashValue is null && hashInfoItem.IsEnabled)
				{
					hashInfoItem.HashValue = "Calculating".GetLocalizedResource();
					try
					{
						using (var stream = File.OpenRead(_item.ItemPath))
						{
							hashInfoItem.HashValue = hashInfoItem.Algorithm switch
							{
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
					catch (Exception)
					{
						hashInfoItem.HashValue = "CalculatingError".GetLocalizedResource();
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
