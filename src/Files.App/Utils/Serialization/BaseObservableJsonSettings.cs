// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

namespace Files.App.Utils.Serialization
{
	internal abstract class BaseObservableJsonSettings : BaseJsonSettings, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		protected override bool Set<TValue>(TValue? value, [CallerMemberName] string propertyName = "") where TValue : default
		{
			if (!base.Set<TValue>(value, propertyName))
				return false;

			OnPropertyChanged(propertyName);

			return true;
		}

		protected override void OnSharedSettingChanged(string name) => OnPropertyChanged(name);

		protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
