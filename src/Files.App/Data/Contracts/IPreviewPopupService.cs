// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
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
