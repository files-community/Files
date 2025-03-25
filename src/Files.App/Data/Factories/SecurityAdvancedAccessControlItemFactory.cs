// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Files.App.Data.Factories
{
	public static class SecurityAdvancedAccessControlItemFactory
	{
		/// <summary>
		/// Returned list list will be shown in an ACE item in security advanced page
		/// </summary>
		/// <param name="current"></param>
		/// <param name="isAdvanced"></param>
		/// <param name="isInherited"></param>
		/// <param name="isFolder"></param>
		/// <returns></returns>
		public static ObservableCollection<AccessMaskItem> Initialize(AccessControlEntry current, bool isAdvanced, bool isInherited, bool isFolder)
		{
			List<AccessMaskItem> accessControls;

			if (isAdvanced)
			{
				accessControls =
				[
					new(current)
					{
						AccessMask = AccessMaskFlags.FullControl,
						AccessMaskName = Strings.SecurityFullControlLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.Traverse,
						AccessMaskName = Strings.SecurityTraverseLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ExecuteFile,
						AccessMaskName = Strings.SecurityExecuteFileLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ListDirectory,
						AccessMaskName = Strings.SecurityListDirectoryLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ReadData,
						AccessMaskName = Strings.SecurityReadDataLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ReadAttributes,
						AccessMaskName = Strings.SecurityReadAttributesLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ReadExtendedAttributes,
						AccessMaskName = Strings.SecurityReadExtendedAttributesLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.CreateFiles,
						AccessMaskName = Strings.SecurityCreateFilesLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.CreateDirectories,
						AccessMaskName = Strings.SecurityCreateDirectoriesLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.WriteData,
						AccessMaskName = Strings.SecurityWriteDataLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.AppendData,
						AccessMaskName = Strings.SecurityAppendDataLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.WriteAttributes,
						AccessMaskName = Strings.SecurityWriteAttributesLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.WriteExtendedAttributes,
						AccessMaskName = Strings.SecurityWriteExtendedAttributesLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.DeleteSubdirectoriesAndFiles,
						AccessMaskName = Strings.SecurityDeleteSubdirectoriesAndFilesLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.Delete,
						AccessMaskName = Strings.Delete.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ReadPermissions,
						AccessMaskName = Strings.SecurityReadPermissionsLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ChangePermissions,
						AccessMaskName = Strings.SecurityChangePermissionsLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.TakeOwnership,
						AccessMaskName = Strings.SecurityTakeOwnershipLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					}
				];

				if (isFolder)
				{
					accessControls.RemoveAll(x =>
						x.AccessMask == AccessMaskFlags.ExecuteFile ||
						x.AccessMask == AccessMaskFlags.ReadData ||
						x.AccessMask == AccessMaskFlags.WriteData ||
						x.AccessMask == AccessMaskFlags.AppendData);
				}
				else
				{
					accessControls.RemoveAll(x =>
						x.AccessMask == AccessMaskFlags.Traverse ||
						x.AccessMask == AccessMaskFlags.ListDirectory ||
						x.AccessMask == AccessMaskFlags.CreateFiles ||
						x.AccessMask == AccessMaskFlags.CreateDirectories ||
						x.AccessMask == AccessMaskFlags.DeleteSubdirectoriesAndFiles);
				}
			}
			else
			{
				accessControls =
				[
					new(current)
					{
						AccessMask = AccessMaskFlags.FullControl,
						AccessMaskName = Strings.SecurityFullControlLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.Modify,
						AccessMaskName = Strings.Modify.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ReadAndExecute,
						AccessMaskName = Strings.SecurityReadAndExecuteLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ListDirectory,
						AccessMaskName = Strings.SecurityListDirectoryLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.Read,
						AccessMaskName = Strings.SecurityReadLabel_Text.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.Write,
						AccessMaskName = Strings.Write.GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current, false)
					{
						AccessMaskName = Strings.SecuritySpecialLabel_Text.GetLocalizedResource()
					}
				];

				if (!isFolder)
				{
					accessControls.RemoveAll(x =>
						x.AccessMask == AccessMaskFlags.ListDirectory);
				}
			}

			return new ObservableCollection<AccessMaskItem>(accessControls);
		}
	}
}
