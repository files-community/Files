using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace Files.App.Filesystem.Security
{
	/// <summary>
	/// Represents an access mask details, such as its name and changeability.
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
			get => _aceAdvanced.AccessMaskFlags.HasFlag(AccessMask);
			set
			{
				if (IsEditable)
					ToggleAccess(AccessMask, value);
			}
		}

		public bool IsEditable { get; set; }

		private readonly AccessControlEntryAdvanced _aceAdvanced;
		#endregion

		#region Methods
		private void ToggleAccess(AccessMaskFlags accessMask, bool value)
		{
			if (value && !_aceAdvanced.AccessMaskFlags.HasFlag(accessMask))
			{
				_aceAdvanced.AccessMaskFlags |= accessMask;
			}
			else if (!value && _aceAdvanced.AccessMaskFlags.HasFlag(accessMask))
			{
				_aceAdvanced.AccessMaskFlags &= ~accessMask;
			}
		}

		private void AccessControlEntryAdvanced_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(AccessControlEntryAdvanced.AccessMaskFlags))
				OnPropertyChanged(nameof(IsEnabled));
		}
		#endregion
	}
}
