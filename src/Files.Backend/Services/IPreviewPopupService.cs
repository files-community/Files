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
		/// Toggle preview popup
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		Task TogglePreviewPopup(string path);
		
		/// <summary>
		/// Switch preview
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		Task SwitchPreview(string path);

		/// <summary>
		/// Detect if the provider is available
		/// </summary>
		/// <returns></returns>
		Task<bool> DetectAvailability();
	}
}
