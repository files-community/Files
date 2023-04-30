// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;

namespace Files.App.Filesystem.Security
{
	/// <summary>
	/// Represents a principal of an ACE or an owner of an ACL.
	/// </summary>
	public class Principal
	{
		/// <summary>
		/// Account type.
		/// </summary>
		public PrincipalType PrincipalType { get; private set; }

		/// <summary>
		/// Acount security identifier (SID).
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
				PrincipalType.User => "\xE77B",
				PrincipalType.Group => "\xE902",
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

		public Principal(string sid)
		{
			if (string.IsNullOrEmpty(sid))
				return;

			Sid = sid;
			var lpSid = ConvertStringSidToSid(sid);

			StringBuilder lpName = new(), lpDomain = new();
			int cchName = 0, cchDomainName = 0;

			// Get size of account name and domain name
			bool bResult = LookupAccountSid(null, lpSid, lpName, ref cchName, lpDomain, ref cchDomainName, out _);

			// Ensure requested capacity
			lpName.EnsureCapacity(cchName);
			lpDomain.EnsureCapacity(cchDomainName);

			// Get account name and domain
			bResult = LookupAccountSid(null, lpSid, lpName, ref cchName, lpDomain, ref cchDomainName, out var snu));
			if(!bResult)
				return;

			PrincipalType = snu switch
			{
				// Group
				var x when
					(x == SID_NAME_USE.SidTypeAlias ||
					x == SID_NAME_USE.SidTypeGroup ||
					x == SID_NAME_USE.SidTypeWellKnownGroup)
					=> PrincipalType.Group,

				// User
				SID_NAME_USE.SidTypeUser
					=> PrincipalType.User,

				// Unknown
				_ => PrincipalType.Unknown
			};

			lpDomain.Clear();

			// Replace domain name with computer name if the account type is user or alias type
			if (snu == SID_NAME_USE.SidTypeUser || snu == SID_NAME_USE.SidTypeAlias)
			{
				lpDomain = new(256, 256);
				uint size = (uint)lpDomain.Capacity;
				bResult = Kernel32.GetComputerName(lpDomain, ref size);
				if (!bResult)
					return;
			}

			Name = lpName.ToString();
			Domain = lpDomain.ToString().ToLower();

			IsValid = true;
		}
	}
}
