using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents an allow permission for UI
	/// </summary>
	public class GrantedPermission : ObservableObject
	{
		public GrantedPermission(AccessControlEntryAdvanced fileSystemAccessRule)
		{
			fsar = fileSystemAccessRule;
			fsar.PropertyChanged += Fsar_PropertyChanged;
		}

		#region Properties
		protected AccessControlEntryAdvanced fsar;

		public virtual bool IsGranted
		{
			get => fsar.FileSystemRights.HasFlag(Permission);
			set
			{
				if (IsEditable)
					TogglePermission(Permission, value);
			}
		}

		public string Name { get; set; }

		public bool IsEditable { get; set; }

		public AccessMask Permission { get; set; }
		#endregion

		#region Methods
		private void TogglePermission(AccessMask permission, bool value)
		{
			if (value && !fsar.FileSystemRights.HasFlag(permission))
			{
				fsar.FileSystemRights |= permission;
			}
			else if (!value && fsar.FileSystemRights.HasFlag(permission))
			{
				fsar.FileSystemRights &= ~permission;
			}
		}

		protected virtual void Fsar_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "FileSystemRights")
				OnPropertyChanged(nameof(IsGranted));
		}
		#endregion
	}
}
