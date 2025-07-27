// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using static Files.App.Helpers.Win32PInvoke;

namespace Files.App.Utils.Signatures
{
    public static class DigitalSignaturesUtil
    {
        // OIDs
        private const string szOID_NESTED_SIGNATURE = "1.3.6.1.4.1.311.2.4.1";
        private const string szOID_RSA_counterSign = "1.2.840.113549.1.9.6";
        private const string szOID_RSA_signingTime = "1.2.840.113549.1.9.5";
        private const string szOID_RFC3161_counterSign = "1.3.6.1.4.1.311.3.3.1";
        private const string szOID_OIWSEC_sha1 = "1.3.14.3.2.26";
        private const string szOID_RSA_MD5 = "1.2.840.113549.2.5";
        private const string szOID_NIST_sha256 = "2.16.840.1.101.3.4.2.1";

        // Flags
        private const uint CERT_NAME_SIMPLE_DISPLAY_TYPE = 4;
        private const uint CERT_FIND_SUBJECT_NAME = 131079;
        private const uint CERT_FIND_ISSUER_NAME = 131076;
        private const uint CERT_QUERY_OBJECT_FILE = 0x00000001;
        private const uint CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED = 0x00000400;
        private const uint CERT_QUERY_FORMAT_FLAG_BINARY = 0x00000002;
        private const uint CERT_SYSTEM_STORE_CURRENT_USER = 0x00010000;
        private const IntPtr CERT_STORE_PROV_SYSTEM = 10;
        private const IntPtr CERT_STORE_PROV_MSG = 1;

        private const uint PKCS_7_ASN_ENCODING = 0x00010000;
        private const uint CRYPT_ASN_ENCODING = 0x00000001;

        private const IntPtr PKCS7_SIGNER_INFO = 500;
        private const IntPtr PKCS_UTC_TIME = 17;

        private const uint CMSG_SIGNER_INFO_PARAM = 6;

        // Version numbers
        private const uint CERT_V1 = 0;
        private const uint CERT_V2 = 1;
        private const uint CERT_V3 = 2;

        private static readonly byte[] SG_ProtoCoded = [
            0x30, 0x82
        ];

        private static readonly byte[] SG_SignedData = [
            0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x07, 0x02
        ];

        private static readonly IDateTimeFormatter formatter = Ioc.Default.GetRequiredService<IDateTimeFormatter>();

        public static void LoadItemSignatures(
            string filePath,
            ObservableCollection<SignatureInfoItem> signatures,
            IntPtr hWnd,
            CancellationToken ct)
        {
            var signChain = new List<SignNodeInfo>();
            GetSignerCertificateInfo(filePath, signChain, ct);

            foreach (var signNode in signChain)
            {
                if (signNode.CertChain.Count == 0)
                    continue;

                var signatureInfo = new SignatureInfoItem(filePath, signNode.Index, hWnd, signNode.CertChain)
                {
                    Version = signNode.Version,
                    IssuedBy = signNode.CertChain[0].IssuedBy,
                    IssuedTo = signNode.CertChain[0].IssuedTo,
                    ValidFromTimestamp = signNode.CertChain[0].ValidFrom,
                    ValidToTimestamp = signNode.CertChain[0].ValidTo,
                    VerifiedTimestamp = signNode.CounterSign.TimeStamp,
                    Verified = signNode.IsValid,
                };
                signatures.Add(signatureInfo);
            }
        }

        public static void DisplaySignerInfoDialog(string filePath, IntPtr hwndParent, int index)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            var hAuthCryptMsg = IntPtr.Zero;
            var signHandle = new SignDataHandle();
            var signDataChain = new List<SignDataHandle>();

