// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Backend.Services
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
