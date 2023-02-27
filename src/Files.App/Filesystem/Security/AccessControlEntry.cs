using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Files.App.Filesystem.Security
{
	/// <summary>
	/// Represents an ACE.
	/// </summary>
	public class AccessControlEntry : ObservableObject
	{
		public AccessControlEntry(ObservableCollection<AccessControlEntryAdvanced> aceAdvanced, bool isFolder)
		{
			_aceAdvanced = aceAdvanced;
			_isFolder = isFolder;
		}

		#region Fields and Properties
		private readonly bool _isFolder;

		private readonly ObservableCollection<AccessControlEntryAdvanced> _aceAdvanced;

		public Principal Principal { get; set; }

		public AccessMaskFlags InheritedDenyAccessMaskFlags { get; set; }

		public AccessMaskFlags InheritedAllowAccessMaskFlags { get; set; }

		private AccessMaskFlags _deniedAccessMaskFlags;
		public AccessMaskFlags DeniedAccessMaskFlags
		{
			get => _deniedAccessMaskFlags;
			set
			{
				if (SetProperty(ref _deniedAccessMaskFlags, value))
				{
					OnPropertyChanged(nameof(DeniedWriteAccess));
					OnPropertyChanged(nameof(DeniedFullControlAccess));
					OnPropertyChanged(nameof(DeniedListDirectoryAccess));
					OnPropertyChanged(nameof(DeniedModifyAccess));
					OnPropertyChanged(nameof(DeniedReadAccess));
					OnPropertyChanged(nameof(DeniedReadAndExecuteAccess));
				}
			}
		}

		private AccessMaskFlags _allowedAccessMaskFlags;
		public AccessMaskFlags AllowedAccessMaskFlags
		{
			get => _allowedAccessMaskFlags;
			set
			{
				if (SetProperty(ref _allowedAccessMaskFlags, value))
				{
					OnPropertyChanged(nameof(AllowedWriteAccess));
					OnPropertyChanged(nameof(AllowedFullControlAccess));
					OnPropertyChanged(nameof(AllowedListDirectoryAccess));
					OnPropertyChanged(nameof(AllowedModifyAccess));
					OnPropertyChanged(nameof(AllowedReadAccess));
					OnPropertyChanged(nameof(AllowedReadAndExecuteAccess));
				}
			}
		}

		public bool AllowedInheritedWriteAccess =>          InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.Write);
		public bool AllowedInheritedReadAccess =>           InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.Read);
		public bool AllowedInheritedListDirectoryAccess =>  InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory);
		public bool AllowedInheritedReadAndExecuteAccess => InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute);
		public bool AllowedInheritedModifyAccess =>         InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.Modify);
		public bool AllowedInheritedFullControlAccess =>    InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.FullControl);

		public bool DeniedInheritedWriteAccess =>           InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.Write);
		public bool DeniedInheritedReadAccess =>            InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.Read);
		public bool DeniedInheritedListDirectoryAccess =>   InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory);
		public bool DeniedInheritedReadAndExecuteAccess =>  InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute);
		public bool DeniedInheritedModifyAccess =>          InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.Modify);
		public bool DeniedInheritedFullControlAccess =>     InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.FullControl);

		public bool AllowedWriteAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.Write) || AllowedInheritedWriteAccess;
			set => ToggleAllowAccess(AccessMaskFlags.Write, value);
		}

		public bool AllowedReadAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.Read) || AllowedInheritedReadAccess;
			set => ToggleAllowAccess(AccessMaskFlags.Read, value);
		}

		public bool AllowedListDirectoryAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory) || AllowedInheritedListDirectoryAccess;
			set => ToggleAllowAccess(AccessMaskFlags.ListDirectory, value);
		}

		public bool AllowedReadAndExecuteAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute) || AllowedInheritedReadAndExecuteAccess;
			set => ToggleAllowAccess(AccessMaskFlags.ReadAndExecute, value);
		}

		public bool AllowedModifyAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.Modify) || AllowedInheritedModifyAccess;
			set => ToggleAllowAccess(AccessMaskFlags.Modify, value);
		}

		public bool AllowedFullControlAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.FullControl) || AllowedInheritedFullControlAccess;
			set => ToggleAllowAccess(AccessMaskFlags.FullControl, value);
		}

		public bool DeniedWriteAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.Write) || DeniedInheritedWriteAccess;
			set => ToggleDenyAccess(AccessMaskFlags.Write, value);
		}

		public bool DeniedReadAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.Read) || DeniedInheritedReadAccess;
			set => ToggleDenyAccess(AccessMaskFlags.Read, value);
		}

		public bool DeniedListDirectoryAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory) || DeniedInheritedListDirectoryAccess;
			set => ToggleDenyAccess(AccessMaskFlags.ListDirectory, value);
		}

		public bool DeniedReadAndExecuteAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute) || DeniedInheritedReadAndExecuteAccess;
			set => ToggleDenyAccess(AccessMaskFlags.ReadAndExecute, value);
		}

		public bool DeniedModifyAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.Modify) || DeniedInheritedModifyAccess;
			set => ToggleDenyAccess(AccessMaskFlags.Modify, value);
		}

		public bool DeniedFullControlAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.FullControl) || DeniedInheritedFullControlAccess;
			set => ToggleDenyAccess(AccessMaskFlags.FullControl, value);
		}
		#endregion

		#region Methods
		public void UpdateAccessControlEntry()
		{
			foreach (var item in _aceAdvanced.Where(x => x.PrincipalSid == Principal.Sid && !x.IsInherited).ToList())
			{
				_aceAdvanced.Remove(item);
			}

			// Do not set if permission is already allowed by inheritance
			if (AllowedAccessMaskFlags != 0 && !InheritedAllowAccessMaskFlags.HasFlag(AllowedAccessMaskFlags))
			{
				_aceAdvanced.Add(new AccessControlEntryAdvanced(_isFolder)
				{
					AccessControlType = AccessControlType.Allow,
					AccessMaskFlags = AllowedAccessMaskFlags,
					PrincipalSid = Principal.Sid,
					InheritanceFlags = _isFolder ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit : InheritanceFlags.None,
					PropagationFlags = PropagationFlags.None
				});
			}

			// Do not set if permission is already denied by inheritance
			if (DeniedAccessMaskFlags != 0 && !InheritedDenyAccessMaskFlags.HasFlag(DeniedAccessMaskFlags))
			{
				_aceAdvanced.Add(new AccessControlEntryAdvanced(_isFolder)
				{
					AccessControlType = AccessControlType.Deny,
					AccessMaskFlags = DeniedAccessMaskFlags,
					PrincipalSid = Principal.Sid,
					InheritanceFlags = _isFolder ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit : InheritanceFlags.None,
					PropagationFlags = PropagationFlags.None
				});
			}
		}

		private void ToggleAllowAccess(AccessMaskFlags accessMask, bool value)
		{
			if (value && !AllowedAccessMaskFlags.HasFlag(accessMask) && !InheritedAllowAccessMaskFlags.HasFlag(accessMask))
			{
				AllowedAccessMaskFlags |= accessMask;
				DeniedAccessMaskFlags &= ~accessMask;
			}
			else if (!value && AllowedAccessMaskFlags.HasFlag(accessMask))
			{
				AllowedAccessMaskFlags &= ~accessMask;
			}

			UpdateAccessControlEntry();
		}

		private void ToggleDenyAccess(AccessMaskFlags accessMask, bool value)
		{
			if (value && !DeniedAccessMaskFlags.HasFlag(accessMask) && !InheritedDenyAccessMaskFlags.HasFlag(accessMask))
			{
				DeniedAccessMaskFlags |= accessMask;
				AllowedAccessMaskFlags &= ~accessMask;
			}
			else if (!value && DeniedAccessMaskFlags.HasFlag(accessMask))
			{
				DeniedAccessMaskFlags &= ~accessMask;
			}

			UpdateAccessControlEntry();
		}

		public static List<AccessControlEntry> ForAllUsers(ObservableCollection<AccessControlEntryAdvanced> aceAdvanceds, bool isFolder)
		{
			return
				aceAdvanceds.Select(x => x.PrincipalSid)
				.Distinct().Select(x => ForUser(aceAdvanceds, isFolder, x))
				.ToList();
		}

		public static AccessControlEntry ForUser(ObservableCollection<AccessControlEntryAdvanced> aceAdvanceds, bool isFolder, string sid)
		{
			var ace = new AccessControlEntry(aceAdvanceds, isFolder)
			{
				Principal = Principal.FromSid(sid)
			};

			foreach (var item in aceAdvanceds.Where(x => x.PrincipalSid == sid))
			{
				if (item.AccessControlType == AccessControlType.Deny)
				{
					if (item.IsInherited)
					{
						ace.InheritedDenyAccessMaskFlags |= item.AccessMaskFlags;
					}
					else
					{
						ace.DeniedAccessMaskFlags |= item.AccessMaskFlags;
					}
				}
				else if (item.AccessControlType == AccessControlType.Allow)
				{
					if (item.IsInherited)
					{
						ace.InheritedAllowAccessMaskFlags |= item.AccessMaskFlags;
					}
					else
					{
						ace.AllowedAccessMaskFlags |= item.AccessMaskFlags;
					}
				}
			}

			return ace;
		}
		#endregion
	}
}
