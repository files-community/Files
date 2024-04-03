// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Data.Enums;

namespace Files.App.ViewModels.Dialogs
{
	public interface IDialog<TViewModel>
		where TViewModel : class, INotifyPropertyChanged
	{
		TViewModel ViewModel { get; set; }

		Task<DialogResult> ShowAsync();

		void Hide();
	}
}
