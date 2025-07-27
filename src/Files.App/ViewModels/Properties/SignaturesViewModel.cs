// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Signatures;
using Microsoft.UI.Windowing;

namespace Files.App.ViewModels.Properties
{
	public sealed partial class SignaturesViewModel : ObservableObject, IDisposable
	{
		private CancellationTokenSource _cancellationTokenSource;

		public ObservableCollection<SignatureInfoItem> Signatures { get; set; }

		public SignaturesViewModel(ListedItem item, AppWindow appWindow)
		{
			_cancellationTokenSource = new();
			Signatures = new();
			var hWnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(appWindow.Id);
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
