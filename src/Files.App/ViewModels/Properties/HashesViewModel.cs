using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.Permissions;
using Files.App.Helpers;
using Files.Backend.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Dispatching;
using CommunityToolkit.WinUI;

namespace Files.App.ViewModels.Properties
{
	public class HashesViewModel : ObservableObject
	{
		public HashesViewModel(ListedItem item)
		{
			Item = item;

			Hashes = new();

			CanAccessFile = true;

			LoadFileContent = new(ExecuteLoadFileContent);

			if (LoadFileContent.CanExecute(null))
				LoadFileContent.Execute(null);
		}

		public ListedItem Item { get; }

		private byte[] _fileData;

		private bool _canAccessFile;
		public bool CanAccessFile
		{
			get => _canAccessFile;
			set => SetProperty(ref _canAccessFile, value);
		}

		private HashInfoItem _selectedItem;
		public HashInfoItem SelectedItem
		{
			get => _selectedItem;
			set => SetProperty(ref _selectedItem, value);
		}

		public ObservableCollection<HashInfoItem> Hashes { get; set; }

		public AsyncRelayCommand LoadFileContent { get; set; }

		private bool _isLoading;
		public bool IsLoading
		{
			get => _isLoading;
			set => SetProperty(ref _isLoading, value);
		}

		private void GetHashes()
		{
			Hashes.Add(new()
			{
				Algorithm = "MD5",
				HashValue = CreateMD5(),
			});
			Hashes.Add(new()
			{
				Algorithm = "SHA1",
				HashValue = CreateSHA1(),
			});
			Hashes.Add(new()
			{
				Algorithm = "SHA256",
				HashValue = CreateSHA256(),
			});
			Hashes.Add(new()
			{
				Algorithm = "SHA384",
				HashValue = CreateSHA384(),
			});
			Hashes.Add(new()
			{
				Algorithm = "SHA512",
				HashValue = CreateSHA512(),
			});
		}

		private string CreateMD5()
		{
			var hashBytes = MD5.HashData(_fileData);

			return Convert.ToHexString(hashBytes);
		}

		private string CreateSHA1()
		{
			var hashBytes = SHA1.HashData(_fileData);

			return Convert.ToHexString(hashBytes);
		}

		private string CreateSHA256()
		{
			var hashBytes = SHA256.HashData(_fileData);

			return Convert.ToHexString(hashBytes);
		}

		private string CreateSHA384()
		{
			var hashBytes = SHA384.HashData(_fileData);

			return Convert.ToHexString(hashBytes);
		}

		private string CreateSHA512()
		{
			var hashBytes = SHA512.HashData(_fileData);

			return Convert.ToHexString(hashBytes);
		}

		private async Task ExecuteLoadFileContent()
		{
			try
			{
				IsLoading = true;

				await App.Window.DispatcherQueue.EnqueueAsync(async () =>
				{
					_fileData = await File.ReadAllBytesAsync(Item.ItemPath);
				});

				CanAccessFile = true;
				GetHashes();
			}
			catch
			{
				CanAccessFile = false;
			}
			finally
			{
				IsLoading = false;
			}
		}
	}
}
