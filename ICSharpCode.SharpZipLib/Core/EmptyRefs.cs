using System;

namespace ICSharpCode.SharpZipLib.Core
{
	internal static class Empty
	{
#if NET45
		internal static class EmptyArray<T>
		{
			public static readonly T[] Value = new T[0];
		}
		public static T[] Array<T>() => EmptyArray<T>.Value;
#else
		public static T[] Array<T>() => System.Array.Empty<T>();
#endif
	}
}
