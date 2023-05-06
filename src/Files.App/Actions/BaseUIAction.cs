// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal abstract class BaseUIAction : ObservableObject
	{
		public virtual bool IsExecutable => UIHelpers.CanShowDialog;

		public BaseUIAction()
		{
			UIHelpers.PropertyChanged += UIHelpers_PropertyChanged;
		}

		private void UIHelpers_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(UIHelpers.CanShowDialog))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
