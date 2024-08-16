// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Represents a service to enumerate and update drives.
	/// </summary>
	public interface IRemovableDrivesService : INotifyPropertyChanged
	{
		/// <summary>
		/// Gets drives.
		/// </summary>
		ObservableCollection<ILocatableFolder> Drives { get; }

		/// <summary>
		/// Gets or sets a value that indicates whether the application should show consent dialog when the primary drive isn't accessible.
		/// </summary>
		bool ShowUserConsentOnInit { get; set; }

		/// <summary>
		/// Updates all connected devices.
		/// </summary>
		/// <returns></returns>
		Task UpdateDrivesAsync();
	}
}
