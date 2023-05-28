// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ServicesImplementation.PreviewPopupProviders;
using Files.Backend.Services;

namespace Files.App.ServicesImplementation
{
	/// <inheritdoc cref="IPreviewPopupService"/>
	internal sealed class PreviewPopupService : ObservableObject, IPreviewPopupService
	{
		public async Task<IPreviewPopupProvider?> GetProviderAsync()
		{
			if (await QuickLookProvider.Instance.DetectAvailability())
				return await Task.FromResult<IPreviewPopupProvider>(QuickLookProvider.Instance);
			else
				return null;
		}
	}
}
