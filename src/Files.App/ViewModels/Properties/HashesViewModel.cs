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

namespace Files.App.ViewModels.Properties
{
	public class HashesViewModel : ObservableObject
	{
		public HashesViewModel(ListedItem item)
		{
			Item = item;

			Hashes = new();

			CopyHashValueCommand = new(ExecuteCopyHashValue, () => SelectedItem is not null);

			try
			{
				_fileStream = File.OpenRead(item.ItemPath);
				CanAccessFile = true;

				GetHashes();
			}
			catch
			{
				CanAccessFile = false;
			}
		}

		public ListedItem Item { get; }

		private readonly FileStream _fileStream;

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

		public RelayCommand CopyHashValueCommand { get; set; }

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
			var hashBytes = MD5.HashData(_fileStream);

			return Convert.ToHexString(hashBytes);
		}

		private string CreateSHA1()
		{
			var hashBytes = SHA1.HashData(_fileStream);

			return Convert.ToHexString(hashBytes);
		}

		private string CreateSHA256()
		{
			var hashBytes = SHA256.HashData(_fileStream);

			return Convert.ToHexString(hashBytes);
		}

		private string CreateSHA384()
		{
			var hashBytes = SHA384.HashData(_fileStream);

			return Convert.ToHexString(hashBytes);
		}

		private string CreateSHA512()
		{
			var hashBytes = SHA512.HashData(_fileStream);

			return Convert.ToHexString(hashBytes);
		}

		private void ExecuteCopyHashValue()
		{
			var dp = new DataPackage();
			dp.SetText(SelectedItem.HashValue);
			Clipboard.SetContent(dp);
		}
	}
}
