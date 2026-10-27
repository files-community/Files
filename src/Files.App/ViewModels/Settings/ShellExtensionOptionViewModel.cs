// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Files.App.ViewModels.Settings
{
	/// <summary>
	/// Represents one selectable shell extension name in the pinned shell extensions settings list.
	/// </summary>
	public sealed class ShellExtensionOptionViewModel : ObservableObject
	{
		private bool isSelected;

		public string Name { get; }

		public bool IsSelected
		{
			get => isSelected;
			set => SetProperty(ref isSelected, value);
		}

		public ShellExtensionOptionViewModel(string name, bool isSelected)
		{
			Name = name;
			this.isSelected = isSelected;
		}
	}
}
