using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents an ACE.
	/// </summary>
	public class AccessControlEntry : ObservableObject
	{
		public AccessControlEntry(ObservableCollection<AccessControlEntryAdvanced> accessRules, bool isFolder)
		{
			this.accessRules = accessRules;
			this.isFolder = isFolder;
		}

		#region Fields and Properties
		private bool isFolder;

		private ObservableCollection<AccessControlEntryAdvanced> accessRules;

		public AccessMaskFlags InheritedDenyRights { get; set; }

		public AccessMaskFlags InheritedAllowRights { get; set; }

		public AccessMaskFlags denyRights;
		public AccessMaskFlags DenyRights
		{
			get => denyRights;
			set
			{
				if (SetProperty(ref denyRights, value))
				{
					OnPropertyChanged(nameof(DeniesWrite));
					OnPropertyChanged(nameof(DeniesFullControl));
					OnPropertyChanged(nameof(DeniesListDirectory));
					OnPropertyChanged(nameof(DeniesModify));
					OnPropertyChanged(nameof(DeniesRead));
					OnPropertyChanged(nameof(DeniesReadAndExecute));
				}
			}
		}

		public AccessMaskFlags allowRights;
		public AccessMaskFlags AllowRights
		{
			get => allowRights;
			set
			{
				if (SetProperty(ref allowRights, value))
				{
					OnPropertyChanged(nameof(GrantsWrite));
					OnPropertyChanged(nameof(GrantsFullControl));
					OnPropertyChanged(nameof(GrantsListDirectory));
					OnPropertyChanged(nameof(GrantsModify));
					OnPropertyChanged(nameof(GrantsRead));
					OnPropertyChanged(nameof(GrantsReadAndExecute));
				}
			}
		}

		public Principal UserGroup { get; set; }

		public bool GrantsInheritedWrite => InheritedAllowRights.HasFlag(AccessMaskFlags.Write);
		public bool GrantsInheritedRead => InheritedAllowRights.HasFlag(AccessMaskFlags.Read);
		public bool GrantsInheritedListDirectory => InheritedAllowRights.HasFlag(AccessMaskFlags.ListDirectory);
		public bool GrantsInheritedReadAndExecute => InheritedAllowRights.HasFlag(AccessMaskFlags.ReadAndExecute);
		public bool GrantsInheritedModify => InheritedAllowRights.HasFlag(AccessMaskFlags.Modify);
		public bool GrantsInheritedFullControl => InheritedAllowRights.HasFlag(AccessMaskFlags.FullControl);

		public bool DeniesInheritedWrite => InheritedDenyRights.HasFlag(AccessMaskFlags.Write);
		public bool DeniesInheritedRead => InheritedDenyRights.HasFlag(AccessMaskFlags.Read);
		public bool DeniesInheritedListDirectory => InheritedDenyRights.HasFlag(AccessMaskFlags.ListDirectory);
		public bool DeniesInheritedReadAndExecute => InheritedDenyRights.HasFlag(AccessMaskFlags.ReadAndExecute);
		public bool DeniesInheritedModify => InheritedDenyRights.HasFlag(AccessMaskFlags.Modify);
		public bool DeniesInheritedFullControl => InheritedDenyRights.HasFlag(AccessMaskFlags.FullControl);

		public bool GrantsWrite
		{
			get => AllowRights.HasFlag(AccessMaskFlags.Write) || GrantsInheritedWrite;
			set => ToggleAllowPermission(AccessMaskFlags.Write, value);
		}

		public bool GrantsRead
		{
			get => AllowRights.HasFlag(AccessMaskFlags.Read) || GrantsInheritedRead;
			set => ToggleAllowPermission(AccessMaskFlags.Read, value);
		}

		public bool GrantsListDirectory
		{
			get => AllowRights.HasFlag(AccessMaskFlags.ListDirectory) || GrantsInheritedListDirectory;
			set => ToggleAllowPermission(AccessMaskFlags.ListDirectory, value);
		}

		public bool GrantsReadAndExecute
		{
			get => AllowRights.HasFlag(AccessMaskFlags.ReadAndExecute) || GrantsInheritedReadAndExecute;
			set => ToggleAllowPermission(AccessMaskFlags.ReadAndExecute, value);
		}

		public bool GrantsModify
		{
			get => AllowRights.HasFlag(AccessMaskFlags.Modify) || GrantsInheritedModify;
			set => ToggleAllowPermission(AccessMaskFlags.Modify, value);
		}

		public bool GrantsFullControl
		{
			get => AllowRights.HasFlag(AccessMaskFlags.FullControl) || GrantsInheritedFullControl;
			set => ToggleAllowPermission(AccessMaskFlags.FullControl, value);
		}

		public bool DeniesWrite
		{
			get => DenyRights.HasFlag(AccessMaskFlags.Write) || DeniesInheritedWrite;
			set => ToggleDenyPermission(AccessMaskFlags.Write, value);
		}

		public bool DeniesRead
		{
			get => DenyRights.HasFlag(AccessMaskFlags.Read) || DeniesInheritedRead;
			set => ToggleDenyPermission(AccessMaskFlags.Read, value);
		}

		public bool DeniesListDirectory
		{
			get => DenyRights.HasFlag(AccessMaskFlags.ListDirectory) || DeniesInheritedListDirectory;
			set => ToggleDenyPermission(AccessMaskFlags.ListDirectory, value);
		}

		public bool DeniesReadAndExecute
		{
			get => DenyRights.HasFlag(AccessMaskFlags.ReadAndExecute) || DeniesInheritedReadAndExecute;
			set => ToggleDenyPermission(AccessMaskFlags.ReadAndExecute, value);
		}

		public bool DeniesModify
		{
			get => DenyRights.HasFlag(AccessMaskFlags.Modify) || DeniesInheritedModify;
			set => ToggleDenyPermission(AccessMaskFlags.Modify, value);
		}

		public bool DeniesFullControl
		{
			get => DenyRights.HasFlag(AccessMaskFlags.FullControl) || DeniesInheritedFullControl;
			set => ToggleDenyPermission(AccessMaskFlags.FullControl, value);
		}
		#endregion

		#region Methods
		public void UpdateAccessRules()
		{
			foreach (var rule in accessRules.Where(x => x.PrincipalSid == UserGroup.Sid && !x.IsInherited).ToList())
			{
				accessRules.Remove(rule);
			}

			// Do not set if permission is already granted by inheritance
			if (AllowRights != 0 && !InheritedAllowRights.HasFlag(AllowRights))
			{
				accessRules.Add(new AccessControlEntryAdvanced(isFolder)
				{
					AccessControlType = AccessControlType.Allow,
					FileSystemRights = AllowRights,
					PrincipalSid = UserGroup.Sid,
					InheritanceFlags = isFolder ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit : InheritanceFlags.None,
					PropagationFlags = PropagationFlags.None
				});
			}

			// Do not set if permission is already denied by inheritance
			if (DenyRights != 0 && !InheritedDenyRights.HasFlag(DenyRights))
			{
				accessRules.Add(new AccessControlEntryAdvanced(isFolder)
				{
					AccessControlType = AccessControlType.Deny,
					FileSystemRights = DenyRights,
					PrincipalSid = UserGroup.Sid,
					InheritanceFlags = isFolder ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit : InheritanceFlags.None,
					PropagationFlags = PropagationFlags.None
				});
			}
		}

		private void ToggleAllowPermission(AccessMaskFlags permission, bool value)
		{
			if (value && !AllowRights.HasFlag(permission) && !InheritedAllowRights.HasFlag(permission))
			{
				AllowRights |= permission;
				DenyRights &= ~permission;
			}
			else if (!value && AllowRights.HasFlag(permission))
			{
				AllowRights &= ~permission;
			}

			UpdateAccessRules();
		}

		private void ToggleDenyPermission(AccessMaskFlags permission, bool value)
		{
			if (value && !DenyRights.HasFlag(permission) && !InheritedDenyRights.HasFlag(permission))
			{
				DenyRights |= permission;
				AllowRights &= ~permission;
			}
			else if (!value && DenyRights.HasFlag(permission))
			{
				DenyRights &= ~permission;
			}

			UpdateAccessRules();
		}

		public static List<AccessControlEntry> ForAllUsers(ObservableCollection<AccessControlEntryAdvanced> accessRules, bool isFolder)
		{
			return
				accessRules.Select(x => x.PrincipalSid)
				.Distinct().Select(x => ForUser(accessRules, isFolder, x))
				.ToList();
		}

		public static AccessControlEntry ForUser(ObservableCollection<AccessControlEntryAdvanced> accessRules, bool isFolder, string identity)
		{
			var perm = new AccessControlEntry(accessRules, isFolder)
			{
				UserGroup = Principal.FromSid(identity)
			};

			foreach (var Rule in accessRules.Where(x => x.PrincipalSid == identity))
			{
				if (Rule.AccessControlType == AccessControlType.Deny)
				{
					if (Rule.IsInherited)
					{
						perm.InheritedDenyRights |= Rule.FileSystemRights;
					}
					else
					{
						perm.DenyRights |= Rule.FileSystemRights;
					}
				}
				else if (Rule.AccessControlType == AccessControlType.Allow)
				{
					if (Rule.IsInherited)
					{
						perm.InheritedAllowRights |= Rule.FileSystemRights;
					}
					else
					{
						perm.AllowRights |= Rule.FileSystemRights;
					}
				}
			}

			return perm;
		}
		#endregion
	}
}
