// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
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
		/// Whether holding the hotkey shows the preview until the key is released
		/// </summary>
		bool SupportsHoldToPreview
			=> false;

		/// <summary>
		/// Closes the preview popup if it is open
		/// </summary>
		Task ClosePreviewAsync()
			=> Task.CompletedTask;

		/// <summary>
		/// Detect if the provider is available
		/// </summary>
		/// <returns></returns>
		Task<bool> DetectAvailability();
	}
}
