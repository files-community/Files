// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.ViewModels.FileTags;
using System.Windows.Input;

namespace Files.App.Data.Items
{
	public class NewTagItem : ObservableObject
	{
		private string name = string.Empty;
		public string Name
		{
			get => name;
			set
			{
				SetProperty(ref name, value);
				{
					OnPropertyChanged(nameof(CanCommit));
					OnPropertyChanged(nameof(IsNameValid));
				}
			}
		}

		private string color = "#FFFFFFFF";
		public string Color
		{
			get => color;
			set => SetProperty(ref color, value);
		}

		private bool isNameValid = true;
		public bool IsNameValid
		{
			get => isNameValid;
			set
			{
				if (SetProperty(ref isNameValid, value))
					OnPropertyChanged(nameof(CanCommit));
			}
		}

		public bool CanCommit
			=> !string.IsNullOrEmpty(name) && IsNameValid;

		public void Reset()
		{
			Name = string.Empty;
			IsNameValid = true;
			Color = ColorHelpers.RandomColor();
		}
	}
}
