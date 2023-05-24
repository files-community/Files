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
		/// Opens preview popup
		/// </summary>
		/// <param name="path"></param>
		/// <param name="switchPreview"></param>
		/// <returns></returns>
		Task OpenPreviewPopup(string path, bool switchPreview = false);

		/// <summary>
		/// Detect if the provider is available
		/// </summary>
		/// <returns></returns>
		Task<bool> DetectAvailability();
	}
}
