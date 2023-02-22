using Files.Core.Extensions;
using System;
using System.Linq;

namespace Files.Core.SecureStore
{
	public sealed class DisposableArray : FreeableStore<DisposableArray>
	{
		public byte[] Bytes { get; }

		public DisposableArray(byte[] array)
		{
			Bytes = array;
		}

		public override DisposableArray CreateCopy()
		{
			return new DisposableArray(Bytes.CloneArray());
		}

		public override bool Equals(DisposableArray other)
		{
			if (other?.Bytes is null || Bytes is null)
			{
				return false;
			}

			return Bytes.SequenceEqual(other.Bytes);
		}

		public override int GetHashCode()
		{
			return Bytes.GetHashCode();
		}

		protected override void SecureFree()
		{
			EnsureSecureDisposal(Bytes);
		}

		internal static void EnsureSecureDisposal(byte[] buffer)
		{
			Array.Clear(buffer, 0, buffer.Length);

			//var bufHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			//try
			//{
			//    IntPtr bufPtr = bufHandle.AddrOfPinnedObject();
			//    UIntPtr cnt = new UIntPtr((uint)buffer.Length * (uint)sizeof(byte));

			//    UnsafeNativeApis.RtlZeroMemory(bufPtr, cnt);
			//}
			//finally
			//{
			//    bufHandle.Free();
			//}
		}

		public static implicit operator byte[](DisposableArray disposableArray) => disposableArray.Bytes;
	}
}
