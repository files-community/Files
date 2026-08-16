// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Services.PreviewPopupProviders
{
	/// <inheritdoc cref="IPreviewPopupService"/>
	internal sealed partial class PreviewPopupService : ObservableObject, IPreviewPopupService
	{
		public async Task<IPreviewPopupProvider?> GetProviderAsync()
		{			
			if (await QuickLookProvider.Instance.DetectAvailability())
				return await Task.FromResult<IPreviewPopupProvider>(QuickLookProvider.Instance);
			if (await SeerProProvider.Instance.DetectAvailability())
				return await Task.FromResult<IPreviewPopupProvider>(SeerProProvider.Instance);
			if (await PowerToysPeekProvider.Instance.DetectAvailability())
				return await Task.FromResult<IPreviewPopupProvider>(PowerToysPeekProvider.Instance);
			else
				return null;
		}
	}
}
