// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers
{
	public static partial class Win32PInvoke
	{
		public enum RM_APP_TYPE
		{
			RmUnknownApp = 0,
			RmMainWindow = 1,
			RmOtherWindow = 2,
			RmService = 3,
			RmExplorer = 4,
			RmConsole = 5,
			RmCritical = 1000
		}

		public enum File_Attributes : uint
		{
			Readonly = 0x00000001,
			Hidden = 0x00000002,
			System = 0x00000004,
			Directory = 0x00000010,
			Archive = 0x00000020,
			Device = 0x00000040,
			Normal = 0x00000080,
			Temporary = 0x00000100,
			SparseFile = 0x00000200,
			ReparsePoint = 0x00000400,
			Compressed = 0x00000800,
			Offline = 0x00001000,
			NotContentIndexed = 0x00002000,
			Encrypted = 0x00004000,
			Write_Through = 0x80000000,
			Overlapped = 0x40000000,
			NoBuffering = 0x20000000,
			RandomAccess = 0x10000000,
			SequentialScan = 0x08000000,
			DeleteOnClose = 0x04000000,
			BackupSemantics = 0x02000000,
			PosixSemantics = 0x01000000,
			OpenReparsePoint = 0x00200000,
			OpenNoRecall = 0x00100000,
			FirstPipeInstance = 0x00080000
		}

		public enum FILE_INFO_BY_HANDLE_CLASS
		{
			FileBasicInfo = 0,
			FileStandardInfo = 1,
			FileNameInfo = 2,
			FileRenameInfo = 3,
			FileDispositionInfo = 4,
			FileAllocationInfo = 5,
			FileEndOfFileInfo = 6,
			FileStreamInfo = 7,
			FileCompressionInfo = 8,
			FileAttributeTagInfo = 9,
			FileIdBothDirectoryInfo = 10,// 0x0A
			FileIdBothDirectoryRestartInfo = 11, // 0xB
			FileIoPriorityHintInfo = 12, // 0xC
			FileRemoteProtocolInfo = 13, // 0xD
			FileFullDirectoryInfo = 14, // 0xE
			FileFullDirectoryRestartInfo = 15, // 0xF
			FileStorageInfo = 16, // 0x10
			FileAlignmentInfo = 17, // 0x11
			FileIdInfo = 18, // 0x12
			FileIdExtdDirectoryInfo = 19, // 0x13
			FileIdExtdDirectoryRestartInfo = 20, // 0x14
			MaximumFileInfoByHandlesClass
		}

		public enum GET_FILEEX_INFO_LEVELS
		{
			GetFileExInfoStandard,
		}

		public enum StreamInfoLevels
		{
			FindStreamInfoStandard = 0
		}

		public enum FINDEX_INFO_LEVELS
		{
			FindExInfoStandard = 0,
			FindExInfoBasic = 1
		}

		public enum FINDEX_SEARCH_OPS
		{
			FindExSearchNameMatch = 0,
			FindExSearchLimitToDirectories = 1,
			FindExSearchLimitToDevices = 2
		}

		[Flags]
		public enum ClassContext : uint
		{
			LocalServer = 0x4
		}
	}
}
