using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Files.App.Filesystem.Permissions
{
	/// <summary>
	/// Represents an ACE information
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

		public AccessMask InheritedDenyRights { get; set; }

		public AccessMask InheritedAllowRights { get; set; }

		public AccessMask denyRights;
		public AccessMask DenyRights
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

		public AccessMask allowRights;
		public AccessMask AllowRights
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

		public bool GrantsInheritedWrite => InheritedAllowRights.HasFlag(AccessMask.Write);
		public bool GrantsInheritedRead => InheritedAllowRights.HasFlag(AccessMask.Read);
		public bool GrantsInheritedListDirectory => InheritedAllowRights.HasFlag(AccessMask.ListDirectory);
		public bool GrantsInheritedReadAndExecute => InheritedAllowRights.HasFlag(AccessMask.ReadAndExecute);
		public bool GrantsInheritedModify => InheritedAllowRights.HasFlag(AccessMask.Modify);
		public bool GrantsInheritedFullControl => InheritedAllowRights.HasFlag(AccessMask.FullControl);

		public bool DeniesInheritedWrite => InheritedDenyRights.HasFlag(AccessMask.Write);
		public bool DeniesInheritedRead => InheritedDenyRights.HasFlag(AccessMask.Read);
		public bool DeniesInheritedListDirectory => InheritedDenyRights.HasFlag(AccessMask.ListDirectory);
		public bool DeniesInheritedReadAndExecute => InheritedDenyRights.HasFlag(AccessMask.ReadAndExecute);
		public bool DeniesInheritedModify => InheritedDenyRights.HasFlag(AccessMask.Modify);
		public bool DeniesInheritedFullControl => InheritedDenyRights.HasFlag(AccessMask.FullControl);

		public bool GrantsWrite
		{
			get => AllowRights.HasFlag(AccessMask.Write) || GrantsInheritedWrite;
			set => ToggleAllowPermission(AccessMask.Write, value);
		}

		public bool GrantsRead
		{
			get => AllowRights.HasFlag(AccessMask.Read) || GrantsInheritedRead;
			set => ToggleAllowPermission(AccessMask.Read, value);
		}

		public bool GrantsListDirectory
		{
			get => AllowRights.HasFlag(AccessMask.ListDirectory) || GrantsInheritedListDirectory;
			set => ToggleAllowPermission(AccessMask.ListDirectory, value);
		}

		public bool GrantsReadAndExecute
		{
			get => AllowRights.HasFlag(AccessMask.ReadAndExecute) || GrantsInheritedReadAndExecute;
			set => ToggleAllowPermission(AccessMask.ReadAndExecute, value);
		}

		public bool GrantsModify
		{
			get => AllowRights.HasFlag(AccessMask.Modify) || GrantsInheritedModify;
			set => ToggleAllowPermission(AccessMask.Modify, value);
		}

		public bool GrantsFullControl
		{
			get => AllowRights.HasFlag(AccessMask.FullControl) || GrantsInheritedFullControl;
			set => ToggleAllowPermission(AccessMask.FullControl, value);
		}

		public bool DeniesWrite
		{
			get => DenyRights.HasFlag(AccessMask.Write) || DeniesInheritedWrite;
			set => ToggleDenyPermission(AccessMask.Write, value);
		}

		public bool DeniesRead
		{
			get => DenyRights.HasFlag(AccessMask.Read) || DeniesInheritedRead;
			set => ToggleDenyPermission(AccessMask.Read, value);
		}

		public bool DeniesListDirectory
		{
			get => DenyRights.HasFlag(AccessMask.ListDirectory) || DeniesInheritedListDirectory;
			set => ToggleDenyPermission(AccessMask.ListDirectory, value);
		}

		public bool DeniesReadAndExecute
		{
			get => DenyRights.HasFlag(AccessMask.ReadAndExecute) || DeniesInheritedReadAndExecute;
			set => ToggleDenyPermission(AccessMask.ReadAndExecute, value);
		}

		public bool DeniesModify
		{
			get => DenyRights.HasFlag(AccessMask.Modify) || DeniesInheritedModify;
			set => ToggleDenyPermission(AccessMask.Modify, value);
		}

		public bool DeniesFullControl
		{
			get => DenyRights.HasFlag(AccessMask.FullControl) || DeniesInheritedFullControl;
			set => ToggleDenyPermission(AccessMask.FullControl, value);
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

		private void ToggleAllowPermission(AccessMask permission, bool value)
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

		private void ToggleDenyPermission(AccessMask permission, bool value)
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
