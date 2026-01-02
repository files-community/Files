// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security.Cryptography;
using Windows.Win32.Security.WinTrust;
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
		private const uint CERT_SYSTEM_STORE_CURRENT_USER = 0x00010000;
		private const uint PKCS_7_ASN_ENCODING = 0x00010000;
		private const uint CRYPT_ASN_ENCODING = 0x00000001;
		private const CERT_QUERY_ENCODING_TYPE ENCODING =
			CERT_QUERY_ENCODING_TYPE.X509_ASN_ENCODING | CERT_QUERY_ENCODING_TYPE.PKCS_7_ASN_ENCODING;

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
			HWND hWnd,
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

		public unsafe static void DisplaySignerInfoDialog(string filePath, HWND hwndParent, int index)
		{
			if (string.IsNullOrEmpty(filePath))
				return;

			void* hAuthCryptMsg = null;
			var signHandle = new SignDataHandle();
			var signDataChain = new List<SignDataHandle>();

			try
			{
				var result = TryGetSignerInfo(
					filePath,
					out hAuthCryptMsg,
					out signHandle.hCertStoreHandle,
					out signHandle.pSignerInfo,
					out signHandle.dwObjSize
				);
				if (!result || signHandle.pSignerInfo is null)
					return;

				signDataChain.Add(signHandle);
				GetNestedSignerInfo(ref signHandle, signDataChain);
				if (index >= signDataChain.Count)
					return;

				signHandle = signDataChain[index];
				var issuer = signHandle.pSignerInfo->Issuer;
				var pCertContext = PInvoke.CertFindCertificateInStore(
					signHandle.hCertStoreHandle,
					ENCODING,
					0,
					CERT_FIND_FLAGS.CERT_FIND_ISSUER_NAME,
					&issuer,
					null
				);
				if (pCertContext is null)
					return;

				var viewInfo = new CRYPTUI_VIEWSIGNERINFO_STRUCT
				{
					dwSize = (uint)Marshal.SizeOf<CRYPTUI_VIEWSIGNERINFO_STRUCT>(),
					hwndParent = hwndParent,
					dwFlags = 0,
					szTitle = (PCSTR)null,
					pSignerInfo = signHandle.pSignerInfo,
					hMsg = hAuthCryptMsg,
					pszOID = (PCSTR)null,
					dwReserved = null,
					cStores = 1,
					rghStores = (HCERTSTORE*)NativeMemory.Alloc((uint)sizeof(void*)),
					cPropPages = 0,
					rgPropPages = null
				};
				*(viewInfo.rghStores) = signHandle.hCertStoreHandle;

				result = CryptUIDlgViewSignerInfo(&viewInfo);

				PInvoke.CertFreeCertificateContext(pCertContext);
			}
			finally
			{
				// Since signDataChain contains nested signatures,
				// you must release them starting from the last one.
				for (int i = signDataChain.Count - 1; i >= 0; i--)
				{
					if (signDataChain[i].pSignerInfo is not null)
						NativeMemory.Free(signDataChain[i].pSignerInfo);

					if (!signDataChain[i].hCertStoreHandle.IsNull)
						PInvoke.CertCloseStore(signDataChain[i].hCertStoreHandle, 0);
				}

				if (hAuthCryptMsg is not null)
					PInvoke.CryptMsgClose(hAuthCryptMsg);
			}
		}

		private unsafe static bool GetSignerSignatureInfo(
			HCERTSTORE hSystemStore,
			HCERTSTORE hCertStore,
			CERT_CONTEXT* pOrigContext,
			ref CERT_CONTEXT* pCurrContext,
			SignNodeInfo signNode)
		{
			var pCertInfo = pCurrContext->pCertInfo;
			var certNode = new CertNodeInfoItem();

			(_, certNode.Version) = CalculateSignVersion(pCertInfo->dwVersion);
			GetStringFromCertContext(pCurrContext, CERT_NAME_SIMPLE_DISPLAY_TYPE, 0, certNode);
			GetStringFromCertContext(pCurrContext, CERT_NAME_SIMPLE_DISPLAY_TYPE, 1, certNode);

			var pft = &(pCertInfo->NotBefore);
			certNode.ValidFrom = TimeToString(pft);
			pft = &(pCertInfo->NotAfter);
			certNode.ValidTo = TimeToString(pft);

			signNode.CertChain.Add(certNode);

			pCurrContext = PInvoke.CertFindCertificateInStore(
				hCertStore,
				ENCODING,
				0,
				CERT_FIND_FLAGS.CERT_FIND_SUBJECT_NAME,
				&(pCertInfo->Issuer),
				null
			);

			if (pCurrContext is null)
			{
				pCurrContext = PInvoke.CertFindCertificateInStore(
					hSystemStore,
					ENCODING,
					0,
					CERT_FIND_FLAGS.CERT_FIND_SUBJECT_NAME,
					&(pCertInfo->Issuer),
					null
				);
			}

			if (pCurrContext is null)
				return false;

			var result = PInvoke.CertComparePublicKeyInfo(
				ENCODING,
				&pCurrContext->pCertInfo->SubjectPublicKeyInfo,
				&pOrigContext->pCertInfo->SubjectPublicKeyInfo
			);

			return !result;
		}

		private unsafe static bool GetSignerCertificateInfo(string fileName, List<SignNodeInfo> signChain, CancellationToken ct)
		{
			var succeded = false;
			var authSignData = new SignDataHandle() { dwObjSize = 0, hCertStoreHandle = HCERTSTORE.Null, pSignerInfo = null };
			var signDataChain = new List<SignDataHandle>();
			signChain.Clear();

			var cert_store_prov_system = (PCSTR)(byte*)10;
			HCERTSTORE hSystemStore;
			fixed (char* pRoot = "Root")
			{
				hSystemStore = PInvoke.CertOpenStore(
					cert_store_prov_system,
					ENCODING,
					HCRYPTPROV_LEGACY.Null,
					(CERT_OPEN_STORE_FLAGS)CERT_SYSTEM_STORE_CURRENT_USER,
					(void*)pRoot
				); 
			}
			if (hSystemStore == IntPtr.Zero)
				return false;

			void* hAuthCryptMsg = null;
			var result = TryGetSignerInfo(
				fileName,
				out hAuthCryptMsg,
				out authSignData.hCertStoreHandle,
				out authSignData.pSignerInfo,
				out authSignData.dwObjSize
				);

			if (hAuthCryptMsg is not null)
			{
				PInvoke.CryptMsgClose(hAuthCryptMsg);
				hAuthCryptMsg = null;
			}

			if (!result)
			{
				if (authSignData.hCertStoreHandle != IntPtr.Zero)
					PInvoke.CertCloseStore(authSignData.hCertStoreHandle, 0);

				PInvoke.CertCloseStore(hSystemStore, 0);
				return false;
			}

			signDataChain.Add(authSignData);
			GetNestedSignerInfo(ref authSignData, signDataChain);

			for (var i = 0; i < signDataChain.Count; i++)
			{
				if (ct.IsCancellationRequested)
				{
					PInvoke.CertCloseStore(hSystemStore, 0);
					return false;
				}

				CERT_CONTEXT* pCurrContext = null;
				CMSG_SIGNER_INFO* pCounterSigner = null;
				var signNode = new SignNodeInfo();

				GetCounterSignerInfo(signDataChain[i].pSignerInfo, &pCounterSigner);
				if (pCounterSigner is not null)
					GetCounterSignerData(pCounterSigner, signNode.CounterSign);
				else
					GetGeneralizedTimeStamp(signDataChain[i].pSignerInfo, signNode.CounterSign);

				var pszObjId = signDataChain[i].pSignerInfo->HashAlgorithm.pszObjId;
				var szObjId = new string((sbyte*)(byte*)pszObjId);
				CalculateDigestAlgorithm(szObjId, signNode);
				(_, signNode.Version) = CalculateSignVersion(signDataChain[i].pSignerInfo->dwVersion);


				var pIssuer = &(signDataChain[i].pSignerInfo->Issuer);
				pCurrContext = PInvoke.CertFindCertificateInStore(
					signDataChain[i].hCertStoreHandle,
					ENCODING,
					0,
					CERT_FIND_FLAGS.CERT_FIND_ISSUER_NAME,
					pIssuer,
					null
				);

				result = pCurrContext is not null;
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
					PInvoke.CertFreeCertificateContext(pOrigContext);
				}

				if (pCurrContext is not null)
					PInvoke.CertFreeCertificateContext(pCurrContext);

				if (pCounterSigner is not null)
					NativeMemory.Free(pCounterSigner);

				if (signDataChain[i].pSignerInfo is not null)
					NativeMemory.Free(signDataChain[i].pSignerInfo);

				if (!signDataChain[i].hCertStoreHandle.IsNull)
					PInvoke.CertCloseStore(signDataChain[i].hCertStoreHandle, 0);

				succeded = true;
				signNode.IsValid = VerifyySignature(fileName);
				signNode.Index = i;
				signChain.Add(signNode);
			}

			PInvoke.CertCloseStore(hSystemStore, 0);
			return succeded;
		}

		private unsafe static bool VerifyySignature(string certPath)
		{
			int res = 1;
			var sFileInfo = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>();
			var sData = (uint)Marshal.SizeOf<WINTRUST_DATA>();
			var actionGuid = new Guid("{00AAC56B-CD44-11D0-8CC2-00C04FC295EE}");

			fixed (char* pCertPath = certPath)
			{
				var fileInfo = new WINTRUST_FILE_INFO
				{
					cbStruct = sFileInfo,
					pcwszFilePath = (PCWSTR)pCertPath,
					hFile = (HANDLE)null,
					pgKnownSubject = null
				};

				var wintrustData = new WINTRUST_DATA
				{
					cbStruct = sData,
					pPolicyCallbackData = null,
					pSIPClientData = null,
					dwUIChoice = WINTRUST_DATA_UICHOICE.WTD_UI_NONE,
					fdwRevocationChecks = 0,    // No revocation checking
					dwUnionChoice = WINTRUST_DATA_UNION_CHOICE.WTD_CHOICE_FILE,
					dwStateAction = WINTRUST_DATA_STATE_ACTION.WTD_STATEACTION_VERIFY,
					hWVTStateData = (HANDLE)null,
					pwszURLReference = null,
					dwUIContext = 0,
					Anonymous = new WINTRUST_DATA._Anonymous_e__Union
					{
						pFile = &fileInfo,
					},
				};

				res = PInvoke.WinVerifyTrust((HWND)null, ref actionGuid, &wintrustData);

				// Release hWVTStateData
				wintrustData.dwStateAction = WINTRUST_DATA_STATE_ACTION.WTD_STATEACTION_CLOSE;
				PInvoke.WinVerifyTrust((HWND)null, ref actionGuid, &wintrustData);
			}

			return res == 0;
		}

		private unsafe static bool TryGetSignerInfo(
		   string fileName,
		   out void* hMsg,
		   out HCERTSTORE hCertStore,
		   out CMSG_SIGNER_INFO* pSignerInfo,
		   out uint signerSize,
		   uint index = 0)
		{
			CERT_QUERY_ENCODING_TYPE encoding = 0;
			CERT_QUERY_CONTENT_TYPE dummy = 0;
			CERT_QUERY_FORMAT_TYPE dummy2 = 0;
			void* pDummy = null;
			BOOL result = false;

			HCERTSTORE hCertStoreTmp = HCERTSTORE.Null;
			void* hMsgTmp = null;
			
			fixed (char* pFileName = fileName)
			{
				result = PInvoke.CryptQueryObject(
					CERT_QUERY_OBJECT_TYPE.CERT_QUERY_OBJECT_FILE,
					pFileName,
					CERT_QUERY_CONTENT_TYPE_FLAGS.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED,
					CERT_QUERY_FORMAT_TYPE_FLAGS.CERT_QUERY_FORMAT_FLAG_BINARY,
					0,
					&encoding,
					&dummy,
					&dummy2,
					&hCertStoreTmp,
					&hMsgTmp,
					&pDummy
				);
			}

			hCertStore = hCertStoreTmp;
			hMsg = hMsgTmp;
			pSignerInfo = null;
			signerSize = 0;

			if (!result)
				return false;

			var vpSignerInfo = (void*)pSignerInfo;
			result = CustomCryptMsgGetParam(
				hMsg,
				CMSG_SIGNER_INFO_PARAM,
				index,
				ref vpSignerInfo,
				ref signerSize
			);
			pSignerInfo = (CMSG_SIGNER_INFO*)vpSignerInfo;

			return result;
		}

		private unsafe static bool GetCounterSignerInfo(
			CMSG_SIGNER_INFO* pSignerInfo,
			CMSG_SIGNER_INFO** pTargetSigner)
		{
			uint objSize = 0;
			if (pSignerInfo is null || pTargetSigner is null)
				return false;

			try
			{
				*pTargetSigner = null;
				CRYPT_ATTRIBUTE* attr = null;
				var res = TryGetUnauthAttr(pSignerInfo, szOID_RSA_counterSign, ref attr);
				if (!res || attr is null)
					return false;

				var pkcs7_signer_info = (PCSTR)(byte*)500;
				var result = PInvoke.CryptDecodeObject(
					ENCODING,
					pkcs7_signer_info,
					attr->rgValue[0].pbData,
					attr->rgValue[0].cbData,
					0,
					null,
					&objSize
				);
				if (!result)
					return false;

				*pTargetSigner = (CMSG_SIGNER_INFO*)NativeMemory.Alloc(objSize);
				if (*pTargetSigner is null)
					return false;

				result = PInvoke.CryptDecodeObject(
					ENCODING,
					pkcs7_signer_info,
					attr->rgValue[0].pbData,
					attr->rgValue[0].cbData,
					0,
					*pTargetSigner,
					&objSize
				);
				if (!result)
					return false;
			}
			finally
			{
			}

			return true;
		}

		private unsafe static bool GetCounterSignerData(CMSG_SIGNER_INFO* pSignerInfo, SignCounterSign counterSign)
		{
			CRYPT_ATTRIBUTE* attr = null;
			var res = TryGetAuthAttr(pSignerInfo, szOID_RSA_signingTime, ref attr);
			if (!res || attr is null)
				return false;

			var data = (uint)Marshal.SizeOf<System.Runtime.InteropServices.ComTypes.FILETIME>();
			var ft = (System.Runtime.InteropServices.ComTypes.FILETIME*)NativeMemory.Alloc(data);
			try
			{
				var pkcs_utc_time = (PCSTR)(byte*)17;
				var result = PInvoke.CryptDecodeObject(
					ENCODING,
					pkcs_utc_time,
					attr->rgValue[0].pbData,
					attr->rgValue[0].cbData,
					0,
					ft,
					&data
				);
				if (!result)
					return false;

				PInvoke.FileTimeToLocalFileTime(*ft, out var lft);
				PInvoke.FileTimeToSystemTime(lft, out var st);
				counterSign.TimeStamp = TimeToString(null, &st);

				return true;
			}
			finally
			{
				NativeMemory.Free(ft);
			}
		}

		private unsafe static bool ParseDERFindType(
			int typeSearch,
			byte* pbSignature,
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
			if (pbSignature is null)
				return false;

			while (size > position)
			{
				if (!SafeToReadNBytes(size, position, 2))
					return false;

				ParseDERType(pbSignature[position], ref iType, ref iClass);
				switch (iType)
				{
					case 0x05: // Null
						++position;
						if (pbSignature[position] != 0x00)
							return false;

						++position;
						break;

					case 0x06: // Object Identifier
						++position;
						if (!SafeToReadNBytes(size - position, 1, pbSignature[position]))
							return false;

						position += 1u + pbSignature[position];
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
								pbSignature + position,
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
								pbSignature + position,
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

		private unsafe static bool GetGeneralizedTimeStamp(
			CMSG_SIGNER_INFO* pSignerInfo,
			SignCounterSign counter)
		{
			uint positionFound = 0;
			uint lengthFound = 0;
			CRYPT_ATTRIBUTE* attr = null;
			var res = TryGetUnauthAttr(pSignerInfo, szOID_RFC3161_counterSign, ref attr);
			if (!res || attr is null)
				return false;

			var result = ParseDERFindType(
				0x04,
				attr->rgValue[0].pbData,
				attr->rgValue[0].cbData,
				ref positionFound,
				ref lengthFound);
			if (!result)
				return false;

			// Counter Signer Timstamp
			var pbOctetString = attr->rgValue[0].pbData + positionFound;
			counter.TimeStamp = GetTimeStampFromDER(pbOctetString, lengthFound, ref positionFound);

			return true;
		}

		private unsafe static string GetTimeStampFromDER(byte* pbOctetString, uint lengthFound, ref uint positionFound)
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

			var st = new Windows.Win32.Foundation.SYSTEMTIME();
			var buffer = new string((sbyte*)(pbOctetString + positionFound));

			_ = ushort.TryParse(buffer.AsSpan(0, 4), out st.wYear);
			_ = ushort.TryParse(buffer.AsSpan(4, 2), out st.wMonth);
			_ = ushort.TryParse(buffer.AsSpan(6, 2), out st.wDay);
			_ = ushort.TryParse(buffer.AsSpan(8, 2), out st.wHour);
			_ = ushort.TryParse(buffer.AsSpan(10, 2), out st.wMinute);
			_ = ushort.TryParse(buffer.AsSpan(12, 2), out st.wSecond);
			_ = ushort.TryParse(buffer.AsSpan(15, 3), out st.wMilliseconds);

			PInvoke.SystemTimeToFileTime(st, out var fft);
			PInvoke.FileTimeToLocalFileTime(fft, out var lft);
			PInvoke.FileTimeToSystemTime(lft, out var lst);
			var timestamp = TimeToString(null, &lst);

			return timestamp;
		}

		private unsafe static bool GetStringFromCertContext(CERT_CONTEXT* pCertContext, uint dwType, uint flag, CertNodeInfoItem info)
		{
			var data = PInvoke.CertGetNameString(pCertContext, dwType, flag, null, (PWSTR)null, 0);
			if (data == 0)
			{
				PInvoke.CertFreeCertificateContext(pCertContext);
				return false;
			}

			var pszTempName = (PWSTR)NativeMemory.Alloc(data * sizeof(char));
			if (pszTempName.Value is null)
			{
				PInvoke.CertFreeCertificateContext(pCertContext);
				NativeMemory.Free(pszTempName);
				return false;
			}

			data = PInvoke.CertGetNameString(pCertContext, dwType, flag, null, pszTempName, data);
			if (data == 0)
			{
				NativeMemory.Free(pszTempName);
				return false;
			}

			var name = pszTempName.AsSpan().ToString();
			NativeMemory.Free(pszTempName);
			if (flag == 0)
				info.IssuedTo = StripString(name);
			else
				info.IssuedBy = StripString(name);

			return true;
		}

		private unsafe static bool TryGetUnauthAttr(CMSG_SIGNER_INFO* pSignerInfo, string oid, ref CRYPT_ATTRIBUTE* attr)
		{
			int n = 0;
			attr = null;
			for (; n < pSignerInfo->UnauthAttrs.cAttr; n++)
			{
				attr = &pSignerInfo->UnauthAttrs.rgAttr[n];
				var objId = new string((sbyte*)(byte*)attr->pszObjId);
				if (objId == oid)
					break;
			}

			return n < pSignerInfo->UnauthAttrs.cAttr;
		}

		private unsafe static bool TryGetAuthAttr(CMSG_SIGNER_INFO* pSignerInfo, string oid, ref CRYPT_ATTRIBUTE* attr)
		{
			int n = 0;
			attr = null;
			for (; n < pSignerInfo->AuthAttrs.cAttr; n++)
			{
				attr = &pSignerInfo->AuthAttrs.rgAttr[n];
				var objId = new string((sbyte*)(byte*)attr->pszObjId);
				if (objId == oid)
					break;
			}

			return n < pSignerInfo->AuthAttrs.cAttr;
		}

		private unsafe static bool GetNestedSignerInfo(ref SignDataHandle AuthSignData, List<SignDataHandle> NestedChain)
		{
			var succeded = false;
			void* hNestedMsg = null;
			if (AuthSignData.pSignerInfo is null)
				return false;

			try
			{
				CRYPT_ATTRIBUTE* attr = null;
				var res = TryGetUnauthAttr(AuthSignData.pSignerInfo, szOID_NESTED_SIGNATURE, ref attr);
				if (!res || attr is null)
					return false;

				var cbCurrData = attr->rgValue[0].cbData;
				var pbCurrData = attr->rgValue[0].pbData;
				var upperBound = AuthSignData.pSignerInfo + AuthSignData.dwObjSize;
				while (pbCurrData > AuthSignData.pSignerInfo && pbCurrData < upperBound)
				{
					var nestedHandle = new SignDataHandle() { dwObjSize = 0, pSignerInfo = null, hCertStoreHandle = HCERTSTORE.Null };
					if (!Memcmp(pbCurrData, SG_ProtoCoded) ||
						!Memcmp(pbCurrData + 6, SG_SignedData))
					{
						break;
					}

					hNestedMsg = PInvoke.CryptMsgOpenToDecode(
						PKCS_7_ASN_ENCODING | CRYPT_ASN_ENCODING,
						0,
						0,
						HCRYPTPROV_LEGACY.Null,
						null,
						null
					);
					if (hNestedMsg is null)
						return false;

					cbCurrData = XCHWordLitend(*(ushort*)(pbCurrData + 2)) + 4u;
					var pbNextData = pbCurrData;
					pbNextData += EightByteAlign(cbCurrData, (long)pbCurrData);
					var result = PInvoke.CryptMsgUpdate(hNestedMsg, pbCurrData, cbCurrData, true);
					pbCurrData = pbNextData;
					if (!result)
						continue;

					var pSignerInfo = (void*)nestedHandle.pSignerInfo;
					result = CustomCryptMsgGetParam(
						hNestedMsg,
						CMSG_SIGNER_INFO_PARAM,
						0,
						ref pSignerInfo,
						ref nestedHandle.dwObjSize
					);
					nestedHandle.pSignerInfo = (CMSG_SIGNER_INFO*)pSignerInfo;
					if (!result)
						continue;

					var cert_store_prov_msg = (PCSTR)(byte*)1;
					nestedHandle.hCertStoreHandle = PInvoke.CertOpenStore(
						cert_store_prov_msg,
						ENCODING,
						HCRYPTPROV_LEGACY.Null,
						0,
						hNestedMsg
					);

					succeded = true;
					NestedChain.Add(nestedHandle);
				}
			}
			finally
			{
				if (hNestedMsg is not null)
					PInvoke.CryptMsgClose(hNestedMsg);
			}

			return succeded;
		}

		private unsafe static bool CustomCryptMsgGetParam(
			void* hCryptMsg,
			uint paramType,
			uint index,
			ref void* pParam,
			ref uint outSize)
		{
			bool result;
			uint size = 0;

			result = PInvoke.CryptMsgGetParam(
				hCryptMsg,
				paramType,
				index,
				null,
				&size
			);
			if (!result)
				return false;

			pParam = NativeMemory.Alloc(size);
			if (pParam is null)
				return false;

			result = PInvoke.CryptMsgGetParam(
				hCryptMsg,
				paramType,
				index,
				pParam,
				&size
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

		private unsafe static bool Memcmp(byte* ptr1, byte[] arr)
		{
			for (var i = 0; i < arr.Length; i++)
			{
				if (ptr1[i] != arr[i])
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

		private unsafe static uint ReadNumberFromNBytes(byte* pbSignature, uint start, uint requestSize)
		{
			uint number = 0;
			for (var i = 0; i < requestSize; i++)
				number = number * 0x100 + pbSignature[start + i];

			return number;
		}

		private unsafe static bool ParseDERSize(byte* pbSignature, uint size, ref uint sizeFound, ref uint bytesParsed)
		{
			if (pbSignature[0] > 0x80 && !SafeToReadNBytes(size, 1, pbSignature[0] - 0x80u))
				return false;

			if (pbSignature[0] <= 0x80)
			{
				sizeFound = pbSignature[0];
				bytesParsed = 1;
			}
			else
			{
				sizeFound = ReadNumberFromNBytes(pbSignature, 1, pbSignature[0] - 0x80u);
				bytesParsed = pbSignature[0] - 0x80u + 1;
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

		private unsafe static string TimeToString(
			System.Runtime.InteropServices.ComTypes.FILETIME* pftIn,
			Windows.Win32.Foundation.SYSTEMTIME* pstIn = null)
		{
			if (pstIn is null)
			{
				if (pftIn is null)
					return string.Empty;

				PInvoke.FileTimeToSystemTime(*pftIn, out var sysTime);
				pstIn = &sysTime;
			}

			var date = new DateTime(
				pstIn->wYear, pstIn->wMonth, pstIn->wDay,
				pstIn->wHour, pstIn->wMinute, pstIn->wSecond
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
			public List<CertNodeInfoItem> CertChain { get; set; } = [];
		}
	}
}
