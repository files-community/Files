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

		[GuidRVAGen.Guid("E9C5EF8D-FD41-4F72-BA87-EB03BAD5817C")]
		public static partial Guid* IID_IAutomaticDestinationList { get; }

		[GuidRVAGen.Guid("6332DEBF-87B5-4670-90C0-5E57B408A49E")]
		public static partial Guid* IID_ICustomDestinationList { get; }

		[GuidRVAGen.Guid("5632B1A4-E38A-400A-928A-D4CD63230295")]
		public static partial Guid* IID_IObjectCollection { get; }

		[GuidRVAGen.Guid("00000000-0000-0000-C000-000000000046")]
		public static partial Guid* IID_IUnknown { get; }

		[GuidRVAGen.Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
		public static partial Guid* IID_IPropertyStore { get; }

		[GuidRVAGen.Guid("507101CD-F6AD-46C8-8E20-EEB9E6BAC47F")]
		public static partial Guid* IID_IInternalCustomDestinationList { get; }

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

		[GuidRVAGen.Guid("00021500-0000-0000-C000-000000000046")]
		public static partial Guid* IID_IQueryInfo { get; }

		[GuidRVAGen.Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B")]
		public static partial Guid* IID_IShellItemImageFactory { get; }

		[GuidRVAGen.Guid("000214F9-0000-0000-C000-000000000046")]
		public static partial Guid* IID_IShellLinkW { get; }

		[GuidRVAGen.Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
		public static partial Guid* IID_IShellItemArray { get; }

		[GuidRVAGen.Guid("7F9185B0-CB92-43C5-80A9-92277A4F7B54")]
		public static partial Guid* IID_IExecuteCommand { get; }

		[GuidRVAGen.Guid("1C9CD5BB-98E9-4491-A60F-31AACC72B83C")]
		public static partial Guid* IID_IObjectWithSelection { get; }

		[GuidRVAGen.Guid("000214E8-0000-0000-C000-000000000046")]
		public static partial Guid* IID_IShellExtInit { get; }

		[GuidRVAGen.Guid("000214F4-0000-0000-C000-000000000046")]
		public static partial Guid* IID_IContextMenu2 { get; }

		[GuidRVAGen.Guid("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9")]
		public static partial Guid* IID_IObjectArray { get; }

		[GuidRVAGen.Guid("000214FA-0000-0000-C000-000000000046")]
		public static partial Guid* IID_IExtractIconW { get; }

		[GuidRVAGen.Guid("000214E6-0000-0000-C000-000000000046")]
		public static partial Guid* IID_IShellFolder { get; }
	}

	public static unsafe partial class CLSID
	{
		[GuidRVAGen.Guid("F0AE1542-F497-484B-A175-A20DB09144BA")]
		public static partial Guid* CLSID_AutomaticDestinationList { get; }

		[GuidRVAGen.Guid("77F10CF0-3DB5-4966-B520-B7C54FD35ED6")]
		public static partial Guid* CLSID_DestinationList { get; }

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

		[GuidRVAGen.Guid("B455F46E-E4AF-4035-B0A4-CF18D2F6F28E")]
		public static partial Guid* CLSID_PinToFrequentExecute { get; }

		[GuidRVAGen.Guid("EE20EEBA-DF64-4A4E-B7BB-2D1C6B2DFCC1")]
		public static partial Guid* CLSID_UnPinFromFrequentExecute { get; }

		[GuidRVAGen.Guid("D969A300-E7FF-11d0-A93B-00A0C90F2719")]
		public static partial Guid* CLSID_NewMenu { get; }

		[GuidRVAGen.Guid("2D3468C1-36A7-43B6-AC24-D3F02FD9607A")]
		public static partial Guid* CLSID_EnumerableObjectCollection { get; }

		[GuidRVAGen.Guid("00021401-0000-0000-C000-000000000046")]
		public static partial Guid* CLSID_ShellLink { get; }
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

		[GuidRVAGen.Guid("AE50C081-EBD2-438A-8655-8A092E34987A")]
		public static partial Guid* FOLDERID_Recent { get; }
	}
}