            try
            {
                var result = TryGetSignerInfo(
                    filePath,
                    ref hAuthCryptMsg,
                    ref signHandle.hCertStoreHandle,
                    ref signHandle.pSignerInfo,
                    ref signHandle.dwObjSize
                );
                if (!result || signHandle.pSignerInfo == IntPtr.Zero)
                    return;

                signDataChain.Add(signHandle);
                GetNestedSignerInfo(ref signHandle, signDataChain);
                if (index >= signDataChain.Count)
                    return;

                signHandle = signDataChain[index];
                var signerInfo = Marshal.PtrToStructure<CMSG_SIGNER_INFO>(signHandle.pSignerInfo);
                var pIssuer = Marshal.AllocHGlobal(Marshal.SizeOf<CRYPTOAPI_BLOB>());
                Marshal.StructureToPtr(signerInfo.Issuer, pIssuer, false);
                var pCertContext = CertFindCertificateInStore(
                    signHandle.hCertStoreHandle,
                    PKCS_7_ASN_ENCODING | CRYPT_ASN_ENCODING,
                    0,
                    CERT_FIND_ISSUER_NAME,
                    pIssuer,
                    IntPtr.Zero
                );
                Marshal.FreeHGlobal(pIssuer);
                if (pCertContext == IntPtr.Zero)
                    return;

                var viewInfo = new CRYPTUI_VIEWSIGNERINFO_STRUCT
                {
                    dwSize = (uint)Marshal.SizeOf<CRYPTUI_VIEWSIGNERINFO_STRUCT>(),
                    hwndParent = hwndParent,
                    dwFlags = 0,
                    szTitle = IntPtr.Zero,
                    pSignerInfo = signHandle.pSignerInfo,
                    hMsg = hAuthCryptMsg,
                    pszOID = IntPtr.Zero,
                    dwReserved = null,
                    cStores = 1,
                    rghStores = Marshal.AllocHGlobal(IntPtr.Size),
                    cPropPages = 0,
                    rgPropPages = IntPtr.Zero
                };
                Marshal.WriteIntPtr(viewInfo.rghStores, signHandle.hCertStoreHandle);
                var pViewInfo = Marshal.AllocHGlobal((int)viewInfo.dwSize);
                Marshal.StructureToPtr(viewInfo, pViewInfo, false);

                result = CryptUIDlgViewSignerInfo(pViewInfo);

                Marshal.FreeHGlobal(viewInfo.rghStores);
                Marshal.FreeHGlobal(pViewInfo);
                CertFreeCertificateContext(pCertContext);
            }
            finally
            {
                // Since signDataChain contains nested signatures,
                // you must release them starting from the last one.
                for (int i = signDataChain.Count - 1; i >= 0; i--)
                {
                    if (signDataChain[i].pSignerInfo != IntPtr.Zero)
                        Marshal.FreeHGlobal(signDataChain[i].pSignerInfo);

                    if (signDataChain[i].hCertStoreHandle != IntPtr.Zero)
                        CertCloseStore(signDataChain[i].hCertStoreHandle, 0);
                }

                if (hAuthCryptMsg != IntPtr.Zero)
                    CryptMsgClose(hAuthCryptMsg);
            }
        }

        private static bool GetSignerSignatureInfo(
            IntPtr hSystemStore,
            IntPtr hCertStore,
            IntPtr pOrigContext,
            ref IntPtr pCurrContext,
            SignNodeInfo signNode)
        {
            var currContext = Marshal.PtrToStructure<CERT_CONTEXT>(pCurrContext);
            var pCertInfo = currContext.pCertInfo;
            var certNode = new CertNodeInfoItem();
            var certInfo = Marshal.PtrToStructure<CERT_INFO>(pCertInfo);

            (_, certNode.Version) = CalculateSignVersion(certInfo.dwVersion);
            GetStringFromCertContext(pCurrContext, CERT_NAME_SIMPLE_DISPLAY_TYPE, 0, certNode);
            GetStringFromCertContext(pCurrContext, CERT_NAME_SIMPLE_DISPLAY_TYPE, 1, certNode);

            var pft = Marshal.AllocHGlobal(Marshal.SizeOf<FILETIME>());
            Marshal.StructureToPtr(certInfo.NotBefore, pft, false);
            certNode.ValidFrom = TimeToString(pft);
            Marshal.StructureToPtr(certInfo.NotAfter, pft, false);
            certNode.ValidTo = TimeToString(pft);
            Marshal.FreeHGlobal(pft);

            signNode.CertChain.Add(certNode);

            var pIssuer = Marshal.AllocHGlobal(Marshal.SizeOf<CRYPTOAPI_BLOB>());
            Marshal.StructureToPtr(certInfo.Issuer, pIssuer, false);
            pCurrContext = CertFindCertificateInStore(
                hCertStore,
                PKCS_7_ASN_ENCODING | CRYPT_ASN_ENCODING,
                0,
                CERT_FIND_SUBJECT_NAME,
                pIssuer,
                IntPtr.Zero
            );

            if (pCurrContext == IntPtr.Zero)
            {
                pCurrContext = CertFindCertificateInStore(
                    hSystemStore,
                    PKCS_7_ASN_ENCODING | CRYPT_ASN_ENCODING,
                    0,
                    CERT_FIND_SUBJECT_NAME,
                    pIssuer,
                    IntPtr.Zero
                );
            }

            Marshal.FreeHGlobal(pIssuer);
            if (pCurrContext == IntPtr.Zero)
                return false;

            var pCurrPublicKey = Marshal.AllocHGlobal(Marshal.SizeOf<CERT_PUBLIC_KEY_INFO>());
            Marshal.StructureToPtr(certInfo.SubjectPublicKeyInfo, pCurrPublicKey, false);

            var origContext = Marshal.PtrToStructure<CERT_CONTEXT>(pOrigContext);
            var origInfo = Marshal.PtrToStructure<CERT_INFO>(origContext.pCertInfo);
            var pOrigPublicKey = Marshal.AllocHGlobal(Marshal.SizeOf<CERT_PUBLIC_KEY_INFO>());
            Marshal.StructureToPtr(origInfo.SubjectPublicKeyInfo, pOrigPublicKey, false);

            var result = CertComparePublicKeyInfo(
                PKCS_7_ASN_ENCODING | CRYPT_ASN_ENCODING,
                pCurrPublicKey,
                pOrigPublicKey
            );

            Marshal.FreeHGlobal(pCurrPublicKey);
            Marshal.FreeHGlobal(pOrigPublicKey);

            return !result;
        }

