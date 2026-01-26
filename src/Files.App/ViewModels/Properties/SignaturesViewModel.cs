// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Signatures;
using Microsoft.UI.Windowing;
using Windows.Win32.Foundation;

namespace Files.App.ViewModels.Properties
{
	public sealed partial class SignaturesViewModel : ObservableObject, IDisposable
	{
		private CancellationTokenSource _cancellationTokenSource;

		public ObservableCollection<SignatureInfoItem> Signatures { get; set; }

		public bool NoSignatureFound => Signatures.Count == 0;

		public SignaturesViewModel(ListedItem item, AppWindow appWindow)
		{
			_cancellationTokenSource = new();
			Signatures = new();
			var hWnd = new HWND(Microsoft.UI.Win32Interop.GetWindowFromWindowId(appWindow.Id));
			Signatures.CollectionChanged += (s, e) => OnPropertyChanged(nameof(NoSignatureFound));
			DigitalSignaturesUtil.LoadItemSignatures(
				item.ItemPath,
				Signatures,
				hWnd,
				_cancellationTokenSource.Token
			);
		}

		public void Dispose()
		{
			_cancellationTokenSource.Cancel();
		}
	}
}
