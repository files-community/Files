using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.Filesystem;
using Files.Backend.Models;
using Files.Shared.Helpers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.ViewModels.Properties
{
	public class HashesViewModel : ObservableObject, IDisposable
	{
		public HashesViewModel(ListedItem item)
		{
			Item = item;
			Hashes = new();
			CanAccessFile = true;
			CancellationTokenSource = new();
			LoadAndCalcHashesCommand = new(ExecuteLoadAndCalcHashesCommandAsync);

			//if (LoadFileContent.CanExecute(CancellationTokenSource.Token))
			//	LoadFileContent.Execute(CancellationTokenSource.Token);
		}

		#region Fields and Properties
		public ListedItem Item { get; }

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

		private Stream _stream;

		public AsyncRelayCommand LoadAndCalcHashesCommand { get; set; }

		public CancellationTokenSource CancellationTokenSource { get; set; }

		private bool _isLoading;
		public bool IsLoading
		{
			get => _isLoading;
			set => SetProperty(ref _isLoading, value);
		}
		#endregion

		public async Task ExecuteLoadAndCalcHashesCommandAsync(CancellationToken cancellationToken)
		{
			try
			{
				IsLoading = true;

				_stream = File.OpenRead(Item.ItemPath);

				CanAccessFile = true;
				await GetHashesAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				CanAccessFile = false;
			}
			catch (Exception)
			{
				CanAccessFile = false;
			}
			finally
			{
				IsLoading = false;
			}
		}

		private async Task GetHashesAsync(CancellationToken cancellationToken)
		{
			Hashes.Add(new()
			{
				Algorithm = "MD5",
				HashValue = await ChecksumHelpers.CreateMD5(_stream, cancellationToken),
			});
			Hashes.Add(new()
			{
				Algorithm = "SHA1",
				HashValue = await ChecksumHelpers.CreateSHA1(_stream, cancellationToken),
			});
			Hashes.Add(new()
			{
				Algorithm = "SHA256",
				HashValue = await ChecksumHelpers.CreateSHA256(_stream, cancellationToken),
			});
			Hashes.Add(new()
			{
				Algorithm = "SHA384",
				HashValue = await ChecksumHelpers.CreateSHA384(_stream, cancellationToken),
			});
			Hashes.Add(new()
			{
				Algorithm = "SHA512",
				HashValue = await ChecksumHelpers.CreateSHA512(_stream, cancellationToken),
			});
		}

		public void Dispose()
		{
			CancellationTokenSource.Cancel();
		}
	}
}