        private static bool GetSignerCertificateInfo(string fileName, List<SignNodeInfo> signChain, CancellationToken ct)
        {
            var succeded = false;
            var authSignData = new SignDataHandle();
            var signDataChain = new List<SignDataHandle>();
            signChain.Clear();

            var pRoot = Marshal.StringToHGlobalAuto("Root");
            var hSystemStore = CertOpenStore(
                CERT_STORE_PROV_SYSTEM,
                PKCS_7_ASN_ENCODING | CRYPT_ASN_ENCODING,
                IntPtr.Zero,
                CERT_SYSTEM_STORE_CURRENT_USER,
                pRoot
            );
            Marshal.FreeHGlobal(pRoot);
            if (hSystemStore == IntPtr.Zero)
                return false;

            var hAuthCryptMsg = IntPtr.Zero;
            var result = TryGetSignerInfo(
                fileName,
                ref hAuthCryptMsg,
                ref authSignData.hCertStoreHandle,
                ref authSignData.pSignerInfo,
                ref authSignData.dwObjSize
            );

            if (hAuthCryptMsg != IntPtr.Zero)
            {
                CryptMsgClose(hAuthCryptMsg);
                hAuthCryptMsg = IntPtr.Zero;
            }

            if (!result)
            {
                if (authSignData.hCertStoreHandle != IntPtr.Zero)
                    CertCloseStore(authSignData.hCertStoreHandle, 0);

                CertCloseStore(hSystemStore, 0);
                return false;
            }

            signDataChain.Add(authSignData);
            GetNestedSignerInfo(ref authSignData, signDataChain);

            for (var i = 0; i < signDataChain.Count; i++)
            {
                if (ct.IsCancellationRequested)
                {
                    CertCloseStore(hSystemStore, 0);
                    return false;
                }

                var pCurrContext = IntPtr.Zero;
                var pCounterSigner = IntPtr.Zero;
                var signNode = new SignNodeInfo();

                GetCounterSignerInfo(signDataChain[i].pSignerInfo, ref pCounterSigner);
                if (pCounterSigner != IntPtr.Zero)
                    GetCounterSignerData(pCounterSigner, signNode.CounterSign);
                else
                    GetGeneralizedTimeStamp(signDataChain[i].pSignerInfo, signNode.CounterSign);

                var signerInfo = Marshal.PtrToStructure<CMSG_SIGNER_INFO>(signDataChain[i].pSignerInfo);
                var szObjId = signerInfo.HashAlgorithm.pszObjId;
                CalculateDigestAlgorithm(szObjId, signNode);
                (_, signNode.Version) = CalculateSignVersion(signerInfo.dwVersion);

                var pIssuer = Marshal.AllocHGlobal(Marshal.SizeOf<CRYPTOAPI_BLOB>());
                Marshal.StructureToPtr(signerInfo.Issuer, pIssuer, false);
                pCurrContext = CertFindCertificateInStore(
                    signDataChain[i].hCertStoreHandle,
                    PKCS_7_ASN_ENCODING | CRYPT_ASN_ENCODING,
                    0,
                    CERT_FIND_ISSUER_NAME,
                    pIssuer,
                    IntPtr.Zero
                );
                Marshal.FreeHGlobal(pIssuer);

                result = pCurrContext != IntPtr.Zero;
                while (result)
                {
                    var pOrigContext = pCurrContext;
                    result = GetSignerSignatureInfo(
                        hSystemStore,
                        signDataChain[i].hCertStoreHandle,
                        pOrigContext,
                        ref pCurrContext,
                        signNode
                    );
                    CertFreeCertificateContext(pOrigContext);
                }

                if (pCurrContext != IntPtr.Zero)
                    CertFreeCertificateContext(pCurrContext);

                if (pCounterSigner != IntPtr.Zero)
                    Marshal.FreeHGlobal(pCounterSigner);

                if (signDataChain[i].pSignerInfo != IntPtr.Zero)
                    Marshal.FreeHGlobal(signDataChain[i].pSignerInfo);

                if (signDataChain[i].hCertStoreHandle != IntPtr.Zero)
                    CertCloseStore(signDataChain[i].hCertStoreHandle, 0);

                succeded = true;
                signNode.IsValid = VerifyySignature(fileName);
                signNode.Index = i;
                signChain.Add(signNode);
            }

            CertCloseStore(hSystemStore, 0);
            return succeded;
        }

