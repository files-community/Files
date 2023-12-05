// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Data.Contracts
{
	public interface IDetailsLayoutColumnItem
	{
		public DetailsLayoutColumnKind Kind { get; }

		public double Width { get; }

		public bool IsVisible { get; }
	}
}
