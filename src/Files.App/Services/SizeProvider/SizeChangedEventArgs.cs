// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;

namespace Files.App.Services.SizeProvider
{
	public sealed class SizeChangedEventArgs : EventArgs
	{
		public string Path { get; }
		public ulong NewSize { get; }
		public SizeChangedValueState ValueState { get; }

		public SizeChangedEventArgs(string path, ulong newSize = 0, SizeChangedValueState valueState = SizeChangedValueState.None)
			=> (Path, NewSize, ValueState) = (path, newSize, valueState);
	}
}
