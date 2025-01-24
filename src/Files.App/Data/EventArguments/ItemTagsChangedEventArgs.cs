// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.EventArguments
{
	public sealed class ItemTagsChangedEventArgs(string[] uids) : EventArgs
	{
		public string[] TagUids { get; } = uids;
	}
}