        private static bool VerifyySignature(string certPath)
        {
            var actionGuid = new Guid("{00AAC56B-CD44-11D0-8CC2-00C04FC295EE}");
            var guidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(actionGuid));
            Marshal.StructureToPtr(actionGuid, guidPtr, false);

            var sFileInfo = Marshal.SizeOf<WINTRUST_FILE_INFO>();
            var fileInfo = new WINTRUST_FILE_INFO
            {
                cbStruct = (uint)sFileInfo,
                pcwszFilePath = Marshal.StringToCoTaskMemAuto(certPath),
                hFile = IntPtr.Zero,
                pgKnownSubject = IntPtr.Zero
            };
            var filePtr = Marshal.AllocHGlobal(sFileInfo);
            Marshal.StructureToPtr(fileInfo, filePtr, false);

            var sData = Marshal.SizeOf<WINTRUST_DATA>();
            var wintrustData = new WINTRUST_DATA
            {
                cbStruct = (uint)sData,
                pPolicyCallbackData = IntPtr.Zero,
                pSIPClientData = IntPtr.Zero,
                dwUIChoice = 2,             // Display no UI
                fdwRevocationChecks = 0,    // No revocation checking
                dwUnionChoice = 1,          // Verify an embedded signature on a file
                dwStateAction = 1,          // Verify action
                hVWTStateData = IntPtr.Zero,
                pwszURLReference = IntPtr.Zero,
                dwUIContext = 0,
                pFile = filePtr
            };
            var dataPtr = Marshal.AllocHGlobal(sData);
            Marshal.StructureToPtr(wintrustData, dataPtr, false);

            try
            {
                var res = WinVerifyTrust(IntPtr.Zero, guidPtr, dataPtr);

                // Release hWVTStateData
                wintrustData.dwStateAction = 2; // Close
                Marshal.StructureToPtr(wintrustData, dataPtr, true);
                WinVerifyTrust(IntPtr.Zero, guidPtr, dataPtr);

                return res == 0;
            }
            finally
            {
                if (fileInfo.pcwszFilePath != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(fileInfo.pcwszFilePath);

                Marshal.FreeHGlobal(guidPtr);
                Marshal.FreeHGlobal(filePtr);
                Marshal.FreeHGlobal(dataPtr);
            }
        }

        private static bool TryGetSignerInfo(
           string fileName,
           ref IntPtr hMsg,
           ref IntPtr hCertStore,
           ref IntPtr pSignerInfo,
           ref uint signerSize,
           uint index = 0)
        {
            uint encoding = 0;
            var pDummy = IntPtr.Zero;
            uint dummy = 0;
            var pFileName = Marshal.StringToHGlobalAuto(fileName);
            var result = CryptQueryObject(
                CERT_QUERY_OBJECT_FILE,
                pFileName,
                CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED,
                CERT_QUERY_FORMAT_FLAG_BINARY,
                0,
                ref encoding,
                ref dummy,
                ref dummy,
                ref hCertStore,
                ref hMsg,
                ref pDummy
            );
            Marshal.FreeHGlobal(pFileName);

            if (!result)
                return false;

            result = CustomCryptMsgGetParam(
                hMsg,
                CMSG_SIGNER_INFO_PARAM,
                index,
                ref pSignerInfo,
                ref signerSize
            );

            return result;
        }

