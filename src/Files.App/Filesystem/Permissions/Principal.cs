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
			if (string.IsNullOrEmpty(sid))
				return default;

			var userGroup = new Principal()
			{
				Sid = sid
			};

			var lpSid = ConvertStringSidToSid(userGroup.Sid);

			var userName = new StringBuilder();
			var domainName = new StringBuilder();
			int cchUserName = 0;
			int cchDomainName = 0;

			LookupAccountSid(null, lpSid, userName, ref cchUserName, domainName, ref cchDomainName, out _);

			userName.EnsureCapacity(cchUserName);
			domainName.EnsureCapacity(cchDomainName);

			if (LookupAccountSid(null, lpSid, userName, ref cchUserName, domainName, ref cchDomainName, out var secType))
			{
				userGroup.PrincipalType = secType switch
				{
					var x when
						x == SID_NAME_USE.SidTypeAlias ||
						x == SID_NAME_USE.SidTypeGroup ||
						x == SID_NAME_USE.SidTypeWellKnownGroup
						=> PrincipalType.Group,
					SID_NAME_USE.SidTypeUser => PrincipalType.User,
					_ => PrincipalType.Unknown
				};

				userGroup.Name = userName.ToString();
				userGroup.Domain = domainName.ToString();
			}

			return userGroup;
		}
		#endregion
	}

	public enum PrincipalType
	{
		/// <summary>
		/// User principal type
		/// </summary>
		User,

		/// <summary>
		/// Group principal type
		/// </summary>
		Group,

		/// <summary>
		/// Unknwon principal type
		/// </summary>
		Unknown
	};

	public enum WellKnownUserGroup
	{
		None,
		Everyone,
		AdminGroup,
		Administrator,
		LocalSystem,
	}
}
