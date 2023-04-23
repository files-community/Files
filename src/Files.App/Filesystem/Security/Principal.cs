using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;

namespace Files.App.Filesystem.Security
{
	/// <summary>
	/// Represents a principal of an ACE or an owner of an ACL.
	/// </summary>
	public class Principal : ObservableObject
	{
		public string Glyph
			=> PrincipalType switch
			{
				PrincipalType.User => "\xE77B",
				PrincipalType.Group => "\xE902",
				_ => "\xE716",
			};

		public string? Sid { get; set; }

		public string? Domain { get; set; }

		public string? Name { get; set; }

		public string? DisplayName
			=> string.IsNullOrEmpty(Name) ? Sid : Name;

		public string? FullNameOrSid
			=> string.IsNullOrEmpty(Domain) ? Sid : $"{Domain}\\{Name}";

		public string? FullNameHumanized
			=> string.IsNullOrEmpty(Domain) ? string.Empty : $"({Domain}\\{Name})";

		public List<string> Groups { get; set; }

		private PrincipalType PrincipalType { get; set; }

		public Principal()
		{
			Groups = new();

			PrincipalType = PrincipalType.Unknown;
		}

		public static Principal? FromSid(string sid)
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

			bool bResult = LookupAccountSid(null, lpSid, lpName, ref cchName, lpDomain, ref cchDomainName, out _);

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

				lpDomain.Clear();

				if (snu == SID_NAME_USE.SidTypeUser || snu == SID_NAME_USE.SidTypeAlias)
				{
					lpDomain = new(256, 256);
					uint size = (uint)lpDomain.Capacity;
					bResult = Kernel32.GetComputerName(lpDomain, ref size);
				}

				userGroup.Name = lpName.ToString();
				userGroup.Domain = lpDomain.ToString().ToLower();
			}

			return userGroup;
		}
	}
}
