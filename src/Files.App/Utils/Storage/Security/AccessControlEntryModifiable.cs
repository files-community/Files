// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Utils.Storage.Security
{
	public class AccessControlEntryModifiable : ObservableObject
	{
		public Principal Principal { get; private set; }

		public IList<AccessControlEntryType> PossibleAccessControlTypes { get; private set; }

		private AccessControlEntryType _SelectedAccessControlType;
		public AccessControlEntryType SelectedAccessControlType
		{
			get => _SelectedAccessControlType;
			set => SetProperty(ref _SelectedAccessControlType, value);
		}

		public AccessControlEntryModifiable(AccessControlEntry item)
		{
			Principal = item.Principal;
			SelectedAccessControlType = item.AccessControlType;
			PossibleAccessControlTypes = new List<AccessControlEntryType>()
			{
				AccessControlEntryType.Allow,
				AccessControlEntryType.Deny,
			};
		}
	}
}
