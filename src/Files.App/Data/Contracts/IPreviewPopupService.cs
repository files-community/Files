// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

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
