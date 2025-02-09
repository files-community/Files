// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents a principal of an ACE or an owner of an ACL.
	/// </summary>
	public sealed partial class AccessControlPrincipal : ObservableObject
	{
		/// <summary>
		/// Account type.
		/// </summary>
		public AccessControlPrincipalType PrincipalType { get; private set; }

		/// <summary>
		/// Account security identifier (SID).
		/// </summary>
		public string? Sid { get; private set; }

		/// <summary>
		/// A domain the account belongs to
		/// </summary>
		public string? Domain { get; private set; }

		/// <summary>
		/// Account name
		/// </summary>
		public string? Name { get; private set; }

		/// <summary>
		/// Indicates whether this instance is valid or not
		/// </summary>
		public bool IsValid { get; private set; }

		/// <summary>
		/// Account type glyph.
		/// </summary>
		public string Glyph
			=> PrincipalType switch
			{
				AccessControlPrincipalType.User => "\xE77B",
				AccessControlPrincipalType.Group => "\xE902",
				_ => "\xE716",
			};

		/// <summary>
		/// Account display name
		/// </summary>
		public string? DisplayName
			=> string.IsNullOrEmpty(Name) ? Sid : Name;

		/// <summary>
		/// Account full name or just name
		/// </summary>
		public string? FullNameHumanized
			=> string.IsNullOrEmpty(Domain) ? Name : $"{Domain}\\{Name}";

		/// <summary>
		/// Account humanized full name.
		/// </summary>
		public string FullNameHumanizedWithBrackes
			=> string.IsNullOrEmpty(Domain) ? string.Empty : $"({Domain}\\{Name})";

		public AccessControlPrincipal(string sid)
		{
			if (string.IsNullOrEmpty(sid))
				return;

			Sid = sid;
			PInvoke.ConvertStringSidToSid(sid, out var lpSid);

			char[] lpName = [];
			char[] lpDomain = [];
			uint cchName = 0, cchDomainName = 0;

			// Get size of account name and domain name
			bool bResult = PInvoke.LookupAccountSid(string.Empty, lpSid, lpName, ref cchName, lpDomain, ref cchDomainName, out _);

			// Ensure requested capacity
			lpName = new char[cchName];
			lpDomain = new char[cchDomainName];

			// Get account name and domain
			bResult = PInvoke.LookupAccountSid(string.Empty, lpSid, lpName, ref cchName, lpDomain, ref cchDomainName, out var snu);
			if(!bResult)
				return;

			PrincipalType = snu switch
			{
				// Group
				var x when
					(x == SID_NAME_USE.SidTypeAlias ||
					x == SID_NAME_USE.SidTypeGroup ||
					x == SID_NAME_USE.SidTypeWellKnownGroup)
					=> AccessControlPrincipalType.Group,

				// User
				SID_NAME_USE.SidTypeUser
					=> AccessControlPrincipalType.User,

				// Unknown
				_ => AccessControlPrincipalType.Unknown
			};

			// Replace domain name with computer name if the account type is user or alias type
			if (snu == SID_NAME_USE.SidTypeUser || snu == SID_NAME_USE.SidTypeAlias)
			{
				uint size = 256;
				lpDomain = new char[size];
				bResult = PInvoke.GetComputerName(lpDomain, ref size);
				if (!bResult)
					return;
			}

			Name = lpName.AsSpan().ToString();
			Domain = lpDomain.AsSpan().ToString().ToLower();

			IsValid = true;
		}
	}
}
