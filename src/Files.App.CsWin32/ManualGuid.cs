// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using Windows.Win32.System.WinRT;

namespace Windows.Win32
{
	public static unsafe partial class IID
	{
		public static Guid* IID_IStorageProviderStatusUISourceFactory
			=> (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IStorageProviderStatusUISourceFactory.Guid));

		[GuidRVAGen.Guid("000214E4-0000-0000-C000-000000000046")]
		public static partial Guid* IID_IContextMenu { get; }

		[GuidRVAGen.Guid("70629033-E363-4A28-A567-0DB78006E6D7")]
		public static partial Guid* IID_IEnumShellItems { get; }

		[GuidRVAGen.Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
		public static partial Guid* IID_IShellItem { get; }

		[GuidRVAGen.Guid("7E9FB0D3-919F-4307-AB2E-9B1860310C93")]
		public static partial Guid* IID_IShellItem2 { get; }

		[GuidRVAGen.Guid("947AAB5F-0A5C-4C13-B4D6-4BF7836FC9F8")]
		public static partial Guid* IID_IFileOperation { get; }

		[GuidRVAGen.Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
		public static partial Guid* IID_IFileOpenDialog { get; }

		[GuidRVAGen.Guid("84BCCD23-5FDE-4CDB-AEA4-AF64B83D78AB")]
		public static partial Guid* IID_IFileSaveDialog { get; }

		[GuidRVAGen.Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
		public static partial Guid* IID_IDesktopWallpaper { get; }

		[GuidRVAGen.Guid("2E941141-7F97-4756-BA1D-9DECDE894A3D")]
		public static partial Guid* IID_IApplicationActivationManager { get; }
	}

	public static unsafe partial class CLSID
	{
		[GuidRVAGen.Guid("3AD05575-8857-4850-9277-11B85BDB8E09")]
		public static partial Guid* CLSID_FileOperation { get; }

		[GuidRVAGen.Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
		public static partial Guid* CLSID_FileOpenDialog { get; }

		[GuidRVAGen.Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B")]
		public static partial Guid* CLSID_FileSaveDialog { get; }

		[GuidRVAGen.Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD")]
		public static partial Guid* CLSID_DesktopWallpaper { get; }

		[GuidRVAGen.Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
		public static partial Guid* CLSID_ApplicationActivationManager { get; }
	}

	public static unsafe partial class BHID
	{
		[GuidRVAGen.Guid("3981E225-F559-11D3-8E3A-00C04F6837D5")]
		public static partial Guid* BHID_SFUIObject { get; }

		[GuidRVAGen.Guid("94F60519-2850-4924-AA5A-D15E84868039")]
		public static partial Guid* BHID_EnumItems { get; }
	}

	public static unsafe partial class FOLDERID
	{
		[GuidRVAGen.Guid("B7534046-3ECB-4C18-BE4E-64CD4CB7D6AC")]
		public static partial Guid* FOLDERID_RecycleBinFolder { get; }
	}
}
