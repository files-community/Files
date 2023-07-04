// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Services
{
	public interface IPreviewPopupService : INotifyPropertyChanged
	{
		/// <summary>
		/// Get Preview Popup provider
		/// </summary>
		/// <returns></returns>
		Task<IPreviewPopupProvider?> GetProviderAsync();
	}
}