        private static bool GetCounterSignerInfo(IntPtr pSignerInfo, ref IntPtr pTargetSigner)
        {
            uint objSize = 0;
            if (pSignerInfo == IntPtr.Zero)
                return false;

            try
            {
                var res = TryGetUnauthAttr(pSignerInfo, szOID_RSA_counterSign, out var attr);
                if (!res)
                    return false;

                var rgValue = Marshal.PtrToStructure<CRYPTOAPI_BLOB>(attr.rgValue);
                var result = CryptDecodeObject(
                    PKCS_7_ASN_ENCODING | CRYPT_ASN_ENCODING,
                    PKCS7_SIGNER_INFO,
                    rgValue.pbData,
                    rgValue.cbData,
                    0,
                    IntPtr.Zero,
                    ref objSize
                );
                if (!result)
                    return false;

                pTargetSigner = Marshal.AllocHGlobal((int)objSize * sizeof(byte));
                if (pTargetSigner == IntPtr.Zero)
                    return false;

                result = CryptDecodeObject(
                    PKCS_7_ASN_ENCODING | CRYPT_ASN_ENCODING,
                    PKCS7_SIGNER_INFO,
                    rgValue.pbData,
                    rgValue.cbData,
                    0,
                    pTargetSigner,
                    ref objSize
                );
                if (!result)
                    return false;
            }
            finally
            {
            }

            return true;
        }

        private static bool GetCounterSignerData(IntPtr pSignerInfo, SignCounterSign counterSign)
        {
            var res = TryGetAuthAttr(pSignerInfo, szOID_RSA_signingTime, out var attr);
            if (!res)
                return false;

            var rgValue = Marshal.PtrToStructure<CRYPTOAPI_BLOB>(attr.rgValue);

            var data = (uint)Marshal.SizeOf<FILETIME>();
            var ft = Marshal.AllocHGlobal((int)data);
            IntPtr lft = IntPtr.Zero;
            IntPtr st = IntPtr.Zero;

            try
            {
                var pStructType = Marshal.StringToHGlobalUni(szOID_RSA_signingTime);
                var result = CryptDecodeObject(
                    PKCS_7_ASN_ENCODING | CRYPT_ASN_ENCODING,
                    PKCS_UTC_TIME,
                    rgValue.pbData,
                    rgValue.cbData,
                    0,
                    ft,
                    ref data
                );
                Marshal.FreeHGlobal(pStructType);
                if (!result)
                    return false;

                lft = Marshal.AllocHGlobal((int)data);
                st = Marshal.AllocHGlobal(Marshal.SizeOf<SYSTEMTIME>());
                FileTimeToLocalFileTime(ft, lft);
                FileTimeToSystemTime(lft, st);
                counterSign.TimeStamp = TimeToString(IntPtr.Zero, st);

                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(ft);
                if (lft != IntPtr.Zero)
                    Marshal.FreeHGlobal(lft);

                if (st != IntPtr.Zero)
                    Marshal.FreeHGlobal(st);
            }
        }

        private static bool ParseDERFindType(
            int typeSearch,
            IntPtr pbSignature,
            uint size,
            ref uint positionFound,
            ref uint lengthFound)
        {
            uint position = 0;
            uint sizeFound = 0;
            uint bytesParsed = 0;
            var iType = 0;
            var iClass = 0;
            positionFound = 0;
            lengthFound = 0;
            if (pbSignature == IntPtr.Zero)
                return false;

            while (size > position)
            {
                if (!SafeToReadNBytes(size, position, 2))
                    return false;

                ParseDERType(Marshal.ReadByte(pbSignature, (int)position), ref iType, ref iClass);
                switch (iType)
                {
                    case 0x05: // Null
                        ++position;
                        if (Marshal.ReadByte(pbSignature, (int)position) != 0x00)
                            return false;

                        ++position;
                        break;

                    case 0x06: // Object Identifier
                        ++position;
                        var val = Marshal.ReadByte(pbSignature, (int)position);
                        if (!SafeToReadNBytes(size - position, 1, val))
                            return false;

                        position += 1u + val;
                        break;

                    case 0x00: // ?
                    case 0x01: // Boolean
                    case 0x02: // Integer
                    case 0x03: // Bit String
                    case 0x04: // Octet String
                    case 0x0A: // enumerated
                    case 0x0C: // UTF8string
                    case 0x13: // printable string
                    case 0x14: // T61 string
                    case 0x16: // IA5String
                    case 0x17: // UTC time
                    case 0x18: // Generalized time
                    case 0x1E: // BMPstring
                        ++position;
                        if (!ParseDERSize(
                            IntPtr.Add(pbSignature, (int)position),
                            size - position,
                            ref sizeFound,
                            ref bytesParsed))
                        {
                            return false;
                        }

                        position += bytesParsed;
                        if (!SafeToReadNBytes(size - position, 0, sizeFound))
                            return false;

                        if (typeSearch == iType)
                        {
                            positionFound = position;
                            lengthFound = sizeFound;

                            return true;
                        }

                        position += sizeFound;
                        break;

                    case 0x20: // context specific
                    case 0x21: // context specific
                    case 0x23: // context specific
                    case 0x24: // context specific
                    case 0x30: // sequence
                    case 0x31: // set
                        position++;
                        if (!ParseDERSize(
                            IntPtr.Add(pbSignature, (int)position),
                            size - position,
                            ref sizeFound,
                            ref bytesParsed))
                        {
                            return false;
                        }

                        position += bytesParsed;
                        break;

                    case 0x22: // ?
                        position += 2;
                        break;

                    default:
                        return false;
                }
            }

            return false;
        }

