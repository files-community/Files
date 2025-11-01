// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.App.Actions
{
	internal abstract class BaseTransferItemAction : ObservableObject
	{
		protected readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		protected readonly StatusCenterViewModel StatusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		public bool IsExecutable
			=> ContentPageContext.HasSelection;

		public BaseTransferItemAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public async Task ExecuteTransferAsync(DataPackageOperation type = DataPackageOperation.Copy)
		{
			await TransferHelpers.ExecuteTransferAsync(ContentPageContext, StatusCenterViewModel, type);
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
