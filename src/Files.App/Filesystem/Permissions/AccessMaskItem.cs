using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents an access mask, such as its name and changeability.
	/// </summary>
	public class AccessMaskItem : ObservableObject
	{
		public AccessMaskItem(AccessControlEntryAdvanced aceAdvanced, bool isEditable = true)
		{
			_aceAdvanced = aceAdvanced;
			_aceAdvanced.PropertyChanged += AccessControlEntryAdvanced_PropertyChanged;
		}

		#region Properties
		public string AccessMaskName { get; set; }

		public AccessMaskFlags AccessMask { get; set; }

		public bool IsEnabled
		{
			get => _aceAdvanced.FileSystemRights.HasFlag(AccessMask);
			set
			{
				if (IsEditable)
					ToggleAccess(AccessMask, value);
			}
		}

		public bool IsEditable { get; set; }

		private AccessControlEntryAdvanced _aceAdvanced;
		#endregion

		#region Methods
		private void ToggleAccess(AccessMaskFlags permission, bool value)
		{
			if (value && !_aceAdvanced.FileSystemRights.HasFlag(permission))
			{
				_aceAdvanced.FileSystemRights |= permission;
			}
			else if (!value && _aceAdvanced.FileSystemRights.HasFlag(permission))
			{
				_aceAdvanced.FileSystemRights &= ~permission;
			}
		}

		private void AccessControlEntryAdvanced_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(AccessControlEntryAdvanced.FileSystemRights))
				OnPropertyChanged(nameof(IsEnabled));
		}
		#endregion
	}
}
