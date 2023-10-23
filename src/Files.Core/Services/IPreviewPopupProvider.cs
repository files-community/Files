// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Services
{
	public interface IPreviewPopupProvider
	{
		/// <summary>
		/// Toggle preview popup
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		Task TogglePreviewPopupAsync(string path);

		/// <summary>
		/// Switch preview
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		Task SwitchPreviewAsync(string path);

		/// <summary>
		/// Detect if the provider is available
		/// </summary>
		/// <returns></returns>
		Task<bool> DetectAvailability();
	}
}
