// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;

namespace Files.Backend.Services.SizeProvider
{
	public class SizeChangedEventArgs : EventArgs
	{
		public string Path { get; }
		public ulong NewSize { get; }
		public SizeChangedValueState ValueState { get; }

		public SizeChangedEventArgs(string path, ulong newSize = 0, SizeChangedValueState valueState = SizeChangedValueState.None)
			=> (Path, NewSize, ValueState) = (path, newSize, valueState);
	}
}
