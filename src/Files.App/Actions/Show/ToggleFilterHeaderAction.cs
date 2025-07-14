﻿// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Actions
{
	internal sealed partial class ToggleFilterHeaderAction : ObservableObject, IToggleAction
	{
		private readonly IGeneralSettingsService generalSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

		public string Label
			=> Strings.ToggleFilterHeader.GetLocalizedResource();

		public string Description
			=> Strings.ToggleFilterHeaderDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Filter");

		public bool IsOn
			=> generalSettingsService.ShowFilterHeader;

		public ToggleFilterHeaderAction()
		{
			generalSettingsService.PropertyChanged += GeneralSettingsService_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			generalSettingsService.ShowFilterHeader = !IsOn;

			if (IsOn)
			{
				var filterTextBox = (MainWindow.Instance.Content as Frame)?.FindDescendant("FilterTextBox") as AutoSuggestBox;
				filterTextBox?.Focus(FocusState.Programmatic);
			}

			return Task.CompletedTask;
		}

		private void GeneralSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(GeneralSettingsService.ShowFilterHeader))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
