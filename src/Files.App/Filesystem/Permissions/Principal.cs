using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Extensions;
using System.Collections.Generic;
using System.Text;
using static Vanara.PInvoke.AdvApi32;

namespace Files.App.Filesystem.Permissions
{
	public class Principal : ObservableObject
	{
		public Principal()
		{
			Groups = new();

			PrincipalType = PrincipalType.Unknown;
		}

		#region Properties
		public string Glyph
		{
			get
			{
				return PrincipalType switch
				{
					PrincipalType.User => "\xE2AF",
					PrincipalType.Group => "\xE902",
					_ => "\xE716",
				};
			}
		}

		public string? Sid { get; set; }

		public string? Domain { get; set; }

		public string? Name { get; set; }

		public string DisplayName
			=> string.IsNullOrEmpty(Name) ? "SecurityUnknownAccount".GetLocalizedResource() : Name;

		public string FullNameOrSid
			=> string.IsNullOrEmpty(Name) ? Sid : string.IsNullOrEmpty(Domain) ? Name : $"{Domain}\\{Name}";

		public List<string> Groups { get; set; }

		private PrincipalType PrincipalType { get; set; }
		#endregion

		#region Methods
		public static Principal FromSid(string sid)
		{
			var userGroup = new Principal()
			{
				Sid = sid
			};

			if (string.IsNullOrEmpty(sid))
				return userGroup;

			var lpSid = ConvertStringSidToSid(userGroup.Sid);

			var lpName = new StringBuilder();
			var lpDomain = new StringBuilder();
			int cchName = 0;
			int cchDomainName = 0;

			LookupAccountSid(null, lpSid, lpName, ref cchName, lpDomain, ref cchDomainName, out _);

			lpName.EnsureCapacity(cchName);
			lpDomain.EnsureCapacity(cchDomainName);

			if (LookupAccountSid(null, lpSid, lpName, ref cchName, lpDomain, ref cchDomainName, out var snu))
			{
				userGroup.PrincipalType = snu switch
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

				userGroup.Name = lpName.ToString();
				userGroup.Domain = lpDomain.ToString();
			}

			return userGroup;
		}
		#endregion
	}
}