        private static bool GetGeneralizedTimeStamp(
            IntPtr pSignerInfo,
            SignCounterSign counter)
        {
            uint positionFound = 0;
            uint lengthFound = 0;
            var res = TryGetUnauthAttr(pSignerInfo, szOID_RFC3161_counterSign, out var attr);
            if (!res)
                return false;

            var rgValue = Marshal.PtrToStructure<CRYPTOAPI_BLOB>(attr.rgValue);
            var result = ParseDERFindType(
                0x04,
                rgValue.pbData,
                rgValue.cbData,
                ref positionFound,
                ref lengthFound);
            if (!result)
                return false;

            // Counter Signer Timstamp
            var pbOctetString = IntPtr.Add(rgValue.pbData, (int)positionFound);
            counter.TimeStamp = GetTimeStampFromDER(pbOctetString, lengthFound, ref positionFound);
            
            return true;
        }

        private static string GetTimeStampFromDER(IntPtr pbOctetString, uint lengthFound, ref uint positionFound)
        {
            var result = ParseDERFindType(
                0x18,
                pbOctetString,
                lengthFound,
                ref positionFound,
                ref lengthFound
            );
            if (!result)
                return string.Empty;

            var st = new SYSTEMTIME();
            var buffer = Marshal.PtrToStringUTF8(
                IntPtr.Add(pbOctetString, (int)positionFound),
                (int)lengthFound
            ) + (char)0;

            _ = short.TryParse(buffer.AsSpan(0, 4), out st.Year);
            _ = short.TryParse(buffer.AsSpan(4, 2), out st.Month);
            _ = short.TryParse(buffer.AsSpan(6, 2), out st.Day);
            _ = short.TryParse(buffer.AsSpan(8, 2), out st.Hour);
            _ = short.TryParse(buffer.AsSpan(10, 2), out st.Minute);
            _ = short.TryParse(buffer.AsSpan(12, 2), out st.Second);
            _ = short.TryParse(buffer.AsSpan(15, 3), out st.Milliseconds);

            var sst = Marshal.AllocHGlobal(Marshal.SizeOf<SYSTEMTIME>());
            var lst = Marshal.AllocHGlobal(Marshal.SizeOf<SYSTEMTIME>());
            var fft = Marshal.AllocHGlobal(Marshal.SizeOf<FILETIME>());
            var lft = Marshal.AllocHGlobal(Marshal.SizeOf<FILETIME>());
            Marshal.StructureToPtr(st, sst, true);
            SystemTimeToFileTime(sst, fft);
            FileTimeToLocalFileTime(fft, lft);
            FileTimeToSystemTime(lft, lst);
            var timestamp = TimeToString(IntPtr.Zero, lst);

            Marshal.FreeHGlobal(fft);
            Marshal.FreeHGlobal(lft);
            Marshal.FreeHGlobal(sst);
            Marshal.FreeHGlobal(lst);

            return timestamp;
        }

        private static bool GetStringFromCertContext(IntPtr pCertContext, uint dwType, uint flag, CertNodeInfoItem info)
        {
            var data = CertGetNameStringA(pCertContext, dwType, flag, IntPtr.Zero, IntPtr.Zero, 0);
            if (data == 0)
            {
                CertFreeCertificateContext(pCertContext);
                return false;
            }

            var pszTempName = Marshal.AllocHGlobal((int)data * sizeof(byte));
            if (pszTempName == IntPtr.Zero)
            {
                CertFreeCertificateContext(pCertContext);
                return false;
            }

            data = CertGetNameStringA(pCertContext, dwType, flag, IntPtr.Zero, pszTempName, data);
            if (data == 0)
            {
                Marshal.FreeHGlobal(pszTempName);
                return false;
            }

            if (flag == 0)
                info.IssuedTo = StripString(Marshal.PtrToStringUTF8(pszTempName));
            else
                info.IssuedBy = StripString(Marshal.PtrToStringUTF8(pszTempName));

            Marshal.FreeHGlobal(pszTempName);

            return true;
        }

