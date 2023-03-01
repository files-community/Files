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
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.ViewModels.Properties
{
	public class HashViewModel
	{
		public HashViewModel(ListedItem item)
		{
			Item = item;

			_hashes = new();
			Hashes = new(_hashes);

			GetHashes();
		}

		public ListedItem Item { get; }

		private readonly ObservableCollection<HashInfoItem> _hashes;
		public ReadOnlyObservableCollection<HashInfoItem> Hashes { get; }

		private void GetHashes()
		{
			// MD5

			// SHA-256

			// SHA-512
		}
	}
}
