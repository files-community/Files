// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Contracts
{
	public interface IDetailsLayoutColumnItem
	{
		public string Name { get; }

		public double UserLengthPixels { get; }

		public bool UserCollapsed { get; }

		public bool IsHidden { get; }
	}
}