        private static bool TryGetUnauthAttr(IntPtr pSignerInfo, string oid, out CRYPT_ATTRIBUTE attr)
        {
            int n = 0;
            var signerInfo = Marshal.PtrToStructure<CMSG_SIGNER_INFO>(pSignerInfo);
            attr = new CRYPT_ATTRIBUTE();
            for (; n < signerInfo.UnauthAttrs.cbData; n++)
            {
                attr = Marshal.PtrToStructure<CRYPT_ATTRIBUTE>(
                    IntPtr.Add(signerInfo.UnauthAttrs.pbData, n * Marshal.SizeOf<CRYPT_ATTRIBUTE>())
                );
                if (attr.pszObjId == oid)
                    break;
            }

            return n < signerInfo.UnauthAttrs.cbData;
        }

        private static bool TryGetAuthAttr(IntPtr pSignerInfo, string oid, out CRYPT_ATTRIBUTE attr)
        {
            int n = 0;
            var signerInfo = Marshal.PtrToStructure<CMSG_SIGNER_INFO>(pSignerInfo);
            attr = new CRYPT_ATTRIBUTE();
            for (; n < signerInfo.AuthAttrs.cbData; n++)
            {
                attr = Marshal.PtrToStructure<CRYPT_ATTRIBUTE>(
                    IntPtr.Add(signerInfo.AuthAttrs.pbData, n * Marshal.SizeOf<CRYPT_ATTRIBUTE>())
                );
                if (attr.pszObjId == oid)
                    break;
            }

            return n < signerInfo.AuthAttrs.cbData;
        }

        private static bool GetNestedSignerInfo(ref SignDataHandle AuthSignData, List<SignDataHandle> NestedChain)
        {
            var succeded = false;
            var hNestedMsg = IntPtr.Zero;
            if (AuthSignData.pSignerInfo == IntPtr.Zero)
                return false;

            try
            {
                var res = TryGetUnauthAttr(AuthSignData.pSignerInfo, szOID_NESTED_SIGNATURE, out var attr);
                if (!res)
                    return false;

                var rgValue = Marshal.PtrToStructure<CRYPTOAPI_BLOB>(attr.rgValue);
                var cbCurrData = rgValue.cbData;
                var pbCurrData = rgValue.pbData;

                var upperBound = IntPtr.Add(AuthSignData.pSignerInfo, (int)AuthSignData.dwObjSize);
                while (pbCurrData > AuthSignData.pSignerInfo && pbCurrData < upperBound)
                {
                    var nestedHandle = new SignDataHandle() { dwObjSize = 0 };
                    if (!Memcmp(pbCurrData, SG_ProtoCoded) ||
                        !Memcmp(IntPtr.Add(pbCurrData, 6), SG_SignedData))
                    {
                        break;
                    }

                    hNestedMsg = CryptMsgOpenToDecode(
                        PKCS_7_ASN_ENCODING | CRYPT_ASN_ENCODING,
                        0,
                        0,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero
                    );
                    if (hNestedMsg == IntPtr.Zero)
                        return false;

                    cbCurrData = XCHWordLitend(unchecked((ushort)Marshal.ReadInt16(pbCurrData)) + 2u) + 4u;
                    var pbNextData = pbCurrData;
                    pbNextData = IntPtr.Add(pbNextData, (int)EightByteAlign(cbCurrData, unchecked(pbCurrData)));
                    var result = CryptMsgUpdate(hNestedMsg, pbCurrData, cbCurrData, true);
                    pbCurrData = pbNextData;
                    if (!result)
                        continue;

                    result = CustomCryptMsgGetParam(
                        hNestedMsg,
                        CMSG_SIGNER_INFO_PARAM,
                        0,
                        ref nestedHandle.pSignerInfo,
                        ref nestedHandle.dwObjSize
                    );
                    if (!result)
                        continue;

                    nestedHandle.hCertStoreHandle = CertOpenStore(
                        CERT_STORE_PROV_MSG,
                        PKCS_7_ASN_ENCODING | CRYPT_ASN_ENCODING,
                        IntPtr.Zero,
                        0,
                        hNestedMsg
                    );

                    succeded = true;
                    NestedChain.Add(nestedHandle);
                }
            }
            finally
            {
                if (hNestedMsg != IntPtr.Zero)
                    CryptMsgClose(hNestedMsg);
            }

            return succeded;
        }

