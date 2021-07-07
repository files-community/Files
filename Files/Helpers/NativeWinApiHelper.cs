﻿using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Files.Helpers
{
    public class NativeWinApiHelper
    {
        [DllImport("api-ms-win-core-processthreads-l1-1-0.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken([In] IntPtr ProcessHandle, TokenAccess DesiredAccess, out IntPtr TokenHandle);

        [DllImport("api-ms-win-core-processthreads-l1-1-2.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("api-ms-win-security-base-l1-1-0.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetTokenInformation(IntPtr hObject, TOKEN_INFORMATION_CLASS tokenInfoClass, IntPtr pTokenInfo, int tokenInfoLength, out int returnLength);

        [DllImport("api-ms-win-core-handle-l1-1-0.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("api-ms-win-security-base-l1-1-0.dll", ExactSpelling = true, SetLastError = true)]
        public static extern int GetLengthSid(IntPtr pSid);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptUnprotectData(
            in CRYPTOAPI_BLOB pDataIn,
            StringBuilder szDataDescr,
            in CRYPTOAPI_BLOB pOptionalEntropy,
            IntPtr pvReserved,
            IntPtr pPromptStruct,
            CryptProtectFlags dwFlags,
            out CRYPTOAPI_BLOB pDataOut);

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_USER
        {
            public SID_AND_ATTRIBUTES User;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;

            public uint Attributes;
        }

        [Flags]
        public enum CryptProtectFlags
        {
            CRYPTPROTECT_UI_FORBIDDEN = 0x1,

            CRYPTPROTECT_LOCAL_MACHINE = 0x4,

            CRYPTPROTECT_CRED_SYNC = 0x8,

            CRYPTPROTECT_AUDIT = 0x10,

            CRYPTPROTECT_NO_RECOVERY = 0x20,

            CRYPTPROTECT_VERIFY_PROTECTION = 0x40,

            CRYPTPROTECT_CRED_REGENERATE = 0x80
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPTOAPI_BLOB
        {
            public uint cbData;

            public IntPtr pbData;
        }

        public enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,

            TokenGroups,

            TokenPrivileges,

            TokenOwner,

            TokenPrimaryGroup,

            TokenDefaultDacl,

            TokenSource,

            TokenType,

            TokenImpersonationLevel,

            TokenStatistics,

            TokenRestrictedSids,

            TokenSessionId,

            TokenGroupsAndPrivileges,

            TokenSessionReference,

            TokenSandBoxInert,

            TokenAuditPolicy,

            TokenOrigin,

            TokenElevationType,

            TokenLinkedToken,

            TokenElevation,

            TokenHasRestrictions,

            TokenAccessInformation,

            TokenVirtualizationAllowed,

            TokenVirtualizationEnabled,

            TokenIntegrityLevel,

            TokenUIAccess,

            TokenMandatoryPolicy,

            TokenLogonSid,

            TokenIsAppContainer,

            TokenCapabilities,

            TokenAppContainerSid,

            TokenAppContainerNumber,

            TokenUserClaimAttributes,

            TokenDeviceClaimAttributes,

            TokenRestrictedUserClaimAttributes,

            TokenRestrictedDeviceClaimAttributes,

            TokenDeviceGroups,

            TokenRestrictedDeviceGroups,

            TokenSecurityAttributes,

            TokenIsRestricted
        }

        [Serializable]
        public enum TOKEN_TYPE
        {
            TokenPrimary = 1,

            TokenImpersonation = 2
        }

        [Flags]
        public enum TokenAccess : uint
        {
            TOKEN_ASSIGN_PRIMARY = 0x0001,

            TOKEN_DUPLICATE = 0x0002,

            TOKEN_IMPERSONATE = 0x0004,

            TOKEN_QUERY = 0x0008,

            TOKEN_QUERY_SOURCE = 0x0010,

            TOKEN_ADJUST_PRIVILEGES = 0x0020,

            TOKEN_ADJUST_GROUPS = 0x0040,

            TOKEN_ADJUST_DEFAULT = 0x0080,

            TOKEN_ADJUST_SESSIONID = 0x0100,

            TOKEN_ALL_ACCESS_P = 0x000F00FF,

            TOKEN_ALL_ACCESS = 0x000F01FF,

            TOKEN_READ = 0x00020008,

            TOKEN_WRITE = 0x000200E0,

            TOKEN_EXECUTE = 0x00020000
        }

        [DllImport("api-ms-win-core-wow64-l1-1-1.dll", SetLastError = true)]
        private static extern bool IsWow64Process2(
                IntPtr process,
                out ushort processMachine,
                out ushort nativeMachine);

        // https://stackoverflow.com/questions/54456140/how-to-detect-were-running-under-the-arm64-version-of-windows-10-in-net
        // https://docs.microsoft.com/en-us/windows/win32/sysinfo/image-file-machine-constants
        private static bool? isRunningOnArm = null;

        public static bool IsRunningOnArm
        {
            get
            {
                if (isRunningOnArm == null)
                {
                    isRunningOnArm = IsArmProcessor();
                    App.Logger.Info("Running on ARM: {0}", isRunningOnArm);
                }
                return isRunningOnArm ?? false;
            }
        }

        private static bool IsArmProcessor()
        {
            var handle = System.Diagnostics.Process.GetCurrentProcess().Handle;
            if (!IsWow64Process2(handle, out _, out var nativeMachine))
            {
                return false;
            }
            return (nativeMachine == 0xaa64 ||
                    nativeMachine == 0x01c0 ||
                    nativeMachine == 0x01c2 ||
                    nativeMachine == 0x01c4);
        }

        // https://www.travelneil.com/wndproc-in-uwp.html
        [ComImport, Guid("45D64A29-A63E-4CB6-B498-5781D298CB4F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface ICoreWindowInterop
        {
            IntPtr WindowHandle { get; }
            bool MessageHandled { get; }
        }

        public static IntPtr CoreWindowHandle => ((ICoreWindowInterop)(object)Windows.UI.Core.CoreWindow.GetForCurrentThread()).WindowHandle;
    }
}