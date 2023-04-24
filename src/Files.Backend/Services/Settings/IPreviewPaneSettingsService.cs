// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.ComponentModel;

namespace Files.Backend.Services.Settings
{
	public interface IPreviewPaneSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets or sets a value indicating if the preview pane is enabled.
		/// </summary>
		bool IsEnabled { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the height of the pane in a horizontal layout.
		/// </summary>
		double HorizontalSizePx { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the width of the pane in a vertical layout.
		/// </summary>
		double VerticalSizePx { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the preview pane media volume.
		/// </summary>
		double MediaVolume { get; set; }

		/// <summary>
		/// Gets or sets a value indicating if the preview pane should only show the item preview without the details section.
		/// </summary>
		bool ShowPreviewOnly { get; set; }
	}
}