        private static bool CustomCryptMsgGetParam(
            IntPtr hCryptMsg,
            uint paramType,
            uint index,
            ref IntPtr pParam,
            ref uint outSize)
        {
            bool result;
            uint size = 0;

            result = CryptMsgGetParam(
                hCryptMsg,
                paramType,
                index,
                IntPtr.Zero,
                ref size
            );
            if (!result)
                return false;

            pParam = Marshal.AllocHGlobal((int)size);
            if (pParam == IntPtr.Zero)
                return false;

            result = CryptMsgGetParam(
                hCryptMsg,
                paramType,
                index,
                pParam,
                ref size
            );
            if (!result)
                return false;

            outSize = size;
            return true;
        }

        private static ushort XCHWordLitend(uint num)
           => (ushort)(((((ushort)num) & 0xFF00) >> 8) | (((ushort)num) & 0x00FF) << 8);

        private static long EightByteAlign(long offset, long b)
            => ((offset + b + 7) & 0xFFFFFFF8L) - (b & 0xFFFFFFF8L);

        private static bool Memcmp(IntPtr ptr1, byte[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                if (Marshal.ReadByte(ptr1, i) != arr[i])
                    return false;
            }

            return true;
        }

        private static (bool, string) CalculateSignVersion(uint versionNumber)
        {
            var res = versionNumber switch
            {
                CERT_V1 => "V1",
                CERT_V2 => "V2",
                CERT_V3 => "V3",
                _ => "Unknown",
            };
            return (true, res);
        }

        private static bool CalculateDigestAlgorithm(string pszObjId, SignNodeInfo info)
        {
            if (string.IsNullOrWhiteSpace(pszObjId))
                info.DigestAlgorithm = "Unknown";
            else if (pszObjId == szOID_OIWSEC_sha1)
                info.DigestAlgorithm = "SHA1";
            else if (pszObjId == szOID_RSA_MD5)
                info.DigestAlgorithm = "MD5";
            else if (pszObjId == szOID_NIST_sha256)
                info.DigestAlgorithm = "SHA256";
            else
                info.DigestAlgorithm = StripString(pszObjId);

            return true;
        }

        private static bool SafeToReadNBytes(uint size, uint start, uint requestSize)
            => size - start >= requestSize;

        private static void ParseDERType(byte bIn, ref int iType, ref int iClass)
        {
            iType = bIn & 0x3F;
            iClass = bIn >> 6;
        }

        private static uint ReadNumberFromNBytes(IntPtr pbSignature, uint start, uint requestSize)
        {
            uint number = 0;
            for (var i = 0; i < requestSize; i++)
                number = number * 0x100 + Marshal.ReadByte(pbSignature, (int)(start + i));

            return number;
        }

        private static bool ParseDERSize(IntPtr pbSignature, uint size, ref uint sizeFound, ref uint bytesParsed)
        {
            var val = Marshal.ReadByte(pbSignature);
            if (val > 0x80 && !SafeToReadNBytes(size, 1, val - 0x80u))
                return false;

            if (val <= 0x80)
            {
                sizeFound = val;
                bytesParsed = 1;
            }
            else
            {
                sizeFound = ReadNumberFromNBytes(pbSignature, 1, val - 0x80u);
                bytesParsed = val - 0x80u + 1;
            }

            return true;
        }

        private static string StripString(string? str)
        {
            return str?
                .Replace("\t", "")?
                .Replace("\n", "")?
                .Replace("\r", "")?
                .Replace(((char)0).ToString(), "") ?? string.Empty;
        }

        private static string TimeToString(IntPtr pftIn, IntPtr pstIn = 0)
        {
            if (pstIn == IntPtr.Zero)
            {
                if (pftIn == IntPtr.Zero)
                    return string.Empty;

                pstIn = Marshal.AllocHGlobal(Marshal.SizeOf<SYSTEMTIME>());
                FileTimeToSystemTime(pftIn, pstIn);
            }

            var st = Marshal.PtrToStructure<SYSTEMTIME>(pstIn);
            var date = new DateTime(
                st.Year, st.Month, st.Day,
                st.Hour, st.Minute, st.Second
            );

            return formatter.ToLongLabel(date);
        }

        class SignCounterSign
        {
            public string TimeStamp { get; set; } = string.Empty;
        }

        class SignNodeInfo
        {
            public bool IsValid { get; set; } = false;
            public string DigestAlgorithm { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public int Index { get; set; } = 0;
            public SignCounterSign CounterSign { get; set; } = new();
            public List<CertNodeInfoItem> CertChain = [];
        }
    }
}
