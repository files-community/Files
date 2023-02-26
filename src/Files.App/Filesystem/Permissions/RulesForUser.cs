using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Files.App.Filesystem.Permissions
{
	public class RulesForUser : ObservableObject
	{
		public void UpdateAccessRules()
		{
			foreach (var rule in accessRules.Where(x => x.IdentityReference == UserGroup.Sid && !x.IsInherited).ToList())
			{
				accessRules.Remove(rule);
			}

			// Do not set if permission is already granted by inheritance
			if (AllowRights != 0 && !InheritedAllowRights.HasFlag(AllowRights))
			{
				accessRules.Add(new FileSystemAccessRuleForUI(isFolder)
				{
					AccessControlType = AccessControlType.Allow,
					FileSystemRights = AllowRights,
					IdentityReference = UserGroup.Sid,
					InheritanceFlags = isFolder ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit : InheritanceFlags.None,
					PropagationFlags = PropagationFlags.None
				});
			}

			// Do not set if permission is already denied by inheritance
			if (DenyRights != 0 && !InheritedDenyRights.HasFlag(DenyRights))
			{
				accessRules.Add(new FileSystemAccessRuleForUI(isFolder)
				{
					AccessControlType = AccessControlType.Deny,
					FileSystemRights = DenyRights,
					IdentityReference = UserGroup.Sid,
					InheritanceFlags = isFolder ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit : InheritanceFlags.None,
					PropagationFlags = PropagationFlags.None
				});
			}
		}

		#region Fields and Properties
		private bool isFolder;

		private ObservableCollection<FileSystemAccessRuleForUI> accessRules;

		public RulesForUser(ObservableCollection<FileSystemAccessRuleForUI> accessRules, bool isFolder)
		{
			this.accessRules = accessRules;
			this.isFolder = isFolder;
		}

		public FileSystemRights InheritedDenyRights { get; set; }

		public FileSystemRights InheritedAllowRights { get; set; }

		public FileSystemRights denyRights;
		public FileSystemRights DenyRights
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

		public FileSystemRights allowRights;
		public FileSystemRights AllowRights
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

		public UserGroup UserGroup { get; set; }

		public bool GrantsInheritedWrite => InheritedAllowRights.HasFlag(FileSystemRights.Write);
		public bool GrantsInheritedRead => InheritedAllowRights.HasFlag(FileSystemRights.Read);
		public bool GrantsInheritedListDirectory => InheritedAllowRights.HasFlag(FileSystemRights.ListDirectory);
		public bool GrantsInheritedReadAndExecute => InheritedAllowRights.HasFlag(FileSystemRights.ReadAndExecute);
		public bool GrantsInheritedModify => InheritedAllowRights.HasFlag(FileSystemRights.Modify);
		public bool GrantsInheritedFullControl => InheritedAllowRights.HasFlag(FileSystemRights.FullControl);

		public bool DeniesInheritedWrite => InheritedDenyRights.HasFlag(FileSystemRights.Write);
		public bool DeniesInheritedRead => InheritedDenyRights.HasFlag(FileSystemRights.Read);
		public bool DeniesInheritedListDirectory => InheritedDenyRights.HasFlag(FileSystemRights.ListDirectory);
		public bool DeniesInheritedReadAndExecute => InheritedDenyRights.HasFlag(FileSystemRights.ReadAndExecute);
		public bool DeniesInheritedModify => InheritedDenyRights.HasFlag(FileSystemRights.Modify);
		public bool DeniesInheritedFullControl => InheritedDenyRights.HasFlag(FileSystemRights.FullControl);

		public bool GrantsWrite
		{
			get => AllowRights.HasFlag(FileSystemRights.Write) || GrantsInheritedWrite;
			set => ToggleAllowPermission(FileSystemRights.Write, value);
		}

		public bool GrantsRead
		{
			get => AllowRights.HasFlag(FileSystemRights.Read) || GrantsInheritedRead;
			set => ToggleAllowPermission(FileSystemRights.Read, value);
		}

		public bool GrantsListDirectory
		{
			get => AllowRights.HasFlag(FileSystemRights.ListDirectory) || GrantsInheritedListDirectory;
			set => ToggleAllowPermission(FileSystemRights.ListDirectory, value);
		}

		public bool GrantsReadAndExecute
		{
			get => AllowRights.HasFlag(FileSystemRights.ReadAndExecute) || GrantsInheritedReadAndExecute;
			set => ToggleAllowPermission(FileSystemRights.ReadAndExecute, value);
		}

		public bool GrantsModify
		{
			get => AllowRights.HasFlag(FileSystemRights.Modify) || GrantsInheritedModify;
			set => ToggleAllowPermission(FileSystemRights.Modify, value);
		}

		public bool GrantsFullControl
		{
			get => AllowRights.HasFlag(FileSystemRights.FullControl) || GrantsInheritedFullControl;
			set => ToggleAllowPermission(FileSystemRights.FullControl, value);
		}

		public bool DeniesWrite
		{
			get => DenyRights.HasFlag(FileSystemRights.Write) || DeniesInheritedWrite;
			set => ToggleDenyPermission(FileSystemRights.Write, value);
		}

		public bool DeniesRead
		{
			get => DenyRights.HasFlag(FileSystemRights.Read) || DeniesInheritedRead;
			set => ToggleDenyPermission(FileSystemRights.Read, value);
		}

		public bool DeniesListDirectory
		{
			get => DenyRights.HasFlag(FileSystemRights.ListDirectory) || DeniesInheritedListDirectory;
			set => ToggleDenyPermission(FileSystemRights.ListDirectory, value);
		}

		public bool DeniesReadAndExecute
		{
			get => DenyRights.HasFlag(FileSystemRights.ReadAndExecute) || DeniesInheritedReadAndExecute;
			set => ToggleDenyPermission(FileSystemRights.ReadAndExecute, value);
		}

		public bool DeniesModify
		{
			get => DenyRights.HasFlag(FileSystemRights.Modify) || DeniesInheritedModify;
			set => ToggleDenyPermission(FileSystemRights.Modify, value);
		}

		public bool DeniesFullControl
		{
			get => DenyRights.HasFlag(FileSystemRights.FullControl) || DeniesInheritedFullControl;
			set => ToggleDenyPermission(FileSystemRights.FullControl, value);
		}
		#endregion

		#region Methods
		private void ToggleAllowPermission(FileSystemRights permission, bool value)
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

		private void ToggleDenyPermission(FileSystemRights permission, bool value)
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

		public static List<RulesForUser> ForAllUsers(ObservableCollection<FileSystemAccessRuleForUI> accessRules, bool isFolder)
		{
			return accessRules.Select(x => x.IdentityReference).Distinct().Select(x => RulesForUser.ForUser(accessRules, isFolder, x)).ToList();
		}

		public static RulesForUser ForUser(ObservableCollection<FileSystemAccessRuleForUI> accessRules, bool isFolder, string identity)
		{
			var perm = new RulesForUser(accessRules, isFolder)
			{
				UserGroup = UserGroup.FromSid(identity)
			};

			foreach (var Rule in accessRules.Where(x => x.IdentityReference == identity))
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
